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
using System.Text.RegularExpressions;
using TetraCore.Exceptions;

namespace TetraCore;

public partial class VarName
{
    public string Name { get; }
    public int? ArrIndex { get; }

    public VarName(string name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrWhiteSpace(name))
            throw new SyntaxErrorException("Variable name cannot be empty.");

        // Capture name and optional '[]' brackets.
        var match = MyRegex().Match(name);
        if (!match.Success)
            throw new SyntaxErrorException($"Invalid variable name: {name}");

        Name = match.Groups[1].Value;
        ArrIndex = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : null;
    }

    public static implicit operator VarName(string name) => new VarName(name);

    public override bool Equals(object obj) =>
        obj is VarName other &&
        Name == other.Name &&
        ArrIndex == other.ArrIndex;

    public override int GetHashCode() =>
        HashCode.Combine(Name, ArrIndex);

    public override string ToString() =>
        Name + (ArrIndex.HasValue ? $"[{ArrIndex}]" : string.Empty);
    
    [GeneratedRegex(@"^([a-zA-Z_][a-zA-Z_0-9]*)(\[(\d+)\])?$")]
    private static partial Regex MyRegex();
}