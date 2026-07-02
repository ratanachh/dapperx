namespace Dapper.Npa.Generator.Cpql;

internal sealed class CpqlParser
{
    private readonly List<CpqlToken> _tokens;
    private int _index;
    private int _subqueryDepth;

    public CpqlParser(List<CpqlToken> tokens) => _tokens = tokens;

    public static CpqlStatementNode Parse(string cpql)
    {
        var lexer = new CpqlLexer(cpql);
        var tokens = lexer.Tokenize();
        var parser = new CpqlParser(tokens);
        return parser.ParseStatement();
    }

    private CpqlToken Peek() => _tokens[_index];
    private CpqlToken Advance() => _tokens[_index++];
    private bool MatchKeyword(string kw)
    {
        var t = Peek();
        if (t.Kind == CpqlTokenKind.Keyword && string.Equals(t.Keyword, kw, StringComparison.OrdinalIgnoreCase))
        {
            Advance();
            return true;
        }
        return false;
    }

    private bool Match(CpqlTokenKind kind)
    {
        if (Peek().Kind == kind) { Advance(); return true; }
        return false;
    }

    private CpqlToken Expect(CpqlTokenKind kind, string msg)
    {
        var t = Peek();
        if (t.Kind != kind) throw new CpqlParseException(msg + " at position " + t.Position);
        return Advance();
    }

    private CpqlToken ExpectKeyword(string kw)
    {
        var t = Peek();
        if (t.Kind != CpqlTokenKind.Keyword || !string.Equals(t.Keyword, kw, StringComparison.OrdinalIgnoreCase))
            throw new CpqlParseException("Expected " + kw + " at position " + t.Position);
        return Advance();
    }

    private CpqlStatementNode ParseStatement()
    {
        var stmt = new CpqlStatementNode();
        if (MatchKeyword("WITH"))
        {
            stmt.With = ParseWithClause();
        }

        if (MatchKeyword("SELECT"))
            stmt.Select = ParseSelectStatement();
        else if (MatchKeyword("UPDATE"))
            stmt.Update = ParseUpdateStatement();
        else if (MatchKeyword("DELETE"))
            stmt.Delete = ParseDeleteStatement();
        else
            throw new CpqlParseException("Expected SELECT, UPDATE, or DELETE at position " + Peek().Position);

        Expect(CpqlTokenKind.End, "Unexpected token after statement");
        return stmt;
    }

    private CpqlWithClauseNode ParseWithClause()
    {
        var with = new CpqlWithClauseNode();
        with.Recursive = MatchKeyword("RECURSIVE");
        do
        {
            var cte = new CpqlCteNode
            {
                Name = Expect(CpqlTokenKind.Identifier, "Expected CTE name").Text,
            };
            ExpectKeyword("AS");
            Expect(CpqlTokenKind.LeftParen, "Expected ( after CTE name");
            cte.Body = ParseSelectStatement();
            Expect(CpqlTokenKind.RightParen, "Expected ) after CTE body");
            with.Ctes.Add(cte);
        } while (Match(CpqlTokenKind.Comma));
        return with;
    }

    private CpqlSelectStatementNode ParseSelectStatement()
    {
        var sel = new CpqlSelectStatementNode();
        sel.Distinct = MatchKeyword("DISTINCT");
        sel.SelectList = ParseSelectList();
        ExpectKeyword("FROM");
        sel.From = ParseFrom();
        while (TryParseJoin(out var join))
            sel.Joins.Add(join);
        if (MatchKeyword("WHERE"))
            sel.Where = ParseCondition();
        if (MatchKeyword("GROUP"))
        {
            ExpectKeyword("BY");
            do sel.GroupBy.Add(ParseValue()); while (Match(CpqlTokenKind.Comma));
        }
        if (MatchKeyword("HAVING"))
            sel.Having = ParseCondition();
        if (MatchKeyword("ORDER"))
        {
            ExpectKeyword("BY");
            do sel.OrderBy.Add(ParseOrderItem()); while (Match(CpqlTokenKind.Comma));
        }
        return sel;
    }

    private CpqlSelectListNode ParseSelectList()
    {
        var list = new CpqlSelectListNode();
        do
        {
            var item = new CpqlSelectItemNode { Value = ParseValue() };
            if (MatchKeyword("AS"))
                item.Alias = Expect(CpqlTokenKind.Identifier, "Expected alias").Text;
            else if (Peek().Kind == CpqlTokenKind.Identifier && _tokens[_index + 1].Kind != CpqlTokenKind.Dot
                     && _tokens[_index + 1].Kind != CpqlTokenKind.Comma && _tokens[_index + 1].Kind != CpqlTokenKind.End
                     && _tokens[_index + 1].Kind != CpqlTokenKind.RightParen && !IsKeywordAt(_index))
            {
                // optional alias without AS when unambiguous — skip for safety; require AS
            }
            list.Items.Add(item);
        } while (Match(CpqlTokenKind.Comma));
        return list;
    }

    private bool IsKeywordAt(int idx)
    {
        var t = _tokens[idx];
        return t.Kind == CpqlTokenKind.Keyword;
    }

    private CpqlFromNode ParseFrom()
    {
        var from = new CpqlFromNode
        {
            EntityOrCteName = Expect(CpqlTokenKind.Identifier, "Expected entity name").Text,
            Alias = Expect(CpqlTokenKind.Identifier, "Expected alias").Text,
        };
        return from;
    }

    private bool TryParseJoin(out CpqlJoinNode join)
    {
        join = new CpqlJoinNode();
        if (!MatchKeyword("LEFT") && !MatchKeyword("JOIN"))
        {
            join = null!;
            return false;
        }
        if (_tokens[_index - 1].Keyword == "LEFT")
            join.IsLeft = true;
        if (join.IsLeft)
            ExpectKeyword("JOIN");

        join.SourceAlias = Expect(CpqlTokenKind.Identifier, "Expected source alias").Text;
        Expect(CpqlTokenKind.Dot, "Expected . after alias");
        join.RelationshipProperty = Expect(CpqlTokenKind.Identifier, "Expected relationship").Text;
        join.JoinAlias = Expect(CpqlTokenKind.Identifier, "Expected join alias").Text;
        if (MatchKeyword("ON"))
            join.OnCondition = ParseCondition();
        return true;
    }

    private CpqlUpdateStatementNode ParseUpdateStatement()
    {
        var upd = new CpqlUpdateStatementNode
        {
            EntityName = Expect(CpqlTokenKind.Identifier, "Expected entity").Text,
            Alias = Expect(CpqlTokenKind.Identifier, "Expected alias").Text,
        };
        ExpectKeyword("SET");
        do
        {
            var alias = Expect(CpqlTokenKind.Identifier, "Expected alias").Text;
            Expect(CpqlTokenKind.Dot, "Expected .");
            var prop = Expect(CpqlTokenKind.Identifier, "Expected property").Text;
            Expect(CpqlTokenKind.Equal, "Expected =");
            upd.Assignments.Add(new CpqlAssignmentNode
            {
                Alias = alias,
                Property = prop,
                Value = ParseValue(),
            });
        } while (Match(CpqlTokenKind.Comma));

        while (TryParseJoin(out var join))
            upd.Joins.Add(join);

        if (MatchKeyword("WHERE"))
            upd.Where = ParseCondition();
        return upd;
    }

    private CpqlDeleteStatementNode ParseDeleteStatement()
    {
        ExpectKeyword("FROM");
        return new CpqlDeleteStatementNode
        {
            EntityName = Expect(CpqlTokenKind.Identifier, "Expected entity").Text,
            Alias = Expect(CpqlTokenKind.Identifier, "Expected alias").Text,
            Where = MatchKeyword("WHERE") ? ParseCondition() : null,
        };
    }

    private CpqlOrderItemNode ParseOrderItem()
    {
        var item = new CpqlOrderItemNode { Value = ParseValue() };
        if (MatchKeyword("ASC")) item.Ascending = true;
        else if (MatchKeyword("DESC")) item.Ascending = false;
        if (MatchKeyword("NULLS"))
        {
            if (MatchKeyword("FIRST")) item.NullsFirst = true;
            else if (MatchKeyword("LAST")) item.NullsFirst = false;
        }
        return item;
    }

    private CpqlConditionNode ParseCondition() => ParseOr();

    private CpqlConditionNode ParseOr()
    {
        var left = ParseAnd();
        while (MatchKeyword("OR"))
            left = new CpqlOrNode { Left = left, Right = ParseAnd() };
        return left;
    }

    private CpqlConditionNode ParseAnd()
    {
        var left = ParseNot();
        while (MatchKeyword("AND"))
            left = new CpqlAndNode { Left = left, Right = ParseNot() };
        return left;
    }

    private CpqlConditionNode ParseNot()
    {
        if (MatchKeyword("NOT"))
            return new CpqlNotNode { Inner = ParseNot() };
        return ParsePredicate();
    }

    private CpqlConditionNode ParsePredicate()
    {
        if (Match(CpqlTokenKind.LeftParen))
        {
            var inner = ParseCondition();
            Expect(CpqlTokenKind.RightParen, "Expected )");
            return inner;
        }

        if (MatchKeyword("EXISTS"))
        {
            Expect(CpqlTokenKind.LeftParen, "Expected ( after EXISTS");
            var sub = ParseSubquerySelect();
            Expect(CpqlTokenKind.RightParen, "Expected )");
            return new CpqlPredicateNode
            {
                Kind = CpqlPredicateKind.Exists,
                Subquery = sub,
            };
        }

        var left = ParseValue();
        return ParsePredicateTail(left);
    }

    private CpqlConditionNode ParsePredicateTail(CpqlValueNode left)
    {
        if (MatchKeyword("BETWEEN"))
        {
            var mid = ParseValue();
            ExpectKeyword("AND");
            var high = ParseValue();
            return new CpqlPredicateNode
            {
                Kind = CpqlPredicateKind.Between,
                Left = left,
                Right = mid,
                Right2 = high,
            };
        }

        if (MatchKeyword("IN"))
        {
            Expect(CpqlTokenKind.LeftParen, "Expected ( after IN");
            if (Peek().Kind == CpqlTokenKind.Parameter)
            {
                var p = Advance();
                Expect(CpqlTokenKind.RightParen, "Expected )");
                return new CpqlPredicateNode
                {
                    Kind = CpqlPredicateKind.InParameter,
                    Left = left,
                    ParameterName = p.Text.Substring(1),
                };
            }
            var sub = ParseSubquerySelect();
            Expect(CpqlTokenKind.RightParen, "Expected )");
            return new CpqlPredicateNode
            {
                Kind = CpqlPredicateKind.InSubquery,
                Left = left,
                Subquery = sub,
            };
        }

        if (MatchKeyword("LIKE"))
        {
            return new CpqlPredicateNode
            {
                Kind = CpqlPredicateKind.Like,
                Left = left,
                Right = ParseValue(),
            };
        }

        if (MatchKeyword("IS"))
        {
            if (MatchKeyword("NULL"))
                return new CpqlPredicateNode { Kind = CpqlPredicateKind.IsNull, Left = left };
            if (MatchKeyword("NOT"))
            {
                ExpectKeyword("NULL");
                return new CpqlPredicateNode { Kind = CpqlPredicateKind.IsNull, Left = left, Negated = true };
            }
            if (MatchKeyword("TRUE"))
                return new CpqlPredicateNode { Kind = CpqlPredicateKind.IsTrue, Left = left };
            if (MatchKeyword("FALSE"))
                return new CpqlPredicateNode { Kind = CpqlPredicateKind.IsFalse, Left = left };
        }

        var op = Peek();
        string? cmp = null;
        if (op.Kind == CpqlTokenKind.Equal) { cmp = "="; Advance(); }
        else if (op.Kind == CpqlTokenKind.NotEqual) { cmp = "<>"; Advance(); }
        else if (op.Kind == CpqlTokenKind.Greater) { cmp = ">"; Advance(); }
        else if (op.Kind == CpqlTokenKind.GreaterEqual) { cmp = ">="; Advance(); }
        else if (op.Kind == CpqlTokenKind.Less) { cmp = "<"; Advance(); }
        else if (op.Kind == CpqlTokenKind.LessEqual) { cmp = "<="; Advance(); }
        else
            throw new CpqlParseException("Expected comparison operator at position " + op.Position);

        var right = ParseValue();
        return new CpqlPredicateNode
        {
            Kind = CpqlPredicateKind.Comparison,
            Left = left,
            Right = right,
            ComparisonOp = cmp,
        };
    }

    private CpqlSubqueryNode ParseSubquerySelect()
    {
        _subqueryDepth++;
        if (_subqueryDepth > 1)
            throw new CpqlParseException("Nested subqueries are not allowed");
        var saved = _index;
        try
        {
            if (!MatchKeyword("SELECT"))
                throw new CpqlParseException("Expected SELECT in subquery");
            var sel = ParseSelectStatement();
            return new CpqlSubqueryNode { Select = sel, NestingDepth = _subqueryDepth };
        }
        finally
        {
            _subqueryDepth--;
        }
    }

    private CpqlValueNode ParseValue() => ParseAdd();

    private CpqlValueNode ParseAdd()
    {
        var left = ParseMul();
        while (true)
        {
            if (Match(CpqlTokenKind.Plus))
                left = new CpqlArithmeticNode { Left = left, Op = "+", Right = ParseMul() };
            else if (Match(CpqlTokenKind.Minus))
                left = new CpqlArithmeticNode { Left = left, Op = "-", Right = ParseMul() };
            else break;
        }
        return left;
    }

    private CpqlValueNode ParseMul()
    {
        var left = ParseUnary();
        while (true)
        {
            if (Match(CpqlTokenKind.Multiply))
                left = new CpqlArithmeticNode { Left = left, Op = "*", Right = ParseUnary() };
            else if (Match(CpqlTokenKind.Divide))
                left = new CpqlArithmeticNode { Left = left, Op = "/", Right = ParseUnary() };
            else break;
        }
        return left;
    }

    private CpqlValueNode ParseUnary()
    {
        if (Match(CpqlTokenKind.Minus))
            return new CpqlArithmeticNode
            {
                Left = new CpqlLiteralNode { Value = 0 },
                Op = "-",
                Right = ParseUnary(),
            };
        return ParsePrimary();
    }

    private CpqlValueNode ParsePrimary()
    {
        if (MatchKeyword("CASE"))
            return ParseCase();

        if (MatchKeyword("NEW"))
            return ParseNew();

        if (MatchKeyword("CAST"))
        {
            Expect(CpqlTokenKind.LeftParen, "Expected ( after CAST");
            var arg = ParseValue();
            ExpectKeyword("AS");
            var typeName = ReadCastType();
            Expect(CpqlTokenKind.RightParen, "Expected )");
            var castNode = new CpqlScalarFunctionNode { Name = "CAST", CastType = typeName };
            castNode.Arguments.Add(arg);
            return castNode;
        }

        if (MatchKeyword("NULLIF") || MatchKeyword("CONCAT"))
        {
            var name = _tokens[_index - 1].Keyword!;
            Expect(CpqlTokenKind.LeftParen, "Expected (");
            var args = ParseArgList();
            Expect(CpqlTokenKind.RightParen, "Expected )");
            var fnNode = new CpqlScalarFunctionNode { Name = name };
            fnNode.Arguments.AddRange(args);
            return fnNode;
        }

        if (Peek().Kind == CpqlTokenKind.LeftParen)
        {
            Advance();
            if (Peek().Kind == CpqlTokenKind.Keyword && string.Equals(Peek().Keyword, "SELECT", StringComparison.OrdinalIgnoreCase))
            {
                var sub = ParseSubquerySelect();
                Expect(CpqlTokenKind.RightParen, "Expected )");
                return sub;
            }
            var val = ParseValue();
            Expect(CpqlTokenKind.RightParen, "Expected )");
            return TryParseOver(val);
        }

        if (IsAggregateKeyword(Peek()))
            return ParseAggregateOrScalar();

        if (Peek().Kind == CpqlTokenKind.Keyword && IsScalarFunction(Peek().Keyword!))
            return ParseScalarCall(Peek().Keyword!);

        if (Match(CpqlTokenKind.Parameter))
        {
            var p = _tokens[_index - 1];
            return new CpqlParameterNode { Name = p.Text.Substring(1) };
        }

        if (Match(CpqlTokenKind.StringLiteral))
            return new CpqlLiteralNode { Value = _tokens[_index - 1].Text };

        if (Match(CpqlTokenKind.NumberLiteral))
            return new CpqlLiteralNode { Value = _tokens[_index - 1].Text };

        if (MatchKeyword("NULL"))
            return new CpqlLiteralNode { IsNull = true };

        if (MatchKeyword("TRUE"))
            return new CpqlLiteralNode { IsTrue = true };

        if (MatchKeyword("FALSE"))
            return new CpqlLiteralNode { IsFalse = true };

        if (Peek().Kind == CpqlTokenKind.Identifier)
        {
            var alias = Advance().Text;
            if (Match(CpqlTokenKind.Dot))
            {
                var path = new CpqlPropertyPathNode { Alias = alias };
                path.Path.Add(Expect(CpqlTokenKind.Identifier, "Expected property").Text);
                while (Match(CpqlTokenKind.Dot))
                    path.Path.Add(Expect(CpqlTokenKind.Identifier, "Expected property").Text);
                return path;
            }
            // bare identifier — treat as alias-only entity reference in SELECT e
            return new CpqlPropertyPathNode { Alias = alias };
        }

        throw new CpqlParseException("Unexpected token at position " + Peek().Position);
    }

    private string ReadCastType()
    {
        var parts = new List<string>();
        parts.Add(Expect(CpqlTokenKind.Identifier, "Expected type").Text);
        while (Peek().Kind == CpqlTokenKind.LeftParen)
        {
            Advance();
            parts.Add("(");
            parts.Add(ParseValue().ToString() ?? "");
            Expect(CpqlTokenKind.RightParen, "Expected )");
            parts.Add(")");
        }
        return string.Join(" ", parts);
    }

    private CpqlValueNode ParseCase()
    {
        var c = new CpqlCaseNode();
        if (Peek().Kind != CpqlTokenKind.Keyword || !string.Equals(Peek().Keyword, "WHEN", StringComparison.OrdinalIgnoreCase))
            c.Input = ParseValue();
        while (MatchKeyword("WHEN"))
        {
            var cond = ParseCondition();
            ExpectKeyword("THEN");
            c.Whens.Add(new CpqlWhenNode { Condition = cond, Result = ParseValue() });
        }
        ExpectKeyword("ELSE");
        c.Else = ParseValue();
        ExpectKeyword("END");
        return c;
    }

    private CpqlNewExprNode ParseNew()
    {
        var typeName = Expect(CpqlTokenKind.Identifier, "Expected type name").Text;
        Expect(CpqlTokenKind.LeftParen, "Expected (");
        var n = new CpqlNewExprNode { TypeName = typeName };
        if (Peek().Kind != CpqlTokenKind.RightParen)
        {
            do n.Arguments.Add(ParseValue()); while (Match(CpqlTokenKind.Comma));
        }
        Expect(CpqlTokenKind.RightParen, "Expected )");
        return n;
    }

    private static bool IsAggregateKeyword(CpqlToken t)
    {
        if (t.Kind != CpqlTokenKind.Keyword) return false;
        var k = t.Keyword!;
        return k is "COUNT" or "SUM" or "AVG" or "MIN" or "MAX";
    }

    private CpqlValueNode ParseAggregateOrScalar()
    {
        var fn = Advance().Keyword!;
        Expect(CpqlTokenKind.LeftParen, "Expected (");
        if (fn == "COUNT" && Match(CpqlTokenKind.Star))
        {
            Expect(CpqlTokenKind.RightParen, "Expected )");
            return new CpqlAggregateNode { Function = fn, CountStar = true };
        }
        var distinct = MatchKeyword("DISTINCT");
        CpqlValueNode? arg = null;
        if (Peek().Kind != CpqlTokenKind.RightParen)
            arg = ParseValue();
        Expect(CpqlTokenKind.RightParen, "Expected )");
        var node = new CpqlAggregateNode { Function = fn, Argument = arg, Distinct = distinct };
        return TryParseOver(node);
    }

    private CpqlValueNode ParseScalarCall(string name)
    {
        Advance();
        Expect(CpqlTokenKind.LeftParen, "Expected (");
        var args = ParseArgList();
        Expect(CpqlTokenKind.RightParen, "Expected )");
        var node = new CpqlScalarFunctionNode { Name = name };
        node.Arguments.AddRange(args);
        return TryParseOver(node);
    }

    private static bool IsScalarFunction(string name)
    {
        return CpqlScalarFunctions.IsKnownFunction(name);
    }

    private List<CpqlValueNode> ParseArgList()
    {
        var args = new List<CpqlValueNode>();
        if (Peek().Kind == CpqlTokenKind.RightParen)
            return args;
        do args.Add(ParseValue()); while (Match(CpqlTokenKind.Comma));
        return args;
    }

    private CpqlValueNode TryParseOver(CpqlValueNode inner)
    {
        if (!MatchKeyword("OVER"))
            return inner;
        var win = new CpqlWindowNode { Inner = inner };
        Expect(CpqlTokenKind.LeftParen, "Expected ( after OVER");
        if (MatchKeyword("PARTITION"))
        {
            ExpectKeyword("BY");
            do win.PartitionBy.Add(ParseValue()); while (Match(CpqlTokenKind.Comma));
        }
        if (MatchKeyword("ORDER"))
        {
            ExpectKeyword("BY");
            do win.OrderBy.Add(ParseOrderItem()); while (Match(CpqlTokenKind.Comma));
        }

        if (Peek().Kind == CpqlTokenKind.Keyword
            && (Peek().Keyword is "ROWS" or "RANGE" or "UNBOUNDED"
                || (Peek().Keyword is "CURRENT" && _index + 1 < _tokens.Count)))
        {
            var frameParts = new List<string>();
            while (Peek().Kind != CpqlTokenKind.RightParen && Peek().Kind != CpqlTokenKind.End)
                frameParts.Add(Peek().Kind == CpqlTokenKind.Keyword ? Advance().Keyword! : Advance().Text);
            win.FrameSpec = string.Join(" ", frameParts);
        }

        Expect(CpqlTokenKind.RightParen, "Expected ) after OVER");
        return win;
    }
}
