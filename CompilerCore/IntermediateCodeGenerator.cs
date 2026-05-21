namespace MiniCompilerWPF.CompilerCore;

public class IntermediateCodeGenerator
{
    public List<string> Generate(List<Token> tokens)
    {
        List<string> ir = [];
        for (int i = 0; i < tokens.Count - 3; i++)
        {
            if ((tokens[i].Type == TokenType.Identifier || tokens[i].Type == TokenType.Keyword) && tokens[i + 1].Type == TokenType.Identifier && tokens[i + 2].Lexeme == "=")
                ir.Add($"{tokens[i + 1].Lexeme} = {tokens[i + 3].Lexeme}");
            if (tokens[i].Type == TokenType.Identifier && tokens[i + 1].Lexeme == "=" && tokens[i + 3].Type == TokenType.Operator)
                ir.Add($"t{i} = {tokens[i + 2].Lexeme} {tokens[i + 3].Lexeme} {tokens[i + 4].Lexeme}\n{tokens[i].Lexeme} = t{i}");
        }
        return ir;
    }
}
