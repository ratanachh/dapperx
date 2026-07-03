using DapperX.Abstractions.Exceptions;

namespace DapperX.Query.Expressions;

using System.Collections;
using System.Linq.Expressions;
using DapperX.Abstractions.Exceptions;

/// <summary>
/// Translates lambda predicates to parameterized SQL WHERE fragments.
/// Uses the generated ResolveColumn(propertyName) switch — no MemberInfo reflection.
/// </summary>
public sealed class WhereTranslator
{
    private readonly Func<string, string> _resolveColumn;
    private readonly string _provider;
    private readonly List<string> _fragments = new();
    private readonly Dictionary<string, object?> _parameters = new();
    private int _paramCount;

    public WhereTranslator(Func<string, string> resolveColumn, string provider = "SqlServer")
    {
        _resolveColumn = resolveColumn;
        _provider = provider;
    }

    public (string Sql, IReadOnlyDictionary<string, object?> Parameters) Translate<T>(
        IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        foreach (var pred in predicates)
            _fragments.Add(TranslateExpression(pred.Body));
        return (string.Join(" AND ", _fragments), _parameters);
    }

    public (string Sql, IReadOnlyDictionary<string, object?> Parameters) Translate(
        IEnumerable<LambdaExpression> predicates)
    {
        foreach (var pred in predicates)
            _fragments.Add(TranslateExpression(pred.Body));
        return (string.Join(" AND ", _fragments), _parameters);
    }

    private string TranslateExpression(Expression expr)
    {
        return expr switch
        {
            BinaryExpression b => TranslateBinary(b),
            UnaryExpression { NodeType: ExpressionType.Not, Operand: var op } => $"(NOT {TranslateExpression(op)})",
            UnaryExpression { Operand: var op } => TranslateExpression(op),
            MethodCallExpression m => TranslateMethodCall(m),
            MemberExpression m when m.Type == typeof(bool) => $"{GetColumnName(m)} = 1",
            _ => throw Unsupported(expr),
        };
    }

    private string TranslateBinary(BinaryExpression b)
    {
        if (b.NodeType == ExpressionType.AndAlso)
            return $"({TranslateExpression(b.Left)} AND {TranslateExpression(b.Right)})";
        if (b.NodeType == ExpressionType.OrElse)
            return $"({TranslateExpression(b.Left)} OR {TranslateExpression(b.Right)})";

        if (IsNullConstant(b.Right) && b.NodeType == ExpressionType.Equal)
            return $"{GetColumnName(b.Left)} IS NULL";
        if (IsNullConstant(b.Right) && b.NodeType == ExpressionType.NotEqual)
            return $"{GetColumnName(b.Left)} IS NOT NULL";
        if (IsNullConstant(b.Left) && b.NodeType == ExpressionType.Equal)
            return $"{GetColumnName(b.Right)} IS NULL";
        if (IsNullConstant(b.Left) && b.NodeType == ExpressionType.NotEqual)
            return $"{GetColumnName(b.Right)} IS NOT NULL";

        if (IsBooleanComparison(b, out var boolValue))
        {
            var col = GetColumnName(b.Left);
            return boolValue ? $"{col} = 1" : $"{col} = 0";
        }

        var colName = GetColumnName(b.Left);
        var op = b.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw Unsupported(b),
        };
        var paramName = NextParam();
        _parameters[paramName] = GetValue(b.Right);
        return $"{colName} {op} @{paramName}";
    }

    private string TranslateMethodCall(MethodCallExpression m)
    {
        if (m.Method.Name != "Contains")
            throw Unsupported(m);

        if (m.Object is not null
            && (m.Method.DeclaringType == typeof(string) || m.Object.Type == typeof(string)))
            return TranslateStringContains(m);

        if (m.Object is not null && m.Arguments.Count >= 1)
            return TranslateEnumerableContains(m);

        if (m.Object is null && m.Arguments.Count >= 2)
            return TranslateEnumerableContains(m);

        throw Unsupported(m);
    }

    private string TranslateStringContains(MethodCallExpression m)
    {
        if (m.Arguments.Count < 1)
            throw Unsupported(m);

        var col = GetColumnName(m.Object!);
        var paramName = NextParam();
        var value = GetValue(m.Arguments[0]) as string
            ?? throw Unsupported(m);
        _parameters[paramName] = value;
        return $"{col} LIKE {LikePattern(paramName)}";
    }

    private string TranslateEnumerableContains(MethodCallExpression m)
    {
        Expression collectionExpr;
        Expression memberExpr;

        if (m.Object is not null)
        {
            collectionExpr = m.Object;
            memberExpr = m.Arguments[0];
        }
        else if (m.Arguments.Count >= 2)
        {
            collectionExpr = m.Arguments[0];
            memberExpr = m.Arguments[1];
        }
        else
        {
            throw Unsupported(m);
        }

        var col = GetColumnName(memberExpr);
        var paramName = NextParam();
        _parameters[paramName] = EvaluateCollection(collectionExpr);
        return $"{col} IN @{paramName}";
    }

    private string LikePattern(string paramName)
        => _provider is "PostgreSql" or "Sqlite"
            ? $"'%' || @{paramName} || '%'"
            : $"'%' + @{paramName} + '%'";

    private static bool IsNullConstant(Expression expr)
        => expr is ConstantExpression { Value: null };

    private static bool IsBooleanComparison(BinaryExpression b, out bool value)
    {
        value = false;
        if (b.NodeType is not (ExpressionType.Equal or ExpressionType.NotEqual))
            return false;
        if (b.Right is ConstantExpression { Value: bool bv })
        {
            value = b.NodeType == ExpressionType.Equal ? bv : !bv;
            return true;
        }
        return false;
    }

    private string GetColumnName(Expression expr)
    {
        var memberName = expr switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression { Operand: MemberExpression m } => m.Member.Name,
            _ => throw Unsupported(expr),
        };
        return _resolveColumn(memberName);
    }

    private static object? GetValue(Expression expr)
    {
        if (expr is ConstantExpression c)
            return c.Value;
        return Expression.Lambda(expr).Compile().DynamicInvoke();
    }

    private static object EvaluateCollection(Expression expr)
    {
        if (expr is ConstantExpression { Value: IEnumerable constant })
            return constant;

        try
        {
            var value = Expression.Lambda(expr).Compile().DynamicInvoke();
            if (value is IEnumerable enumerable)
                return enumerable;
        }
        catch (Exception)
        {
            throw new UnsupportedQueryExpressionException(
                "IN expression requires a compile-time collection.");
        }

        throw new UnsupportedQueryExpressionException("IN expression requires a compile-time collection.");
    }

    private string NextParam() => $"p{_paramCount++}";

    private static UnsupportedQueryExpressionException Unsupported(Expression expr)
        => new($"Expression type '{expr.NodeType}' is not supported in WhereTranslator.");
}
