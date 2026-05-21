using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.CompilerCore;

public class CompilerResult
{
    public List<Token> Tokens { get; } = [];
    public string SyntaxTree { get; set; } = string.Empty;
    public List<CompilerError> Errors { get; } = [];
    public List<SymbolInfo> Symbols { get; } = [];
    public List<TACInstruction> IntermediateCode { get; } = [];
    public List<TACInstruction> OptimizedCode { get; } = [];
    public List<string> TargetCode { get; } = [];
}
