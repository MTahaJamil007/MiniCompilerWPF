namespace MiniCompilerWPF.Models;

public record Token(TokenType Type, string Lexeme, int Line, int Column)
{
    public string Value => Lexeme;
}
