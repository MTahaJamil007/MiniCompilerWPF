namespace MiniCompilerWPF.CompilerCore;

public record Token(TokenType Type, string Lexeme, int Line, int Column);
