using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MiniCompilerWPF.CompilerCore;
using MiniCompilerWPF.Models;
using MiniCompilerWPF.Resources;

namespace MiniCompilerWPF.ViewModels;

public enum PhaseState
{
    NotRun,
    Success,
    Error
}

public class MainViewModel : INotifyPropertyChanged
{
    private string _sourceCode = "int x = 10;";
    private string _syntaxTree = string.Empty;
    private int _selectedTabIndex;
    private string _statusMessage = string.Empty;
    private int _tokenCount;
    private int _errorCount;
    private int _warningCount;
    private string _irText = string.Empty;
    private string _optimizedIrText = string.Empty;
    private string _targetCodeText = string.Empty;
    private string _optimizationReport = string.Empty;
    private int _currentLine = 1;
    private int _currentColumn = 1;

    public string SourceCode
    {
        get => _sourceCode;
        set
        {
            if (_sourceCode == value) return;
            _sourceCode = value;
            UpdateLineNumbers();
            OnPropertyChanged();
        }
    }

    public string SyntaxTree
    {
        get => _syntaxTree;
        set => SetField(ref _syntaxTree, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetField(ref _selectedTabIndex, value);
    }

    public ObservableCollection<int> LineNumbers { get; } = [];
    public ObservableCollection<Token> Tokens { get; } = [];
    public ObservableCollection<CompilerError> Errors { get; } = [];
    public ObservableCollection<SymbolInfo> Symbols { get; } = [];
    public List<TACInstruction> IR { get; } = [];
    public List<TACInstruction> OptimizedIR { get; } = [];
    public ObservableCollection<string> TargetCode { get; } = [];
    public List<TestCase> Cases => TestCases.All;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public int TokenCount
    {
        get => _tokenCount;
        set => SetField(ref _tokenCount, value);
    }

    public int ErrorCount
    {
        get => _errorCount;
        set => SetField(ref _errorCount, value);
    }

    public int WarningCount
    {
        get => _warningCount;
        set => SetField(ref _warningCount, value);
    }

    public string IRText
    {
        get => _irText;
        set => SetField(ref _irText, value);
    }

    public string OptimizedIRText
    {
        get => _optimizedIrText;
        set => SetField(ref _optimizedIrText, value);
    }

    public string TargetCodeText
    {
        get => _targetCodeText;
        set => SetField(ref _targetCodeText, value);
    }

    public string OptimizationReport
    {
        get => _optimizationReport;
        set => SetField(ref _optimizationReport, value);
    }

    public int CurrentLine
    {
        get => _currentLine;
        set => SetField(ref _currentLine, value);
    }

    public int CurrentColumn
    {
        get => _currentColumn;
        set => SetField(ref _currentColumn, value);
    }

    public ObservableCollection<PhaseState> PhaseStatuses { get; } =
    [
        PhaseState.NotRun,
        PhaseState.NotRun,
        PhaseState.NotRun,
        PhaseState.NotRun,
        PhaseState.NotRun,
        PhaseState.NotRun
    ];

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        UpdateLineNumbers();
    }

    public void RunAll()
    {
        Tokens.Clear();
        Errors.Clear();
        Symbols.Clear();
        IR.Clear();
        OptimizedIR.Clear();
        TargetCode.Clear();
        IRText = string.Empty;
        OptimizedIRText = string.Empty;
        TargetCodeText = string.Empty;
        OptimizationReport = string.Empty;
        SyntaxTree = string.Empty;
        ResetPhaseStatuses();

        var lexer = new Lexer();
        var parser = new Parser();
        var semantic = new SemanticAnalyzer();
        var irGen = new IRGenerator();
        var opt = new Optimizer();
        var gen = new TargetCodeGenerator();

        try
        {
            var (tokens, lexErrors) = lexer.Tokenize(SourceCode);
            foreach (var t in tokens) Tokens.Add(t);
            foreach (var e in lexErrors) Errors.Add(e);
            TokenCount = Tokens.Count;
            UpdatePhaseStatus(0, lexErrors.All(e => e.Severity != ErrorSeverity.Error && e.Severity != ErrorSeverity.Fatal));

            if (HasFatal(lexErrors))
            {
                FinalizeErrors();
                return;
            }

            var (parseResult, parseErrors) = parser.Parse(tokens, SourceCode);
            SyntaxTree = parseResult.SyntaxTree;
            foreach (var e in parseErrors) Errors.Add(e);
            UpdatePhaseStatus(1, parseErrors.All(e => e.Severity != ErrorSeverity.Error && e.Severity != ErrorSeverity.Fatal));

            var (symbols, semErrors) = semantic.Analyze(parseResult);
            foreach (var s in symbols) Symbols.Add(s);
            foreach (var e in semErrors) Errors.Add(e);
            UpdatePhaseStatus(2, semErrors.All(e => e.Severity != ErrorSeverity.Error && e.Severity != ErrorSeverity.Fatal));

            try
            {
                var ir = irGen.Generate(parseResult);
                IR.AddRange(ir);
                IRText = string.Join(Environment.NewLine, FormatTacLines(IR));
                UpdatePhaseStatus(3, true);
            }
            catch (Exception ex)
            {
                Errors.Add(new CompilerError(ErrorPhase.IRGen, ErrorSeverity.Fatal, "E999", ex.Message, 0, 0));
                UpdatePhaseStatus(3, false);
            }

            try
            {
                var result = opt.Optimize(IR.ToList());
                OptimizedIR.Clear();
                OptimizedIR.AddRange(result.Instructions);
                OptimizationReport = BuildOptimizationReport(result.Report);
                OptimizedIRText = OptimizationReport + Environment.NewLine + string.Join(Environment.NewLine, FormatTacLines(OptimizedIR));
                UpdatePhaseStatus(4, true);
            }
            catch (Exception ex)
            {
                Errors.Add(new CompilerError(ErrorPhase.Optimizer, ErrorSeverity.Fatal, "E999", ex.Message, 0, 0));
                UpdatePhaseStatus(4, false);
            }

            try
            {
                var target = gen.Generate(OptimizedIR.ToList());
                foreach (var line in target) TargetCode.Add(line);
                TargetCodeText = string.Join(Environment.NewLine, TargetCode);
                UpdatePhaseStatus(5, true);
            }
            catch (Exception ex)
            {
                Errors.Add(new CompilerError(ErrorPhase.CodeGen, ErrorSeverity.Fatal, "E999", ex.Message, 0, 0));
                UpdatePhaseStatus(5, false);
            }
        }
        catch (Exception ex)
        {
            Errors.Add(new CompilerError(ErrorPhase.Lexer, ErrorSeverity.Fatal, "E999", ex.Message, 0, 0));
            UpdatePhaseStatus(0, false);
        }
        finally
        {
            FinalizeErrors();
        }
    }

    public void UpdateCaretPosition(int line, int column)
    {
        CurrentLine = line;
        CurrentColumn = column;
    }

    private void UpdateLineNumbers()
    {
        LineNumbers.Clear();
        var count = SourceCode.Count(c => c == '\n') + 1;
        for (int i = 1; i <= count; i++) LineNumbers.Add(i);
    }

    private void ResetPhaseStatuses()
    {
        for (int i = 0; i < PhaseStatuses.Count; i++) PhaseStatuses[i] = PhaseState.NotRun;
        OnPropertyChanged(nameof(PhaseStatuses));
    }

    private void UpdatePhaseStatus(int index, bool success)
    {
        PhaseStatuses[index] = success ? PhaseState.Success : PhaseState.Error;
        OnPropertyChanged(nameof(PhaseStatuses));
    }

    private static bool HasFatal(IEnumerable<CompilerError> errors) => errors.Any(e => e.Severity == ErrorSeverity.Fatal);

    private void FinalizeErrors()
    {
        ErrorCount = Errors.Count(e => e.Severity is ErrorSeverity.Error or ErrorSeverity.Fatal);
        WarningCount = Errors.Count(e => e.Severity == ErrorSeverity.Warning);
    }

    private static string BuildOptimizationReport(OptimizationReport report)
    {
        return string.Join(Environment.NewLine,
        [
            "; === Optimization Report ===",
            $"; Constant folds   : {report.ConstantFolds}",
            $"; Constants prop   : {report.ConstantProps}",
            $"; Dead instrs elim : {report.DeadEliminated}",
            $"; CSE hits         : {report.CseHits}",
            "; ================================"
        ]);
    }

    private static IEnumerable<string> FormatTacLines(IEnumerable<TACInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            var text = instr.ToString();
            if (instr.Op == TACOp.Label || instr.Op == TACOp.Nop)
            {
                yield return text;
            }
            else
            {
                yield return $"    {text}";
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
    }
}
