using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public static class ErrorRegistry
{
    private static readonly Dictionary<string, (string Message, string Suggestion)> Registry = new()
    {
        ["E001"] = ("Unexpected character '{0}'", "Remove or escape the character."),
        ["E002"] = ("Unterminated string literal", "Add closing quote."),
        ["E003"] = ("Unterminated block comment", "Add closing */."),
        ["E004"] = ("Invalid numeric literal '{0}'", "Check number format."),
        ["W001"] = ("Unreachable code after return", "Remove dead code."),
        ["W002"] = ("Variable '{0}' declared but never used", "Remove declaration or use the variable."),
        ["W003"] = ("Implicit type conversion from {0} to {1}", "Add explicit cast."),
        ["E101"] = ("Expected '{0}', found '{1}' at line {2}", "Check syntax."),
        ["E102"] = ("Unexpected token '{0}'", "Possibly missing semicolon or brace."),
        ["E103"] = ("Mismatched parentheses", "Check opening and closing parentheses."),
        ["E104"] = ("Mismatched braces", "Check opening and closing braces."),
        ["E105"] = ("Missing semicolon after statement", "Add semicolon."),
        ["E201"] = ("Undeclared identifier '{0}'", "Declare the variable before use."),
        ["E202"] = ("Variable '{0}' used before assignment", "Initialize the variable."),
        ["E203"] = ("Type mismatch: cannot assign {0} to {1}", "Check types."),
        ["E204"] = ("Duplicate declaration of '{0}'", "Remove duplicate or use different name."),
        ["E205"] = ("Function '{0}' called with wrong number of arguments ({1} expected, {2} given).", ""),
        ["E301"] = ("Division by zero detected at compile time", "Check divisor.")
    };

    public static CompilerError Make(string code, ErrorPhase phase, int line, int col, string sourceSnippet, params object[] args)
    {
        if (!Registry.TryGetValue(code, out var entry))
        {
            return new CompilerError(phase, ErrorSeverity.Error, code, string.Format(code, args), line, col, sourceSnippet, null);
        }

        var message = string.Format(entry.Message, args);
        var severity = code.StartsWith('W') ? ErrorSeverity.Warning : ErrorSeverity.Error;
        return new CompilerError(phase, severity, code, message, line, col, sourceSnippet, entry.Suggestion);
    }
}
