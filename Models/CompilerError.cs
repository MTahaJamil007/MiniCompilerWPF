namespace MiniCompilerWPF.Models;

public record CompilerError(string Phase, string Message, int Line = 0, int Column = 0);
