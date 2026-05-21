using System.Windows;
using System.Windows.Controls;
using MiniCompilerWPF.ViewModels;

namespace MiniCompilerWPF;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        TestCaseCombo.ItemsSource = _vm.Cases;
        TestCaseCombo.DisplayMemberPath = "Name";
        SourceCodeBox.Text = _vm.SourceCode;
    }

    private void RunAll_Click(object sender, RoutedEventArgs e)
    {
        _vm.SourceCode = SourceCodeBox.Text;
        _vm.RunAll();
        TokensGrid.ItemsSource = _vm.Tokens;
        SymbolGrid.ItemsSource = _vm.Symbols;
        ErrorGrid.ItemsSource = _vm.Errors;
        SyntaxBox.Text = _vm.SyntaxTree;
        IrBox.Text = string.Join(Environment.NewLine, _vm.IR);
        OptIrBox.Text = string.Join(Environment.NewLine, _vm.OptimizedIR);
        TargetBox.Text = string.Join(Environment.NewLine, _vm.TargetCode);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        SourceCodeBox.Clear();
        SyntaxBox.Clear(); IrBox.Clear(); OptIrBox.Clear(); TargetBox.Clear();
        TokensGrid.ItemsSource = null; SymbolGrid.ItemsSource = null; ErrorGrid.ItemsSource = null;
    }

    private void TestCaseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TestCaseCombo.SelectedItem is Models.TestCase testCase) SourceCodeBox.Text = testCase.SourceCode;
    }
}
