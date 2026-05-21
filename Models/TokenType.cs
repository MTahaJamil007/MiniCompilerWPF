namespace MiniCompilerWPF.Models;

public enum TokenType
{
    Keyword,
    Identifier,
    IntegerLiteral,
    FloatLiteral,
    StringLiteral,
    BooleanLiteral,
    Operator,
    Punctuation,
    EndOfFile,
    Unknown
}
