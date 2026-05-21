using System.Text;
using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public class Parser
{
    private readonly List<CompilerError> _errors = [];
    private List<Token> _tokens = [];
    private int _current;
    private string _source = string.Empty;

    public (ParseResult Result, List<CompilerError> Errors) Parse(List<Token> tokens, string source)
    {
        _tokens = tokens;
        _source = source;
        _current = 0;
        _errors.Clear();

        List<Statement> statements = [];
        while (!IsAtEnd())
        {
            var stmt = DeclarationOrStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }

        var syntaxTree = BuildSyntaxTree(statements);
        var braceBalance = tokens.Count(t => t.Lexeme == "{") - tokens.Count(t => t.Lexeme == "}");
        if (braceBalance != 0)
        {
            _errors.Add(ErrorRegistry.Make("E104", ErrorPhase.Parser, 0, 0, string.Empty));
        }
        var parenBalance = tokens.Count(t => t.Lexeme == "(") - tokens.Count(t => t.Lexeme == ")");
        if (parenBalance != 0)
        {
            _errors.Add(ErrorRegistry.Make("E103", ErrorPhase.Parser, 0, 0, string.Empty));
        }
        return (new ParseResult(statements, syntaxTree, tokens), _errors.ToList());
    }

    private Statement? DeclarationOrStatement()
    {
        try
        {
            if (MatchKeyword("int", "float", "string", "bool", "void"))
            {
                return ParseDeclaration(Previous().Lexeme);
            }

            return ParseStatement();
        }
        catch
        {
            Synchronize();
            return null;
        }
    }

    private Statement ParseDeclaration(string type)
    {
        var name = Consume(TokenType.Identifier, "E101", "identifier");
        Expression? initializer = null;
        if (MatchLexeme("="))
        {
            initializer = ParseExpression();
        }
        ConsumeLexeme(";", "E105");
        return new DeclarationStatement(type, name.Lexeme, initializer);
    }

    private Statement ParseStatement()
    {
        if (MatchKeyword("if")) return ParseIfStatement();
        if (MatchKeyword("while")) return ParseWhileStatement();
        if (MatchKeyword("for")) return ParseForStatement();
        if (MatchKeyword("return")) return ParseReturnStatement();
        if (MatchLexeme("{")) return ParseBlockStatement();
        return ParseExpressionStatement();
    }

    private Statement ParseIfStatement()
    {
        ConsumeLexeme("(", "E101", "(");
        var condition = ParseExpression();
        ConsumeLexeme(")", "E101", ")");
        var thenBranch = ParseStatement();
        Statement? elseBranch = null;
        if (MatchKeyword("else"))
        {
            elseBranch = ParseStatement();
        }
        return new IfStatement(condition, thenBranch, elseBranch);
    }

    private Statement ParseWhileStatement()
    {
        ConsumeLexeme("(", "E101", "(");
        var condition = ParseExpression();
        ConsumeLexeme(")", "E101", ")");
        var body = ParseStatement();
        return new WhileStatement(condition, body);
    }

    private Statement ParseForStatement()
    {
        ConsumeLexeme("(", "E101", "(");
        Statement? initializer = null;
        if (!CheckLexeme(";"))
        {
            if (MatchKeyword("int", "float", "string", "bool", "void"))
            {
                initializer = ParseDeclaration(Previous().Lexeme);
            }
            else
            {
                initializer = ParseExpressionStatement();
            }
        }
        else
        {
            Advance();
        }

        Expression? condition = null;
        if (!CheckLexeme(";"))
        {
            condition = ParseExpression();
        }
        ConsumeLexeme(";", "E105");

        Statement? increment = null;
        if (!CheckLexeme(")"))
        {
            var expr = ParseExpression();
            increment = new ExpressionStatement(expr);
        }
        ConsumeLexeme(")", "E101", ")");
        var body = ParseStatement();
        return new ForStatement(initializer, condition, increment, body);
    }

    private Statement ParseReturnStatement()
    {
        Expression? value = null;
        if (!CheckLexeme(";"))
        {
            value = ParseExpression();
        }
        ConsumeLexeme(";", "E105");
        return new ReturnStatement(value);
    }

    private Statement ParseBlockStatement()
    {
        List<Statement> statements = [];
        while (!CheckLexeme("}") && !IsAtEnd())
        {
            var stmt = DeclarationOrStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }
        ConsumeLexeme("}", "E104", "}");
        return new BlockStatement(statements);
    }

    private Statement ParseExpressionStatement()
    {
        var expr = ParseExpression();
        ConsumeLexeme(";", "E105");
        if (expr is BinaryExpression { Operator: "=", Left: IdentifierExpression ident })
        {
            return new AssignmentStatement(ident.Name, ((BinaryExpression)expr).Right);
        }
        return new ExpressionStatement(expr);
    }

    private Expression ParseExpression() => ParseAssignment();

    private Expression ParseAssignment()
    {
        var expr = ParseEquality();
        if (MatchLexeme("="))
        {
            var value = ParseAssignment();
            if (expr is IdentifierExpression identifier)
            {
                return new BinaryExpression(identifier, "=", value);
            }
            Error("E102", Previous(), Previous().Lexeme);
        }
        return expr;
    }

    private Expression ParseEquality()
    {
        var expr = ParseComparison();
        while (MatchLexeme("==", "!="))
        {
            var op = Previous().Lexeme;
            var right = ParseComparison();
            expr = new BinaryExpression(expr, op, right);
        }
        return expr;
    }

    private Expression ParseComparison()
    {
        var expr = ParseTerm();
        while (MatchLexeme("<", ">", "<=", ">="))
        {
            var op = Previous().Lexeme;
            var right = ParseTerm();
            expr = new BinaryExpression(expr, op, right);
        }
        return expr;
    }

    private Expression ParseTerm()
    {
        var expr = ParseFactor();
        while (MatchLexeme("+", "-"))
        {
            var op = Previous().Lexeme;
            var right = ParseFactor();
            expr = new BinaryExpression(expr, op, right);
        }
        return expr;
    }

    private Expression ParseFactor()
    {
        var expr = ParseUnary();
        while (MatchLexeme("*", "/", "%"))
        {
            var op = Previous().Lexeme;
            var right = ParseUnary();
            expr = new BinaryExpression(expr, op, right);
        }
        return expr;
    }

    private Expression ParseUnary()
    {
        if (MatchLexeme("-", "!", "~"))
        {
            var op = Previous().Lexeme;
            var right = ParseUnary();
            return new UnaryExpression(op, right);
        }
        return ParsePrimary();
    }

    private Expression ParsePrimary()
    {
        if (MatchType(TokenType.IntegerLiteral, TokenType.FloatLiteral, TokenType.StringLiteral, TokenType.BooleanLiteral))
        {
            return new LiteralExpression(Previous().Lexeme);
        }

        if (MatchType(TokenType.Identifier))
        {
            var name = Previous().Lexeme;
            if (MatchLexeme("("))
            {
                List<Expression> args = [];
                if (!CheckLexeme(")"))
                {
                    do
                    {
                        args.Add(ParseExpression());
                    } while (MatchLexeme(","));
                }
                ConsumeLexeme(")", "E101", ")");
                return new CallExpression(name, args);
            }

            if (MatchLexeme("["))
            {
                var index = ParseExpression();
                ConsumeLexeme("]", "E101", "]");
                return new ArrayAccessExpression(name, index);
            }

            return new IdentifierExpression(name);
        }

        if (MatchLexeme("("))
        {
            var expr = ParseExpression();
            ConsumeLexeme(")", "E103", ")");
            return expr;
        }

        Error("E102", Peek(), Peek().Lexeme);
        throw new InvalidOperationException("Unexpected token.");
    }

    private void Synchronize()
    {
        Advance();
        while (!IsAtEnd())
        {
            if (Previous().Lexeme == ";") return;
            if (Previous().Lexeme is "}" or ")") return;
            if (Peek().Type == TokenType.Keyword) return;
            Advance();
        }
    }

    private Token Consume(TokenType type, string code, string expected)
    {
        if (CheckType(type)) return Advance();
        Error(code, Peek(), expected, Peek().Lexeme, Peek().Line);
        throw new InvalidOperationException();
    }

    private void ConsumeLexeme(string lexeme, string code, string? expected = null)
    {
        if (CheckLexeme(lexeme))
        {
            Advance();
            return;
        }
        Error(code, Peek(), expected ?? lexeme, Peek().Lexeme, Peek().Line);
        throw new InvalidOperationException();
    }

    private void Error(string code, Token token, params object[] args)
    {
        _errors.Add(ErrorRegistry.Make(code, ErrorPhase.Parser, token.Line, token.Column, GetLineSnippet(token.Line), args));
    }

    private bool MatchLexeme(params string[] lexemes)
    {
        foreach (var lexeme in lexemes)
        {
            if (CheckLexeme(lexeme))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool MatchKeyword(params string[] keywords)
    {
        if (Peek().Type != TokenType.Keyword) return false;
        if (!keywords.Contains(Peek().Lexeme)) return false;
        Advance();
        return true;
    }

    private bool MatchType(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (CheckType(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool CheckLexeme(string lexeme) => !IsAtEnd() && Peek().Lexeme == lexeme;

    private bool CheckType(TokenType type) => !IsAtEnd() && Peek().Type == type;

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool IsAtEnd() => Peek().Type == TokenType.EndOfFile;

    private Token Peek() => _tokens[_current];

    private Token Previous() => _tokens[_current - 1];

    private string GetLineSnippet(int line)
    {
        var lines = _source.Replace("\r", string.Empty).Split('\n');
        return line - 1 < lines.Length && line - 1 >= 0 ? lines[line - 1] : string.Empty;
    }

    private static string BuildSyntaxTree(IEnumerable<Statement> statements)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Program");
        foreach (var stmt in statements)
        {
            BuildStatement(builder, stmt, " ├── ");
        }
        return builder.ToString().TrimEnd();
    }

    private static void BuildStatement(StringBuilder builder, Statement statement, string prefix)
    {
        switch (statement)
        {
            case DeclarationStatement decl:
                builder.AppendLine($"{prefix}Declaration: {decl.Type} {decl.Name}");
                if (decl.Initializer != null)
                {
                    BuildExpression(builder, decl.Initializer, prefix + "    ");
                }
                break;
            case AssignmentStatement assign:
                builder.AppendLine($"{prefix}Assignment: {assign.Name}");
                BuildExpression(builder, assign.Value, prefix + "    ");
                break;
            case IfStatement ifs:
                builder.AppendLine($"{prefix}IfStatement");
                BuildExpression(builder, ifs.Condition, prefix + "    ");
                BuildStatement(builder, ifs.ThenBranch, prefix + "    ");
                if (ifs.ElseBranch != null)
                {
                    BuildStatement(builder, ifs.ElseBranch, prefix + "    ");
                }
                break;
            case WhileStatement wh:
                builder.AppendLine($"{prefix}WhileStatement");
                BuildExpression(builder, wh.Condition, prefix + "    ");
                BuildStatement(builder, wh.Body, prefix + "    ");
                break;
            case ForStatement fs:
                builder.AppendLine($"{prefix}ForStatement");
                if (fs.Initializer != null) BuildStatement(builder, fs.Initializer, prefix + "    ");
                if (fs.Condition != null) BuildExpression(builder, fs.Condition, prefix + "    ");
                if (fs.Increment != null) BuildStatement(builder, fs.Increment, prefix + "    ");
                BuildStatement(builder, fs.Body, prefix + "    ");
                break;
            case ReturnStatement ret:
                builder.AppendLine($"{prefix}Return");
                if (ret.Value != null) BuildExpression(builder, ret.Value, prefix + "    ");
                break;
            case BlockStatement block:
                builder.AppendLine($"{prefix}Block");
                foreach (var stmt in block.Statements)
                {
                    BuildStatement(builder, stmt, prefix + "    ");
                }
                break;
            case ExpressionStatement expr:
                builder.AppendLine($"{prefix}Expression");
                BuildExpression(builder, expr.Expression, prefix + "    ");
                break;
        }
    }

    private static void BuildExpression(StringBuilder builder, Expression expression, string prefix)
    {
        switch (expression)
        {
            case LiteralExpression lit:
                builder.AppendLine($"{prefix}Literal: {lit.Value}");
                break;
            case IdentifierExpression id:
                builder.AppendLine($"{prefix}Identifier: {id.Name}");
                break;
            case BinaryExpression bin:
                builder.AppendLine($"{prefix}Binary: {bin.Operator}");
                BuildExpression(builder, bin.Left, prefix + "    ");
                BuildExpression(builder, bin.Right, prefix + "    ");
                break;
            case UnaryExpression un:
                builder.AppendLine($"{prefix}Unary: {un.Operator}");
                BuildExpression(builder, un.Operand, prefix + "    ");
                break;
            case CallExpression call:
                builder.AppendLine($"{prefix}Call: {call.Function}");
                foreach (var arg in call.Arguments)
                {
                    BuildExpression(builder, arg, prefix + "    ");
                }
                break;
            case ArrayAccessExpression arr:
                builder.AppendLine($"{prefix}ArrayAccess: {arr.Array}");
                BuildExpression(builder, arr.Index, prefix + "    ");
                break;
        }
    }
}
