using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public class SemanticAnalyzer
{
    public (List<SymbolInfo> Symbols, List<CompilerError> Errors) Analyze(List<Token> tokens)
    {
        Dictionary<string, SymbolInfo> table = [];
        List<CompilerError> errors = [];
        int memory = 1000;

        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (tokens[i].Type == TokenType.Keyword && tokens[i].Lexeme is "int" or "float" or "string" or "bool")
            {
                var type = tokens[i].Lexeme;
                if (tokens[i + 1].Type != TokenType.Identifier) continue;
                string name = tokens[i + 1].Lexeme;
                if (table.ContainsKey(name)) errors.Add(new("Semantic", $"Duplicate declaration of variable '{name}'", tokens[i + 1].Line, tokens[i + 1].Column));
                else
                {
                    string value = "-";
                    if (tokens.ElementAtOrDefault(i + 2)?.Lexeme == "=") value = tokens.ElementAtOrDefault(i + 3)?.Lexeme ?? "-";
                    table[name] = new SymbolInfo(name, type, "global", value, memory);
                    memory += 4;
                }
            }

            if (tokens[i].Type == TokenType.Identifier && tokens.ElementAtOrDefault(i + 1)?.Lexeme == "=" && !table.ContainsKey(tokens[i].Lexeme))
                errors.Add(new("Semantic", $"Variable '{tokens[i].Lexeme}' is not declared", tokens[i].Line, tokens[i].Column));
        }

        return (table.Values.ToList(), errors);
    }
}
