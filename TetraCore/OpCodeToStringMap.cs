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
    private static readonly (string asString, OpCode opCode)[] Lut;

    static OpCodeToStringMap()
    {
        // Special case names (Cannot be based on enum name).
        var manualEntries = new[]
        {
            ("jmp_z", OpCode.JmpZ),
            ("jmp_nz", OpCode.JmpNz),
            ("push_frame", OpCode.PushFrame),
            ("pop_frame", OpCode.PopFrame),
            ("bit_and", OpCode.BitAnd),
            ("bit_or", OpCode.BitOr)
        };

        // Auto-populate the LUT based on the lower-case enum name.
        var autoEntries =
            Enum.GetValues<OpCode>()
                .Where(op => manualEntries.All(e => e.Item2 != op))
                .Where(op => op.ToString().Count(char.IsUpper) == 1)
                .Select(op => (op.ToString().ToLower(), op));
        Lut = manualEntries.Concat(autoEntries).ToArray();

        // Check all OpCodes are represented in the map.
        var missing = Enum.GetValues<OpCode>().Where(o => Lut.All(l => l.opCode != o)).ToArray();
        if (missing.Length > 0)
            throw new Exception($"OpCodeToStringMap is missing entries for {string.Join(", ", missing)}");
    }
    
    public static string GetString(OpCode opCode) =>
        Lut.First(o => o.opCode == opCode).asString;

    /// <summary>
    /// Case insensitive lookup of an opcode from plain text.
    /// </summary>
    public static OpCode? GetOpCode(string opCode)
    {
        if (Lut.All(o => !o.asString.Equals(opCode, StringComparison.OrdinalIgnoreCase)))
            return null; // Instruction not found.
        return Lut.First(o => o.asString.Equals(opCode, StringComparison.OrdinalIgnoreCase)).opCode;
    }

    public static OpCode? GetIntrinsic(string name)
    {
        var opCode = GetOpCode(name);
        return (int)(opCode ?? 0) > (int)OpCode.Intrinsic ? opCode : null;
    }
}