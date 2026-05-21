using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public class SemanticAnalyzer
{
    public (List<SymbolInfo> Symbols, List<CompilerError> Errors) Analyze(ParseResult parseResult)
    {
        Dictionary<string, (string Type, bool Assigned, bool Used, string Value)> symbols = [];
        List<CompilerError> errors = [];

        foreach (var statement in parseResult.Statements)
        {
            AnalyzeStatement(statement, symbols, errors, parseResult);
        }

        foreach (var (name, data) in symbols)
        {
            if (!data.Used)
            {
                errors.Add(ErrorRegistry.Make("W002", ErrorPhase.Semantic, 0, 0, string.Empty, name));
            }
        }

        var symbolList = symbols.Select(kvp => new SymbolInfo(kvp.Key, kvp.Value.Type, "global", kvp.Value.Value, kvp.Value.Used)).ToList();
        return (symbolList, errors);
    }

    private static void AnalyzeStatement(Statement statement, Dictionary<string, (string Type, bool Assigned, bool Used, string Value)> symbols, List<CompilerError> errors, ParseResult parseResult)
    {
        switch (statement)
        {
            case DeclarationStatement decl:
                if (symbols.ContainsKey(decl.Name))
                {
                    errors.Add(ErrorRegistry.Make("E204", ErrorPhase.Semantic, 0, 0, string.Empty, decl.Name));
                    break;
                }
                var value = decl.Initializer is LiteralExpression lit ? lit.Value : "-";
                symbols[decl.Name] = (decl.Type, decl.Initializer != null, false, value);
                if (decl.Initializer != null)
                {
                    AnalyzeExpression(decl.Initializer, symbols, errors);
                    if (decl.Initializer is LiteralExpression literal && !IsLiteralCompatible(decl.Type, literal.Value))
                    {
                        errors.Add(ErrorRegistry.Make("E203", ErrorPhase.Semantic, 0, 0, string.Empty, literal.Value, decl.Type));
                    }
                }
                break;
            case AssignmentStatement assign:
                if (!symbols.TryGetValue(assign.Name, out var info))
                {
                    errors.Add(ErrorRegistry.Make("E201", ErrorPhase.Semantic, 0, 0, string.Empty, assign.Name));
                    break;
                }
                AnalyzeExpression(assign.Value, symbols, errors);
                symbols[assign.Name] = (info.Type, true, info.Used, info.Value);
                break;
            case IfStatement ifs:
                AnalyzeExpression(ifs.Condition, symbols, errors);
                AnalyzeStatement(ifs.ThenBranch, symbols, errors, parseResult);
                if (ifs.ElseBranch != null) AnalyzeStatement(ifs.ElseBranch, symbols, errors, parseResult);
                break;
            case WhileStatement wh:
                AnalyzeExpression(wh.Condition, symbols, errors);
                AnalyzeStatement(wh.Body, symbols, errors, parseResult);
                break;
            case ForStatement fs:
                if (fs.Initializer != null) AnalyzeStatement(fs.Initializer, symbols, errors, parseResult);
                if (fs.Condition != null) AnalyzeExpression(fs.Condition, symbols, errors);
                if (fs.Increment != null) AnalyzeStatement(fs.Increment, symbols, errors, parseResult);
                AnalyzeStatement(fs.Body, symbols, errors, parseResult);
                break;
            case ReturnStatement ret:
                if (ret.Value != null) AnalyzeExpression(ret.Value, symbols, errors);
                break;
            case BlockStatement block:
                foreach (var stmt in block.Statements)
                {
                    AnalyzeStatement(stmt, symbols, errors, parseResult);
                }
                break;
            case ExpressionStatement expr:
                AnalyzeExpression(expr.Expression, symbols, errors);
                break;
        }
    }

    private static void AnalyzeExpression(Expression expression, Dictionary<string, (string Type, bool Assigned, bool Used, string Value)> symbols, List<CompilerError> errors)
    {
        switch (expression)
        {
            case IdentifierExpression ident:
                if (!symbols.TryGetValue(ident.Name, out var info))
                {
                    errors.Add(ErrorRegistry.Make("E201", ErrorPhase.Semantic, 0, 0, string.Empty, ident.Name));
                }
                else
                {
                    if (!info.Assigned)
                    {
                        errors.Add(ErrorRegistry.Make("E202", ErrorPhase.Semantic, 0, 0, string.Empty, ident.Name));
                    }
                    symbols[ident.Name] = (info.Type, info.Assigned, true, info.Value);
                }
                break;
            case BinaryExpression bin:
                AnalyzeExpression(bin.Left, symbols, errors);
                AnalyzeExpression(bin.Right, symbols, errors);
                break;
            case UnaryExpression unary:
                AnalyzeExpression(unary.Operand, symbols, errors);
                break;
            case CallExpression call:
                foreach (var arg in call.Arguments) AnalyzeExpression(arg, symbols, errors);
                break;
            case ArrayAccessExpression array:
                if (!symbols.TryGetValue(array.Array, out var infoArray))
                {
                    errors.Add(ErrorRegistry.Make("E201", ErrorPhase.Semantic, 0, 0, string.Empty, array.Array));
                }
                else
                {
                    symbols[array.Array] = (infoArray.Type, infoArray.Assigned, true, infoArray.Value);
                }
                AnalyzeExpression(array.Index, symbols, errors);
                break;
        }
    }

    private static bool IsLiteralCompatible(string type, string literal)
    {
        return type switch
        {
            "int" => int.TryParse(literal, out _),
            "float" => double.TryParse(literal, out _),
            "bool" => bool.TryParse(literal, out _),
            "string" => true,
            _ => true
        };
    }
}
