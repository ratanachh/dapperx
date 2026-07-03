namespace DapperX.Generator.Cpql;

internal enum CpqlTokenKind
{
    End,
    Identifier,
    Parameter,
    StringLiteral,
    NumberLiteral,
    LeftParen,
    RightParen,
    Comma,
    Dot,
    Colon,
    Star,
    Plus,
    Minus,
    Multiply,
    Divide,
    Equal,
    NotEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    Keyword,
}

internal sealed class CpqlToken
{
    public CpqlTokenKind Kind { get; }
    public string Text { get; }
    public int Position { get; }
    public string? Keyword { get; }

    public CpqlToken(CpqlTokenKind kind, string text, int position, string? keyword = null)
    {
        Kind = kind;
        Text = text;
        Position = position;
        Keyword = keyword;
    }
}
