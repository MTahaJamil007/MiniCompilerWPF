using MiniCompilerWPF.Models;

namespace MiniCompilerWPF.Resources;

public static class TestCases
{
    public static List<TestCase> All =>
    [
        new("Hello Variables", "int a = 5;\nint b = 3;\nint c = a + b;"),
        new("If-Else Branch", "int x = 10;\nif (x > 5) {\n x = x - 1;\n} else {\n x = x + 1;\n}"),
        new("While Loop", "int i = 1;\nint acc = 1;\nwhile (i < 5) {\n acc = acc * i;\n i = i + 1;\n}"),
        new("For Loop", "int i = 0;\nfor (i = 0; i < 4; i = i + 1) {\n int x = i * 2;\n}"),
        new("Function Call", "int add(int x, int y) {\n return x + y;\n}\nint main() {\n int v = add(2, 3);\n return v;\n}"),
        new("Nested If", "int x = 3;\nif (x > 1) {\n if (x < 5) {\n  x = x + 1;\n } else {\n  x = x - 1;\n }\n}"),
        new("Dead Code", "int x = 1;\nreturn x;\nx = x + 1;"),
        new("Constant Expression", "int x = 3 * 4 + 2 * 0;"),
        new("Error Case 1", "int x = 5\nint y = 2;"),
        new("Error Case 2", "x = 4;"),
        new("Error Case 3", "int a = ;\nif (a > ) {\n b = a + 1;\n}")
    ];
}
