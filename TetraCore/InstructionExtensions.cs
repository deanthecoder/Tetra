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

public static class InstructionExtensions
{
    /// <summary>
    /// Return true if the instruction writes to a variable, without
    /// needing that variable as part of the operations.
    /// E.g. a = cos(b)  ('a' is not used in the input)
    /// </summary>
    public static bool IsDirectAssignment(this Instruction instruction)
    {
        switch (instruction.OpCode)
        {
            case OpCode.Ld:
            case OpCode.Sin:
            case OpCode.Sinh:
            case OpCode.Asin:
            case OpCode.Cos:
            case OpCode.Cosh:
            case OpCode.Acos:
            case OpCode.Tan:
            case OpCode.Tanh:
            case OpCode.Atan:
            case OpCode.Sqrt:
            case OpCode.Log:
            case OpCode.Exp:
            case OpCode.Abs:
            case OpCode.Sign:
            case OpCode.Floor:
            case OpCode.Ceil:
            case OpCode.Fract:
                return true;
            default:
                return false;
        }
    }
}