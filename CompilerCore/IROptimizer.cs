namespace MiniCompilerWPF.CompilerCore;

public class IROptimizer
{
    public List<string> Optimize(List<string> ir)
    {
        return ir.Select(line => line.Replace(" + 0", "").Replace(" * 1", "")).Distinct().ToList();
    }
}
