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

public static class OpCodeToStringMap
{
    private static readonly (string asString, OpCode opCode)[] Lut =
    {
        ("ld", OpCode.Ld),
        ("halt", OpCode.Halt),
        ("add", OpCode.Add),
        ("sub", OpCode.Sub),
        ("mul", OpCode.Mul),
        ("div", OpCode.Div),
        ("inc", OpCode.Inc),
        ("dec", OpCode.Dec),
        ("jmp", OpCode.Jmp),
        ("jmp_eq", OpCode.JmpEq),
        ("jmp_ne", OpCode.JmpNe),
        ("jmp_lt", OpCode.JmpLt),
        ("jmp_le", OpCode.JmpLe),
        ("jmp_gt", OpCode.JmpGt),
        ("jmp_ge", OpCode.JmpGe),
        ("print", OpCode.Print)
    };

    static OpCodeToStringMap()
    {
        // Check all OpCodes are represented in the map.
        var opCodes = Enum.GetValues<OpCode>().Where(o => Lut.All(l => l.opCode != o)).ToArray();
        if (opCodes.Any())
            throw new Exception($"OpCodeToStringMap is missing entries for {string.Join(", ", opCodes)}");
    }
    
    public static string GetString(OpCode opCode) =>
        Lut.First(o => o.opCode == opCode).asString;

    public static OpCode? GetOpCode(string opCode)
    {
        if (Lut.All(o => o.asString != opCode))
            return null;
        return Lut.First(o => o.asString == opCode).opCode;
    }
}