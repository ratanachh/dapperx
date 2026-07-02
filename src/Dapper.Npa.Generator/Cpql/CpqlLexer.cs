namespace Dapper.Npa.Generator.Cpql;

internal sealed class CpqlLexer
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "DISTINCT", "FROM", "WHERE", "AND", "OR", "NOT", "JOIN", "LEFT", "ON",
        "UPDATE", "SET", "DELETE", "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC",
        "NULLS", "FIRST", "LAST", "AS", "CASE", "WHEN", "THEN", "ELSE", "END", "NEW",
        "WITH", "RECURSIVE", "EXISTS", "IN", "LIKE", "BETWEEN", "IS", "NULL", "TRUE", "FALSE",
        "CAST", "OVER", "PARTITION", "ROWS", "RANGE", "UNBOUNDED", "PRECEDING", "FOLLOWING",
        "CURRENT", "ROW", "DATE", "TIMESTAMP", "NULLIF", "CONCAT",
        "COUNT", "SUM", "AVG", "MIN", "MAX",
        "LOWER", "UPPER", "TRIM", "LTRIM", "RTRIM", "LENGTH", "SUBSTRING", "REPLACE",
        "COALESCE", "ABS", "CEILING", "FLOOR", "ROUND", "POWER", "MOD",
        "YEAR", "MONTH", "DAY", "HOUR", "MINUTE", "SECOND",
        "NOW", "CURRENT_DATE", "CURRENT_TIMESTAMP", "DATEADD", "DATEDIFF",
        "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE", "LAG", "LEAD",
    };

    private readonly string _input;
    private int _pos;

    public CpqlLexer(string input) => _input = input ?? string.Empty;

    public List<CpqlToken> Tokenize()
    {
        var tokens = new List<CpqlToken>();
        while (_pos < _input.Length)
        {
            SkipWhitespace();
            if (_pos >= _input.Length)
                break;

            var start = _pos;
            var c = _input[_pos];

            if (c == ':')
            {
                _pos++;
                var name = ReadIdentifier();
                tokens.Add(new CpqlToken(CpqlTokenKind.Parameter, ":" + name, start));
                continue;
            }

            if (c == '\'' )
            {
                tokens.Add(ReadString(start));
                continue;
            }

            if (char.IsDigit(c) || (c == '.' && _pos + 1 < _input.Length && char.IsDigit(_input[_pos + 1])))
            {
                tokens.Add(ReadNumber(start));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var id = ReadIdentifier();
                if (Keywords.Contains(id))
                    tokens.Add(new CpqlToken(CpqlTokenKind.Keyword, id, start, id.ToUpperInvariant()));
                else
                    tokens.Add(new CpqlToken(CpqlTokenKind.Identifier, id, start));
                continue;
            }

            switch (c)
            {
                case '(': tokens.Add(new CpqlToken(CpqlTokenKind.LeftParen, "(", start)); _pos++; break;
                case ')': tokens.Add(new CpqlToken(CpqlTokenKind.RightParen, ")", start)); _pos++; break;
                case ',': tokens.Add(new CpqlToken(CpqlTokenKind.Comma, ",", start)); _pos++; break;
                case '.': tokens.Add(new CpqlToken(CpqlTokenKind.Dot, ".", start)); _pos++; break;
                case '*': tokens.Add(new CpqlToken(CpqlTokenKind.Star, "*", start)); _pos++; break;
                case '+': tokens.Add(new CpqlToken(CpqlTokenKind.Plus, "+", start)); _pos++; break;
                case '-': tokens.Add(new CpqlToken(CpqlTokenKind.Minus, "-", start)); _pos++; break;
                case '/': tokens.Add(new CpqlToken(CpqlTokenKind.Divide, "/", start)); _pos++; break;
                case '=':
                    tokens.Add(new CpqlToken(CpqlTokenKind.Equal, "=", start));
                    _pos++;
                    break;
                case '<':
                    _pos++;
                    if (Match('=')) tokens.Add(new CpqlToken(CpqlTokenKind.LessEqual, "<=", start));
                    else if (Match('>')) tokens.Add(new CpqlToken(CpqlTokenKind.NotEqual, "<>", start));
                    else tokens.Add(new CpqlToken(CpqlTokenKind.Less, "<", start));
                    break;
                case '>':
                    _pos++;
                    if (Match('=')) tokens.Add(new CpqlToken(CpqlTokenKind.GreaterEqual, ">=", start));
                    else tokens.Add(new CpqlToken(CpqlTokenKind.Greater, ">", start));
                    break;
                case '!':
                    if (Match('='))
                    {
                        tokens.Add(new CpqlToken(CpqlTokenKind.NotEqual, "!=", start));
                        break;
                    }
                    throw new CpqlParseException("Unexpected character '!' at position " + start);
                default:
                    throw new CpqlParseException("Unexpected character '" + c + "' at position " + start);
            }
        }

        tokens.Add(new CpqlToken(CpqlTokenKind.End, "", _pos));
        return tokens;
    }

    private void SkipWhitespace()
    {
        while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
            _pos++;
    }

    private string ReadIdentifier()
    {
        var start = _pos;
        while (_pos < _input.Length && (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] == '_'))
            _pos++;
        return _input.Substring(start, _pos - start);
    }

    private CpqlToken ReadString(int start)
    {
        _pos++;
        var sb = new System.Text.StringBuilder();
        while (_pos < _input.Length)
        {
            if (_input[_pos] == '\'')
            {
                _pos++;
                if (_pos < _input.Length && _input[_pos] == '\'')
                {
                    sb.Append('\'');
                    _pos++;
                    continue;
                }
                return new CpqlToken(CpqlTokenKind.StringLiteral, sb.ToString(), start);
            }
            sb.Append(_input[_pos++]);
        }
        throw new CpqlParseException("Unterminated string literal at position " + start);
    }

    private CpqlToken ReadNumber(int start)
    {
        while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || _input[_pos] == '.'))
            _pos++;
        return new CpqlToken(CpqlTokenKind.NumberLiteral, _input.Substring(start, _pos - start), start);
    }

    private bool Match(char c)
    {
        if (_pos < _input.Length && _input[_pos] == c)
        {
            _pos++;
            return true;
        }
        return false;
    }
}

internal sealed class CpqlParseException : Exception
{
    public CpqlParseException(string message) : base(message) { }
}
