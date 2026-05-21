namespace MiniCompilerWPF.CompilerCore;

public class CodeGenerator
{
    public List<string> Generate(List<string> optimizedIr)
    {
        List<string> asm = [];
        int reg = 1;
        foreach (var line in optimizedIr)
        {
            if (!line.Contains('=')) continue;
            var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
            asm.Add($"LOAD R{reg}, {parts[1]}");
            asm.Add($"STORE {parts[0]}, R{reg}");
            reg++;
        }
        return asm;
    }
}
