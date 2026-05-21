using System.Text;
using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public class Lexer
{
    private static readonly HashSet<string> Keywords = ["int", "float", "string", "bool", "if", "else", "while", "for", "return", "true", "false", "void"];
    private static readonly HashSet<char> Punctuation = [';', '(', ')', '{', '}', '[', ']', ','];

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

            if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                while (i < source.Length && source[i] != '\n') { i++; col++; }
                continue;
            }

            if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
            {
                int sc = col;
                i += 2;
                col += 2;
                bool closed = false;
                while (i + 1 < source.Length)
                {
                    if (source[i] == '*' && source[i + 1] == '/')
                    {
                        i += 2;
                        col += 2;
                        closed = true;
                        break;
                    }
                    if (source[i] == '\n') { line++; col = 1; i++; continue; }
                    i++;
                    col++;
                }
                if (!closed)
                {
                    errors.Add(ErrorRegistry.Make("E003", ErrorPhase.Lexer, line, sc, GetLineSnippet(source, line)));
                }
                continue;
            }

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
                    if (source[i] == '.')
                    {
                        if (dot)
                        {
                            errors.Add(ErrorRegistry.Make("E004", ErrorPhase.Lexer, line, col, GetLineSnippet(source, line), source[start..(i + 1)]));
                        }
                        dot = true;
                    }
                    i++;
                    col++;
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
                    if (source[i] == '\n') { line++; col = 1; sb.Append('\n'); i++; continue; }
                    sb.Append(source[i]); i++; col++;
                }
                if (!closed)
                {
                    errors.Add(ErrorRegistry.Make("E002", ErrorPhase.Lexer, line, sc, GetLineSnippet(source, line)));
                }
                tokens.Add(new Token(TokenType.StringLiteral, sb.ToString(), line, sc));
                continue;
            }

            string two = i + 1 < source.Length ? source.Substring(i, 2) : string.Empty;
            if (new[] { "==", "!=", "<=", ">=", "&&", "||" }.Contains(two))
            {
                tokens.Add(new(TokenType.Operator, two, line, col));
                i += 2;
                col += 2;
                continue;
            }
            if (new[] { '+', '-', '*', '/', '=', '<', '>', '%', '!', '~' }.Contains(c))
            {
                tokens.Add(new(TokenType.Operator, c.ToString(), line, col));
                i++;
                col++;
                continue;
            }
            if (Punctuation.Contains(c)) { tokens.Add(new(TokenType.Punctuation, c.ToString(), line, col)); i++; col++; continue; }

            errors.Add(ErrorRegistry.Make("E001", ErrorPhase.Lexer, line, col, GetLineSnippet(source, line), c));
            i++;
            col++;
        }

        tokens.Add(new(TokenType.EndOfFile, "EOF", line, col));
        return (tokens, errors);
    }

    private static string GetLineSnippet(string source, int line)
    {
        var lines = source.Replace("\r", string.Empty).Split('\n');
        return line - 1 < lines.Length && line - 1 >= 0 ? lines[line - 1] : string.Empty;
    }
}
