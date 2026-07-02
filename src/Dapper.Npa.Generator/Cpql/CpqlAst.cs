namespace Dapper.Npa.Generator.Cpql;

internal abstract class CpqlNode { }

internal sealed class CpqlStatementNode : CpqlNode
{
    public CpqlWithClauseNode? With { get; set; }
    public CpqlSelectStatementNode? Select { get; set; }
    public CpqlUpdateStatementNode? Update { get; set; }
    public CpqlDeleteStatementNode? Delete { get; set; }
}

internal sealed class CpqlWithClauseNode : CpqlNode
{
    public bool Recursive { get; set; }
    public List<CpqlCteNode> Ctes { get; } = new();
}

internal sealed class CpqlCteNode : CpqlNode
{
    public string Name { get; set; } = string.Empty;
    public CpqlSelectStatementNode Body { get; set; } = null!;
}

internal sealed class CpqlSelectStatementNode : CpqlNode
{
    public bool Distinct { get; set; }
    public CpqlSelectListNode SelectList { get; set; } = null!;
    public CpqlFromNode From { get; set; } = null!;
    public List<CpqlJoinNode> Joins { get; } = new();
    public CpqlConditionNode? Where { get; set; }
    public List<CpqlValueNode> GroupBy { get; } = new();
    public CpqlConditionNode? Having { get; set; }
    public List<CpqlOrderItemNode> OrderBy { get; } = new();
}

internal sealed class CpqlUpdateStatementNode : CpqlNode
{
    public string EntityName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public List<CpqlAssignmentNode> Assignments { get; } = new();
    public List<CpqlJoinNode> Joins { get; } = new();
    public CpqlConditionNode? Where { get; set; }
}

internal sealed class CpqlDeleteStatementNode : CpqlNode
{
    public string EntityName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public CpqlConditionNode? Where { get; set; }
}

internal sealed class CpqlSelectListNode : CpqlNode
{
    public List<CpqlSelectItemNode> Items { get; } = new();
}

internal sealed class CpqlSelectItemNode : CpqlNode
{
    public CpqlValueNode Value { get; set; } = null!;
    public string? Alias { get; set; }
}

internal sealed class CpqlFromNode : CpqlNode
{
    public string EntityOrCteName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public bool IsCte { get; set; }
}

internal sealed class CpqlJoinNode : CpqlNode
{
    public bool IsLeft { get; set; }
    public string SourceAlias { get; set; } = string.Empty;
    public string RelationshipProperty { get; set; } = string.Empty;
    public string JoinAlias { get; set; } = string.Empty;
    public CpqlConditionNode? OnCondition { get; set; }
    public bool IsCteJoin { get; set; }
    public string? CteName { get; set; }
}

internal sealed class CpqlAssignmentNode : CpqlNode
{
    public string Alias { get; set; } = string.Empty;
    public string Property { get; set; } = string.Empty;
    public CpqlValueNode Value { get; set; } = null!;
}

internal sealed class CpqlOrderItemNode : CpqlNode
{
    public CpqlValueNode Value { get; set; } = null!;
    public bool Ascending { get; set; } = true;
    public bool? NullsFirst { get; set; }
}

internal abstract class CpqlValueNode : CpqlNode { }

internal sealed class CpqlPropertyPathNode : CpqlValueNode
{
    public string Alias { get; set; } = string.Empty;
    public List<string> Path { get; } = new();
}

internal sealed class CpqlParameterNode : CpqlValueNode
{
    public string Name { get; set; } = string.Empty;
}

internal sealed class CpqlLiteralNode : CpqlValueNode
{
    public object? Value { get; set; }
    public bool IsNull { get; set; }
    public bool IsTrue { get; set; }
    public bool IsFalse { get; set; }
}

internal sealed class CpqlArithmeticNode : CpqlValueNode
{
    public CpqlValueNode Left { get; set; } = null!;
    public string Op { get; set; } = "+";
    public CpqlValueNode Right { get; set; } = null!;
}

internal sealed class CpqlCaseNode : CpqlValueNode
{
    public CpqlValueNode? Input { get; set; }
    public List<CpqlWhenNode> Whens { get; } = new();
    public CpqlValueNode? Else { get; set; }
}

internal sealed class CpqlWhenNode : CpqlNode
{
    public CpqlConditionNode Condition { get; set; } = null!;
    public CpqlValueNode Result { get; set; } = null!;
}

internal sealed class CpqlNewExprNode : CpqlValueNode
{
    public string TypeName { get; set; } = string.Empty;
    public List<CpqlValueNode> Arguments { get; } = new();
}

internal sealed class CpqlAggregateNode : CpqlValueNode
{
    public string Function { get; set; } = string.Empty;
    public CpqlValueNode? Argument { get; set; }
    public bool Distinct { get; set; }
    public bool CountStar { get; set; }
}

internal sealed class CpqlScalarFunctionNode : CpqlValueNode
{
    public string Name { get; set; } = string.Empty;
    public List<CpqlValueNode> Arguments { get; } = new();
    public string? CastType { get; set; }
}

internal sealed class CpqlWindowNode : CpqlValueNode
{
    public CpqlValueNode Inner { get; set; } = null!;
    public List<CpqlValueNode> PartitionBy { get; } = new();
    public List<CpqlOrderItemNode> OrderBy { get; } = new();
    public string? FrameSpec { get; set; }
}

internal sealed class CpqlSubqueryNode : CpqlValueNode
{
    public CpqlSelectStatementNode Select { get; set; } = null!;
    public int NestingDepth { get; set; }
}

internal abstract class CpqlConditionNode : CpqlNode { }

internal sealed class CpqlAndNode : CpqlConditionNode
{
    public CpqlConditionNode Left { get; set; } = null!;
    public CpqlConditionNode Right { get; set; } = null!;
}

internal sealed class CpqlOrNode : CpqlConditionNode
{
    public CpqlConditionNode Left { get; set; } = null!;
    public CpqlConditionNode Right { get; set; } = null!;
}

internal sealed class CpqlNotNode : CpqlConditionNode
{
    public CpqlConditionNode Inner { get; set; } = null!;
}

internal sealed class CpqlPredicateNode : CpqlConditionNode
{
    public CpqlPredicateKind Kind { get; set; }
    public CpqlValueNode? Left { get; set; }
    public CpqlValueNode? Right { get; set; }
    public CpqlValueNode? Right2 { get; set; }
    public string? ComparisonOp { get; set; }
    public CpqlSubqueryNode? Subquery { get; set; }
    public string? ParameterName { get; set; }
    public bool Negated { get; set; }
}

internal enum CpqlPredicateKind
{
    Comparison,
    Between,
    InParameter,
    InSubquery,
    Like,
    IsNull,
    IsTrue,
    IsFalse,
    Exists,
}
