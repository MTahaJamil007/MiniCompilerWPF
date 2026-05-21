using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public class Parser
{
    public (string SyntaxTree, List<CompilerError> Errors) Parse(List<Token> tokens)
    {
        List<CompilerError> errors = [];
        var lines = new List<string> { "Program" };
        int i = 0;
        while (i < tokens.Count && tokens[i].Type != TokenType.EndOfFile)
        {
            if (IsType(tokens, i) && tokens.ElementAtOrDefault(i + 1)?.Type == TokenType.Identifier)
            {
                lines.Add($" ├── Declaration: {tokens[i].Lexeme} {tokens[i + 1].Lexeme}");
            }
            else if (tokens[i].Type == TokenType.Identifier && tokens.ElementAtOrDefault(i + 1)?.Lexeme == "=")
            {
                lines.Add($" ├── Assignment: {tokens[i].Lexeme}");
            }
            else if (tokens[i].Lexeme == "if") lines.Add(" ├── IfStatement");
            else if (tokens[i].Lexeme == "while") lines.Add(" ├── WhileStatement");
            i++;
        }

        int braceBalance = tokens.Count(t => t.Lexeme == "{") - tokens.Count(t => t.Lexeme == "}");
        if (braceBalance != 0) errors.Add(new("Parser", "Unbalanced braces"));
        if (tokens.Count(t => t.Lexeme == ";") == 0) errors.Add(new("Parser", "No statements terminated with semicolon"));

        return (string.Join(Environment.NewLine, lines), errors);
    }

    private static bool IsType(List<Token> tokens, int i) => tokens[i].Type == TokenType.Keyword && tokens[i].Lexeme is "int" or "float" or "string" or "bool";
}
