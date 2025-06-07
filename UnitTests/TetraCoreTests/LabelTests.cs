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

using TetraCore;
using TetraCore.Exceptions;

namespace UnitTests.TetraCoreTests;

[TestFixture]
public class LabelTests
{
    [Test]
    public void CheckAssemblingLabelDoesNotTreatAsInstruction()
    {
        const string code = "label:";
        var instructions = Assembler.Assemble(code).Instructions;

        Assert.That(instructions, Is.Empty);
    }

    [Test]
    public void CheckDefiningDuplicateLabelThrows()
    {
        const string code =
            """
            label:
            label:
            """;

        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void CheckLabelWithOperandThrows()
    {
        const string code = "label: 1";
        
        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void CheckLabelsAreResolved()
    {
        const string code =
            """
                ld $a, 23
            label:
                jmp label
            """;
        var instructions = Assembler.Assemble(code).Instructions;
        
        Assert.That(instructions.Count, Is.EqualTo(2));
        Assert.That(instructions[1].OpCode, Is.EqualTo(OpCode.Jmp));
        Assert.That(instructions[1].Operands.Single().Int, Is.EqualTo(1));
    }
}