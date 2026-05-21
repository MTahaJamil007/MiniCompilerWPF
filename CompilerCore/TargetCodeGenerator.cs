using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public class TargetCodeGenerator
{
    private const int RegisterCount = 8;

    public List<string> Generate(List<TACInstruction> instructions)
    {
        List<string> lines = [".text"];
        string? currentFunction = null;

        foreach (var instr in instructions)
        {
            if (instr.Op == TACOp.Nop && instr.Extra != null && instr.Extra.Contains("Function"))
            {
                if (currentFunction != null)
                {
                    lines.Add($"{currentFunction} ENDP");
                }
                currentFunction = ExtractFunctionName(instr.Extra);
                lines.Add($"{currentFunction} PROC");
                continue;
            }

            switch (instr.Op)
            {
                case TACOp.Label:
                    lines.Add($"{instr.Result}:");
                    break;
                case TACOp.Assign:
                case TACOp.Copy:
                    EmitMove(lines, instr.Result, instr.Arg1);
                    break;
                case TACOp.BinaryOp:
                    EmitBinary(lines, instr.Result, instr.Arg1, instr.Arg2, instr.Extra);
                    break;
                case TACOp.UnaryOp:
                    EmitUnary(lines, instr.Result, instr.Arg1, instr.Extra);
                    break;
                case TACOp.Goto:
                    lines.Add($"    JMP {instr.Extra}");
                    break;
                case TACOp.IfFalse:
                    EmitCompareZero(lines, instr.Arg1, instr.Extra);
                    break;
                case TACOp.IfRelOp:
                    EmitRelational(lines, instr.Arg1, instr.Arg2, instr.Extra, instr.Result);
                    break;
                case TACOp.Param:
                    EmitParam(lines, instr.Arg1);
                    break;
                case TACOp.Call:
                    EmitCall(lines, instr.Result, instr.Arg1);
                    break;
                case TACOp.Return:
                    EmitReturn(lines, instr.Arg1);
                    break;
            }
        }

        if (currentFunction != null)
        {
            lines.Add($"{currentFunction} ENDP");
        }

        return lines;
    }

    private static string ExtractFunctionName(string extra)
    {
        var parts = extra.Split(':');
        if (parts.Length > 1)
        {
            return parts[1].Trim().Replace("===", string.Empty).Trim();
        }
        return "main";
    }

    private static void EmitMove(List<string> lines, string? result, string? arg1)
    {
        if (result == null || arg1 == null) return;
        if (IsTemp(result))
        {
            var dest = TempRegister(result);
            var source = ResolveOperand(lines, arg1, dest);
            lines.Add($"    MOV {dest}, {source}");
        }
        else
        {
            var scratch = "R7";
            var source = ResolveOperand(lines, arg1, scratch);
            lines.Add($"    MOV {scratch}, {source}");
            lines.Add($"    MOV [{result}], {scratch}");
        }
    }

    private static void EmitBinary(List<string> lines, string? result, string? arg1, string? arg2, string? op)
    {
        if (result == null || arg1 == null || arg2 == null || op == null) return;
        var dest = IsTemp(result) ? TempRegister(result) : "R7";
        var left = ResolveOperand(lines, arg1, dest);
        lines.Add($"    MOV {dest}, {left}");
        var right = ResolveOperand(lines, arg2, "R6");
        var instr = op switch
        {
            "+" => "ADD",
            "-" => "SUB",
            "*" => "MUL",
            "/" => "DIV",
            _ => "ADD"
        };
        lines.Add($"    {instr} {dest}, {dest}, {right}");
        if (!IsTemp(result))
        {
            lines.Add($"    MOV [{result}], {dest}");
        }
    }

    private static void EmitUnary(List<string> lines, string? result, string? arg1, string? op)
    {
        if (result == null || arg1 == null || op == null) return;
        var dest = IsTemp(result) ? TempRegister(result) : "R7";
        var operand = ResolveOperand(lines, arg1, dest);
        lines.Add($"    MOV {dest}, {operand}");
        if (op == "-")
        {
            lines.Add($"    NEG {dest}");
        }
        else if (op == "!")
        {
            lines.Add($"    CMP {dest}, #0");
            lines.Add($"    MOV {dest}, #0");
            lines.Add($"    SETE {dest}");
        }
        if (!IsTemp(result))
        {
            lines.Add($"    MOV [{result}], {dest}");
        }
    }

    private static void EmitCompareZero(List<string> lines, string? arg1, string? label)
    {
        if (arg1 == null || label == null) return;
        var reg = ResolveOperand(lines, arg1, "R7");
        lines.Add($"    CMP {reg}, #0");
        lines.Add($"    JE {label}");
    }

    private static void EmitRelational(List<string> lines, string? arg1, string? arg2, string? relop, string? label)
    {
        if (arg1 == null || arg2 == null || relop == null || label == null) return;
        var left = ResolveOperand(lines, arg1, "R7");
        var right = ResolveOperand(lines, arg2, "R6");
        lines.Add($"    CMP {left}, {right}");
        var jump = relop switch
        {
            "<" => "JL",
            ">" => "JG",
            "<=" => "JLE",
            ">=" => "JGE",
            "==" => "JE",
            "!=" => "JNE",
            _ => "JE"
        };
        lines.Add($"    {jump} {label}");
    }

    private static void EmitParam(List<string> lines, string? arg1)
    {
        if (arg1 == null) return;
        var reg = ResolveOperand(lines, arg1, "R7");
        lines.Add($"    PUSH {reg}");
    }

    private static void EmitCall(List<string> lines, string? result, string? function)
    {
        if (function == null) return;
        lines.Add($"    CALL {function}");
        if (result == null) return;
        if (IsTemp(result))
        {
            lines.Add($"    MOV {TempRegister(result)}, R0");
        }
        else
        {
            lines.Add($"    MOV [{result}], R0");
        }
    }

    private static void EmitReturn(List<string> lines, string? arg1)
    {
        if (arg1 != null)
        {
            var reg = ResolveOperand(lines, arg1, "R0");
            lines.Add($"    MOV R0, {reg}");
        }
        lines.Add("    RET");
    }

    private static string ResolveOperand(List<string> lines, string operand, string register)
    {
        if (IsLiteral(operand))
        {
            return $"#{operand}";
        }
        if (IsTemp(operand))
        {
            return TempRegister(operand);
        }
        lines.Add($"    MOV {register}, [{operand}]");
        return register;
    }

    private static bool IsTemp(string value) => value.StartsWith("t") && int.TryParse(value[1..], out _);
    private static string TempRegister(string temp)
    {
        var index = int.Parse(temp[1..]) % RegisterCount;
        return $"R{index}";
    }

    private static bool IsLiteral(string value) => double.TryParse(value, out _) || bool.TryParse(value, out _);
}
