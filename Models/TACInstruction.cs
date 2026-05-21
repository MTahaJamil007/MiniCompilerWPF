namespace MiniCompilerWPF.Models;

public enum TACOp
{
    Assign,
    BinaryOp,
    UnaryOp,
    Copy,
    Label,
    Goto,
    IfTrue,
    IfFalse,
    IfRelOp,
    Param,
    Call,
    Return,
    ArrayLoad,
    ArrayStore,
    AddrOf,
    Deref,
    DerefAssign,
    Cast,
    Nop
}

public record TACInstruction(
    TACOp Op,
    string? Result = null,
    string? Arg1 = null,
    string? Arg2 = null,
    string? Extra = null
)
{
    public override string ToString() => Op switch
    {
        TACOp.Label => $"{Result}:",
        TACOp.Assign => $"{Result} = {Arg1}",
        TACOp.Copy => $"{Result} = {Arg1}",
        TACOp.BinaryOp => $"{Result} = {Arg1} {Extra} {Arg2}",
        TACOp.UnaryOp => $"{Result} = {Extra}{Arg1}",
        TACOp.Goto => $"goto {Extra}",
        TACOp.IfTrue => $"if {Arg1} goto {Extra}",
        TACOp.IfFalse => $"ifFalse {Arg1} goto {Extra}",
        TACOp.IfRelOp => $"if {Arg1} {Extra} {Arg2} goto {Result}",
        TACOp.Param => $"param {Arg1}",
        TACOp.Call => Result != null ? $"{Result} = call {Arg1}, {Arg2}" : $"call {Arg1}, {Arg2}",
        TACOp.Return => Arg1 != null ? $"return {Arg1}" : "return",
        TACOp.ArrayLoad => $"{Result} = {Arg1}[{Arg2}]",
        TACOp.ArrayStore => $"{Arg1}[{Arg2}] = {Result}",
        TACOp.AddrOf => $"{Result} = &{Arg1}",
        TACOp.Deref => $"{Result} = *{Arg1}",
        TACOp.DerefAssign => $"*{Result} = {Arg1}",
        TACOp.Cast => $"{Result} = ({Extra}) {Arg1}",
        TACOp.Nop => $"// {Extra}",
        _ => $"? {Op}"
    };
}
