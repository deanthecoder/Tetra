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
using TetraCore.Exceptions;

namespace TetraCore;

/// <summary>
/// Represents a variable identifier used in the Tetra virtual machine.
/// Variables are identified by a numeric slot index and may optionally include a subscript index (e.g., for vector access).
/// </summary>
public class VarName
{
    public int Slot { get; }
    public int? ArrIndex { get; }
    public string Swizzle { get; }

    public VarName(string name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrWhiteSpace(name))
            throw new SyntaxErrorException("Variable name cannot be empty.");
        
        // Extract any swizzle component.
        if (name.Contains('.'))
        {
            var index = name.IndexOf('.');
            Swizzle = name[(index + 1)..];
            if (Swizzle.Contains('['))
                Swizzle = Swizzle[..Swizzle.IndexOf('[')];
            name = name.Replace($".{Swizzle}", string.Empty);
        }

        var bracketIndex = name.IndexOf('[');
        if (bracketIndex == -1)
        {
            // Simple variable name - No array index.
            Slot = ParseName(name);
            return;
        }
        
        // Variable name with array index.
        Slot = ParseName(name[..bracketIndex]);

        var closingBracketIndex = name.IndexOf(']', bracketIndex);
        if (closingBracketIndex == -1)
            throw new SyntaxErrorException($"Missing closing bracket in variable name: {name}");

        var indexPart = name[(bracketIndex + 1)..closingBracketIndex];
        if (!int.TryParse(indexPart, out var arrIndex))
            throw new SyntaxErrorException($"Invalid variable array subscript: {name}");

        ArrIndex = arrIndex;
    }
    
    private static int ParseName(string name)
    {
        if (!int.TryParse(name, out var slot))
            throw new SyntaxErrorException($"Invalid variable name: {name} (Must be a number)");
        if (slot >= ScopeFrame.MaxSlots)
            throw new SyntaxErrorException("Variable count limit reached.");
        return slot;
    }

    public static implicit operator VarName(string name) =>
        new VarName(name);

    public override bool Equals(object obj) =>
        obj is VarName other &&
        Slot == other.Slot &&
        ArrIndex == other.ArrIndex;

    public override int GetHashCode() =>
        HashCode.Combine(Slot, ArrIndex);

    public override string ToString() =>
        ToUiString();

    public string ToUiString(SymbolTable symbolTable = null)
    {
        var varName = $"${symbolTable?[Slot] ?? Slot.ToString()}";
        return varName + (Swizzle != null ? $".{Swizzle}" : string.Empty) + (ArrIndex.HasValue ? $"[{ArrIndex}]" : string.Empty);
    }
}