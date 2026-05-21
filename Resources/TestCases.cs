using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.Resources;

public static class TestCases
{
    public static List<TestCase> All =>
    [
        new("Test 1: Valid Declaration", "int x = 10;\nfloat y = 20.5;"),
        new("Test 2: Assignment and Expression", "int x = 10;\nint y = x + 5;"),
        new("Test 3: Duplicate Declaration", "int x = 10;\nfloat x = 20.5;"),
        new("Test 4: Undeclared Variable", "int x = 10;\ny = x + 5;"),
        new("Test 5: Type Mismatch", "int x = \"hello\";"),
        new("Test 6: If Statement", "int x = 10;\nif (x > 5) {\n x = x + 1;\n}"),
        new("Test 7: While Loop", "int x = 0;\nwhile (x < 5) {\n x = x + 1;\n}")
    ];
}
