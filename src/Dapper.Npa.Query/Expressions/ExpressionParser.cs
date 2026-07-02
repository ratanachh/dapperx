namespace Dapper.Npa.Query.Expressions;
using System.Linq.Expressions;

public static class ExpressionParser
{
    public static string GetMemberName<T>(Expression<Func<T, object?>> expr)
    {
        return expr.Body switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression { Operand: MemberExpression m } => m.Member.Name,
            _ => throw new ArgumentException($"Cannot extract member name from expression: {expr}")
        };
    }
}
