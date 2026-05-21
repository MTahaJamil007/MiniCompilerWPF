using System.Windows;
using System.IO;
using System.Windows.Controls;
using Microsoft.Win32;
using MiniCompilerWPF.Models;
using MiniCompilerWPF.ViewModels;

namespace MiniCompilerWPF;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void RunAll_Click(object sender, RoutedEventArgs e)
    {
        _vm.RunAll();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _vm.SourceCode = string.Empty;
        _vm.SyntaxTree = string.Empty;
        _vm.IRText = string.Empty;
        _vm.OptimizedIRText = string.Empty;
        _vm.TargetCodeText = string.Empty;
        _vm.Tokens.Clear();
        _vm.Symbols.Clear();
        _vm.Errors.Clear();
    }

    private void TestCaseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TestCaseCombo.SelectedItem is TestCase testCase) _vm.SourceCode = testCase.SourceCode;
    }

    private void LoadFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Source Files (*.txt;*.c;*.mc)|*.txt;*.c;*.mc|All Files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            _vm.SourceCode = File.ReadAllText(dialog.FileName);
        }
    }

    private void ExportIr_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "IR Files (*.ir.txt)|*.ir.txt|All Files (*.*)|*.*",
            FileName = "ir.txt"
        };
        if (dialog.ShowDialog() == true)
        {
            File.WriteAllText(dialog.FileName, _vm.IRText);
        }
    }

    private void SourceCodeBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var caret = textBox.CaretIndex;
        var text = textBox.Text;
        var line = text.Take(caret).Count(c => c == '\n') + 1;
        var lastNewLine = text.LastIndexOf('\n', Math.Max(0, caret - 1));
        var col = caret - (lastNewLine >= 0 ? lastNewLine : -1);
        _vm.UpdateCaretPosition(line, col);
    }
}
