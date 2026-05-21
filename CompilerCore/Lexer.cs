using System.Text;
using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public class Lexer
{
    private static readonly HashSet<string> Keywords = ["int", "float", "string", "bool", "if", "while", "true", "false"];
    private static readonly HashSet<char> Punctuation = [';', '(', ')', '{', '}'];

    public (List<Token> Tokens, List<CompilerError> Errors) Tokenize(string source)
    {
        List<Token> tokens = [];
        List<CompilerError> errors = [];
        int line = 1, col = 1;

        for (int i = 0; i < source.Length;)
        {
            char c = source[i];
            if (c == '\n') { line++; col = 1; i++; continue; }
            if (char.IsWhiteSpace(c)) { i++; col++; continue; }

            if (char.IsLetter(c) || c == '_')
            {
                int start = i, sc = col;
                while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_')) { i++; col++; }
                string lex = source[start..i];
                tokens.Add(new Token(Keywords.Contains(lex) ? (lex is "true" or "false" ? TokenType.BooleanLiteral : TokenType.Keyword) : TokenType.Identifier, lex, line, sc));
                continue;
            }

            if (char.IsDigit(c))
            {
                int start = i, sc = col; bool dot = false;
                while (i < source.Length && (char.IsDigit(source[i]) || source[i] == '.'))
                {
                    if (source[i] == '.') { if (dot) errors.Add(new("Lexer", "Invalid number format", line, col)); dot = true; }
                    i++; col++;
                }
                tokens.Add(new Token(dot ? TokenType.FloatLiteral : TokenType.IntegerLiteral, source[start..i], line, sc));
                continue;
            }

            if (c == '"')
            {
                int sc = col; i++; col++; StringBuilder sb = new(); bool closed = false;
                while (i < source.Length)
                {
                    if (source[i] == '"') { closed = true; i++; col++; break; }
                    sb.Append(source[i]); i++; col++;
                }
                if (!closed) errors.Add(new("Lexer", "Unclosed string literal", line, sc));
                tokens.Add(new Token(TokenType.StringLiteral, sb.ToString(), line, sc));
                continue;
            }

            string two = i + 1 < source.Length ? source.Substring(i, 2) : string.Empty;
            if (new[] { "==", "!=", "<=", ">=" }.Contains(two)) { tokens.Add(new(TokenType.Operator, two, line, col)); i += 2; col += 2; continue; }
            if (new[] { '+', '-', '*', '/', '=', '<', '>' }.Contains(c)) { tokens.Add(new(TokenType.Operator, c.ToString(), line, col)); i++; col++; continue; }
            if (Punctuation.Contains(c)) { tokens.Add(new(TokenType.Punctuation, c.ToString(), line, col)); i++; col++; continue; }

            errors.Add(new("Lexer", $"Invalid character '{c}'", line, col)); i++; col++;
        }

        tokens.Add(new(TokenType.EndOfFile, "EOF", line, col));
        return (tokens, errors);
    }
}
