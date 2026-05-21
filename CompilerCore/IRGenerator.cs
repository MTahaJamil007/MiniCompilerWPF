using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public class IRGenerator
{
    private int _tempCounter;
    private int _labelCounter;
    private readonly List<TACInstruction> _instructions = [];

    public List<TACInstruction> Generate(ParseResult parseResult)
    {
        _tempCounter = 0;
        _labelCounter = 0;
        _instructions.Clear();

        _instructions.Add(new TACInstruction(TACOp.Nop, Extra: "=== Function: main ==="));
        foreach (var statement in parseResult.Statements)
        {
            EmitStatement(statement);
        }
        return _instructions.ToList();
    }

    private void EmitStatement(Statement statement)
    {
        switch (statement)
        {
            case DeclarationStatement decl:
                if (decl.Initializer != null)
                {
                    var value = EmitExpression(decl.Initializer);
                    _instructions.Add(new TACInstruction(TACOp.Assign, decl.Name, value));
                }
                break;
            case AssignmentStatement assign:
                var rhs = EmitExpression(assign.Value);
                _instructions.Add(new TACInstruction(TACOp.Assign, assign.Name, rhs));
                break;
            case ExpressionStatement expr:
                EmitExpression(expr.Expression);
                break;
            case IfStatement ifs:
                EmitIfStatement(ifs);
                break;
            case WhileStatement wh:
                EmitWhileStatement(wh);
                break;
            case ForStatement fs:
                EmitForStatement(fs);
                break;
            case ReturnStatement ret:
                if (ret.Value != null)
                {
                    var value = EmitExpression(ret.Value);
                    _instructions.Add(new TACInstruction(TACOp.Return, Arg1: value));
                }
                else
                {
                    _instructions.Add(new TACInstruction(TACOp.Return));
                }
                break;
            case BlockStatement block:
                foreach (var stmt in block.Statements)
                {
                    EmitStatement(stmt);
                }
                break;
        }
    }

    private void EmitIfStatement(IfStatement ifs)
    {
        var thenLabel = NewLabel();
        var elseLabel = NewLabel();
        var endLabel = NewLabel();

        if (ifs.Condition is BinaryExpression { Operator: "<" or ">" or "<=" or ">=" or "==" or "!=" } rel)
        {
            var left = EmitExpression(rel.Left);
            var right = EmitExpression(rel.Right);
            _instructions.Add(new TACInstruction(TACOp.IfRelOp, thenLabel, left, right, rel.Operator));
            _instructions.Add(new TACInstruction(TACOp.Goto, Extra: elseLabel));
        }
        else
        {
            var cond = EmitExpression(ifs.Condition);
            _instructions.Add(new TACInstruction(TACOp.IfFalse, Arg1: cond, Extra: elseLabel));
        }

        _instructions.Add(new TACInstruction(TACOp.Label, thenLabel));
        EmitStatement(ifs.ThenBranch);
        _instructions.Add(new TACInstruction(TACOp.Goto, Extra: endLabel));
        _instructions.Add(new TACInstruction(TACOp.Label, elseLabel));
        if (ifs.ElseBranch != null)
        {
            EmitStatement(ifs.ElseBranch);
        }
        _instructions.Add(new TACInstruction(TACOp.Label, endLabel));
    }

    private void EmitWhileStatement(WhileStatement wh)
    {
        var startLabel = NewLabel();
        var bodyLabel = NewLabel();
        var endLabel = NewLabel();
        _instructions.Add(new TACInstruction(TACOp.Label, startLabel));

        if (wh.Condition is BinaryExpression { Operator: "<" or ">" or "<=" or ">=" or "==" or "!=" } rel)
        {
            var left = EmitExpression(rel.Left);
            var right = EmitExpression(rel.Right);
            _instructions.Add(new TACInstruction(TACOp.IfRelOp, bodyLabel, left, right, rel.Operator));
            _instructions.Add(new TACInstruction(TACOp.Goto, Extra: endLabel));
            _instructions.Add(new TACInstruction(TACOp.Label, bodyLabel));
        }
        else
        {
            var cond = EmitExpression(wh.Condition);
            _instructions.Add(new TACInstruction(TACOp.IfFalse, Arg1: cond, Extra: endLabel));
        }

        EmitStatement(wh.Body);
        _instructions.Add(new TACInstruction(TACOp.Goto, Extra: startLabel));
        _instructions.Add(new TACInstruction(TACOp.Label, endLabel));
    }

    private void EmitForStatement(ForStatement fs)
    {
        if (fs.Initializer != null) EmitStatement(fs.Initializer);
        var startLabel = NewLabel();
        var bodyLabel = NewLabel();
        var endLabel = NewLabel();
        _instructions.Add(new TACInstruction(TACOp.Label, startLabel));

        if (fs.Condition != null)
        {
            if (fs.Condition is BinaryExpression { Operator: "<" or ">" or "<=" or ">=" or "==" or "!=" } rel)
            {
                var left = EmitExpression(rel.Left);
                var right = EmitExpression(rel.Right);
                _instructions.Add(new TACInstruction(TACOp.IfRelOp, bodyLabel, left, right, rel.Operator));
                _instructions.Add(new TACInstruction(TACOp.Goto, Extra: endLabel));
                _instructions.Add(new TACInstruction(TACOp.Label, bodyLabel));
            }
            else
            {
                var cond = EmitExpression(fs.Condition);
                _instructions.Add(new TACInstruction(TACOp.IfFalse, Arg1: cond, Extra: endLabel));
            }
        }

        EmitStatement(fs.Body);
        if (fs.Increment != null) EmitStatement(fs.Increment);
        _instructions.Add(new TACInstruction(TACOp.Goto, Extra: startLabel));
        _instructions.Add(new TACInstruction(TACOp.Label, endLabel));
    }

    private string EmitExpression(Expression expression)
    {
        switch (expression)
        {
            case LiteralExpression lit:
                return lit.Value;
            case IdentifierExpression ident:
                return ident.Name;
            case UnaryExpression unary:
                var operand = EmitExpression(unary.Operand);
                var temp = NewTemp();
                _instructions.Add(new TACInstruction(TACOp.UnaryOp, temp, operand, null, unary.Operator));
                return temp;
            case BinaryExpression bin when bin.Operator == "=":
                if (bin.Left is IdentifierExpression identAssign)
                {
                    var value = EmitExpression(bin.Right);
                    _instructions.Add(new TACInstruction(TACOp.Assign, identAssign.Name, value));
                    return identAssign.Name;
                }
                if (bin.Left is ArrayAccessExpression arrAssign)
                {
                    var index = EmitExpression(arrAssign.Index);
                    var value = EmitExpression(bin.Right);
                    _instructions.Add(new TACInstruction(TACOp.ArrayStore, value, arrAssign.Array, index));
                    return value;
                }
                break;
            case BinaryExpression bin:
                var left = EmitExpression(bin.Left);
                var right = EmitExpression(bin.Right);
                var result = NewTemp();
                _instructions.Add(new TACInstruction(TACOp.BinaryOp, result, left, right, bin.Operator));
                return result;
            case CallExpression call:
                foreach (var arg in call.Arguments)
                {
                    var argValue = EmitExpression(arg);
                    _instructions.Add(new TACInstruction(TACOp.Param, Arg1: argValue));
                }
                var callResult = NewTemp();
                _instructions.Add(new TACInstruction(TACOp.Call, callResult, call.Function, call.Arguments.Count.ToString()));
                return callResult;
            case ArrayAccessExpression array:
                var indexValue = EmitExpression(array.Index);
                var arrayTemp = NewTemp();
                _instructions.Add(new TACInstruction(TACOp.ArrayLoad, arrayTemp, array.Array, indexValue));
                return arrayTemp;
        }

        return "0";
    }

    private string NewTemp() => $"t{_tempCounter++}";
    private string NewLabel() => $"L{_labelCounter++}";
}
