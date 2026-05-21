namespace MiniCompilerWPF.CompilerCore;

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
