using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public record OptimizationReport(int ConstantFolds, int ConstantProps, int DeadEliminated, int CseHits);
public record OptimizationResult(List<TACInstruction> Instructions, OptimizationReport Report);

public class Optimizer
{
    public OptimizationResult Optimize(List<TACInstruction> input)
    {
        var instructions = input.Select(i => i with { }).ToList();
        int constantFolds = 0;
        int constantProps = 0;
        int deadElims = 0;
        int cseHits = 0;

        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            if (instr.Op == TACOp.BinaryOp && IsNumeric(instr.Arg1) && IsNumeric(instr.Arg2))
            {
                var left = double.Parse(instr.Arg1!);
                var right = double.Parse(instr.Arg2!);
                var result = instr.Extra switch
                {
                    "+" => left + right,
                    "-" => left - right,
                    "*" => left * right,
                    "/" => right == 0 ? double.NaN : left / right,
                    "%" => left % right,
                    _ => double.NaN
                };
                instructions[i] = new TACInstruction(TACOp.Copy, instr.Result, result.ToString());
                constantFolds++;
            }
            else if (instr.Op == TACOp.UnaryOp && IsNumeric(instr.Arg1))
            {
                var value = double.Parse(instr.Arg1!);
                var result = instr.Extra switch
                {
                    "-" => -value,
                    _ => value
                };
                instructions[i] = new TACInstruction(TACOp.Copy, instr.Result, result.ToString());
                constantFolds++;
            }
        }

        var constants = new Dictionary<string, string>();
        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            if (instr.Arg1 != null && constants.TryGetValue(instr.Arg1, out var replacement1))
            {
                instr = instr with { Arg1 = replacement1 };
                constantProps++;
            }
            if (instr.Arg2 != null && constants.TryGetValue(instr.Arg2, out var replacement2))
            {
                instr = instr with { Arg2 = replacement2 };
                constantProps++;
            }

            if ((instr.Op == TACOp.Copy || instr.Op == TACOp.Assign) && instr.Result != null && IsLiteral(instr.Arg1))
            {
                constants[instr.Result] = instr.Arg1!;
            }
            else if (instr.Result != null)
            {
                constants.Remove(instr.Result);
            }

            instructions[i] = instr;
        }

        var copies = new Dictionary<string, string>();
        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            if (instr.Arg1 != null && copies.TryGetValue(instr.Arg1, out var copy1))
            {
                instr = instr with { Arg1 = copy1 };
            }
            if (instr.Arg2 != null && copies.TryGetValue(instr.Arg2, out var copy2))
            {
                instr = instr with { Arg2 = copy2 };
            }

            if ((instr.Op == TACOp.Copy || instr.Op == TACOp.Assign) && instr.Result != null && instr.Arg1 != null && !IsLiteral(instr.Arg1))
            {
                copies[instr.Result] = instr.Arg1;
            }
            else if (instr.Result != null)
            {
                copies.Remove(instr.Result);
            }

            instructions[i] = instr;
        }

        var used = new HashSet<string>();
        foreach (var instr in instructions)
        {
            if (instr.Arg1 != null) used.Add(instr.Arg1);
            if (instr.Arg2 != null) used.Add(instr.Arg2);
            if (instr.Op == TACOp.Return && instr.Arg1 != null) used.Add(instr.Arg1);
        }

        instructions = instructions.Where(instr =>
        {
            if ((instr.Op == TACOp.Assign || instr.Op == TACOp.BinaryOp || instr.Op == TACOp.UnaryOp || instr.Op == TACOp.Copy) && instr.Result != null && IsTemp(instr.Result) && !used.Contains(instr.Result))
            {
                deadElims++;
                return false;
            }
            return true;
        }).ToList();

        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            if (instr.Op == TACOp.BinaryOp)
            {
                if (instr.Extra == "*" && (instr.Arg1 == "1" || instr.Arg2 == "1"))
                {
                    var copy = instr.Arg1 == "1" ? instr.Arg2 : instr.Arg1;
                    instructions[i] = new TACInstruction(TACOp.Copy, instr.Result, copy);
                }
                else if (instr.Extra == "*" && (instr.Arg1 == "0" || instr.Arg2 == "0"))
                {
                    instructions[i] = new TACInstruction(TACOp.Copy, instr.Result, "0");
                }
                else if (instr.Extra == "+" && (instr.Arg1 == "0" || instr.Arg2 == "0"))
                {
                    var copy = instr.Arg1 == "0" ? instr.Arg2 : instr.Arg1;
                    instructions[i] = new TACInstruction(TACOp.Copy, instr.Result, copy);
                }
                else if (instr.Extra == "-" && instr.Arg2 == "0")
                {
                    instructions[i] = new TACInstruction(TACOp.Copy, instr.Result, instr.Arg1);
                }
                else if (instr.Extra == "/" && instr.Arg2 == "1")
                {
                    instructions[i] = new TACInstruction(TACOp.Copy, instr.Result, instr.Arg1);
                }
                else if (instr.Extra == "-" && instr.Arg1 == instr.Arg2)
                {
                    instructions[i] = new TACInstruction(TACOp.Copy, instr.Result, "0");
                }
            }
        }

        var reachable = new List<TACInstruction>();
        bool skipping = false;
        foreach (var instr in instructions)
        {
            if (instr.Op == TACOp.Label)
            {
                skipping = false;
                reachable.Add(instr);
                continue;
            }
            if (skipping) continue;
            reachable.Add(instr);
            if (instr.Op == TACOp.Goto || instr.Op == TACOp.Return)
            {
                skipping = true;
            }
        }
        instructions = reachable;

        var exprMap = new Dictionary<string, string>();
        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            if (instr.Op == TACOp.BinaryOp && instr.Arg1 != null && instr.Arg2 != null)
            {
                var key = $"{instr.Arg1} {instr.Extra} {instr.Arg2}";
                if (exprMap.TryGetValue(key, out var existing))
                {
                    instructions[i] = new TACInstruction(TACOp.Copy, instr.Result, existing);
                    cseHits++;
                }
                else
                {
                    exprMap[key] = instr.Result ?? string.Empty;
                }
            }
            if (instr.Result != null)
            {
                var keysToRemove = exprMap.Where(kvp => kvp.Value == instr.Result).Select(kvp => kvp.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    exprMap.Remove(key);
                }
            }
        }

        var report = new OptimizationReport(constantFolds, constantProps, deadElims, cseHits);
        return new OptimizationResult(instructions, report);
    }

    private static bool IsLiteral(string? value) => value != null && (IsNumeric(value) || bool.TryParse(value, out _));
    private static bool IsNumeric(string? value) => value != null && double.TryParse(value, out _);
    private static bool IsTemp(string value) => value.StartsWith("t") && value.Length > 1 && int.TryParse(value[1..], out _);
}
