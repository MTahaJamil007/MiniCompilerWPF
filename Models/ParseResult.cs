namespace MiniCompilerWPF.Models;

public class ParseResult
{
    public ParseResult(List<Statement> statements, string syntaxTree, List<Token> tokens)
    {
        Statements = statements;
        SyntaxTree = syntaxTree;
        Tokens = tokens;
    }

    public List<Statement> Statements { get; }
    public string SyntaxTree { get; }
    public List<Token> Tokens { get; }
}

public abstract record Statement;
public record BlockStatement(List<Statement> Statements) : Statement;
public record DeclarationStatement(string Type, string Name, Expression? Initializer) : Statement;
public record AssignmentStatement(string Name, Expression Value) : Statement;
public record IfStatement(Expression Condition, Statement ThenBranch, Statement? ElseBranch) : Statement;
public record WhileStatement(Expression Condition, Statement Body) : Statement;
public record ForStatement(Statement? Initializer, Expression? Condition, Statement? Increment, Statement Body) : Statement;
public record ReturnStatement(Expression? Value) : Statement;
public record ExpressionStatement(Expression Expression) : Statement;

public abstract record Expression;
public record LiteralExpression(string Value) : Expression;
public record IdentifierExpression(string Name) : Expression;
public record BinaryExpression(Expression Left, string Operator, Expression Right) : Expression;
public record UnaryExpression(string Operator, Expression Operand) : Expression;
public record CallExpression(string Function, List<Expression> Arguments) : Expression;
public record ArrayAccessExpression(string Array, Expression Index) : Expression;
