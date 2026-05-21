namespace MiniCompilerWPF.Models;

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Fatal
}

public enum ErrorPhase
{
    Lexer,
    Parser,
    Semantic,
    IRGen,
    Optimizer,
    CodeGen
}

public record CompilerError(
    ErrorPhase Phase,
    ErrorSeverity Severity,
    string Code,
    string Message,
    int Line,
    int Column,
    string? SourceSnippet = null,
    string? Suggestion = null
);
