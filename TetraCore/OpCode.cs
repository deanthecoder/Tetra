// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

namespace TetraCore;

public enum OpCode
{
    Nop,
    Decl,
    Ld,
    Halt,
    Add,
    Sub,
    Inc,
    Dec,
    Neg,
    Mul,
    Div,
    Shiftl,
    Shiftr,
    BitAnd,
    BitOr,
    Lt,
    Le,
    Gt,
    Ge,
    Eq,
    Ne,
    And,
    Or,
    Not,
    Test,
    Jmp,
    Jmpz,
    Jmpnz,
    Print,
    PushFrame,
    PopFrame,
    Call,
    Ret,
    
    // Intrinsics.
    Intrinsic,
    Sin,
    Sinh,
    Asin,
    Cos,
    Cosh,
    Acos,
    Tan,
    Tanh,
    Atan,
    Pow,
    Sqrt,
    Exp,
    Log,
    Abs,
    Sign,
    Mod,
    Min,
    Max,
    Clamp,
    Mix,
    Smoothstep,
    Reflect,
    Refract,
    Normalize,
    Length,
    Dot,
    Ceil,
    Floor,
    Fract,
    Cross
}