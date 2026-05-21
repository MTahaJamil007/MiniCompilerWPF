using System.Collections.ObjectModel;
using MiniCompilerWPF.CompilerCore;
using MiniCompilerWPF.Models;
using MiniCompilerWPF.Resources;

namespace MiniCompilerWPF.ViewModels;

public class MainViewModel
{
    public string SourceCode { get; set; } = "int x = 10;";
    public ObservableCollection<Token> Tokens { get; } = [];
    public ObservableCollection<CompilerError> Errors { get; } = [];
    public ObservableCollection<SymbolInfo> Symbols { get; } = [];
    public ObservableCollection<string> IR { get; } = [];
    public ObservableCollection<string> OptimizedIR { get; } = [];
    public ObservableCollection<string> TargetCode { get; } = [];
    public string SyntaxTree { get; set; } = string.Empty;
    public List<TestCase> Cases => TestCases.All;

    public void RunAll()
    {
        Tokens.Clear(); Errors.Clear(); Symbols.Clear(); IR.Clear(); OptimizedIR.Clear(); TargetCode.Clear();
        var lexer = new Lexer();
        var parser = new Parser();
        var semantic = new SemanticAnalyzer();
        var irGen = new IntermediateCodeGenerator();
        var opt = new IROptimizer();
        var gen = new CodeGenerator();

        var (tokens, lexErrors) = lexer.Tokenize(SourceCode);
        foreach (var t in tokens) Tokens.Add(t);
        foreach (var e in lexErrors) Errors.Add(e);

        var (tree, parseErrors) = parser.Parse(tokens);
        SyntaxTree = tree;
        foreach (var e in parseErrors) Errors.Add(e);

        var (symbols, semErrors) = semantic.Analyze(tokens);
        foreach (var s in symbols) Symbols.Add(s);
        foreach (var e in semErrors) Errors.Add(e);

        foreach (var line in irGen.Generate(tokens)) IR.Add(line);
        foreach (var line in opt.Optimize(IR.ToList())) OptimizedIR.Add(line);
        foreach (var line in gen.Generate(OptimizedIR.ToList())) TargetCode.Add(line);
    }
}
