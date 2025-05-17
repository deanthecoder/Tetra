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
using OpCode = TetraCore.OpCode;

namespace UnitTests;

[TestFixture]
public class AssemblerTests
{
    [Test]
    public void GivenNullCodeCheckAssemblingThrows()
    {
        Assert.That(() => Assembler.Assemble(null), Throws.ArgumentNullException);
    }

    [Test]
    public void ParseSingleInstruction()
    {
        const string code = "ld $a, 1.0";
        var instructions = Assembler.Assemble(code);

        Assert.That(instructions, Has.Length.EqualTo(1));
        var instr = instructions[0];
        Assert.That(instr.LineNumber, Is.EqualTo(1));
        Assert.That(instr.OpCode, Is.EqualTo(OpCode.Ld));
        Assert.That(instr.Operands, Has.Length.EqualTo(2));

        AssertOperand(instr.Operands[0], OperandType.Variable, "$a");
        Assert.That(instr.Operands[0].Name, Is.EqualTo("a"));

        AssertOperand(instr.Operands[1], OperandType.Float, "1.0");
        Assert.That(instr.Operands[1].FloatValue, Is.EqualTo(1.0).Within(0.001));
    }
    
    [Test]
    public void ParseCodeWithComments()
    {
        const string code = """
                            # This is a comment
                            ld $a, 1.0  # Another comment.
                            """;
        var instructions = Assembler.Assemble(code);

        Assert.That(instructions, Has.Length.EqualTo(1));
        var instr = instructions[0];
        Assert.That(instr.LineNumber, Is.EqualTo(2));
        Assert.That(instr.OpCode, Is.EqualTo(OpCode.Ld));
        Assert.That(instr.Operands, Has.Length.EqualTo(2));

        AssertOperand(instr.Operands[0], OperandType.Variable, "$a");
        Assert.That(instr.Operands[0].Name, Is.EqualTo("a"));

        AssertOperand(instr.Operands[1], OperandType.Float, "1.0");
        Assert.That(instr.Operands[1].FloatValue, Is.EqualTo(1.0).Within(0.001));
    }

    [Test]
    public void CheckParsingUnknownInstructionThrows()
    {
        const string code = "unknown $a, 1.0";

        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void CheckParsingInstructionWithInvalidOperandCountThrows()
    {
        Assert.That(() => Assembler.Assemble("ld $a"), Throws.TypeOf<SyntaxErrorException>());
        Assert.That(() => Assembler.Assemble("ld $a, $b, 1.2"), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void CheckParsingInstructionWithInvalidOperandTypeThrows()
    {
        Assert.That(() => Assembler.Assemble("ld 1.2, $a"), Throws.TypeOf<SyntaxErrorException>());
    }

    private static void AssertOperand(Operand op, OperandType expectedType, string expectedRaw = null) =>
        Assert.Multiple(() =>
        {
            Assert.That(op.Type, Is.EqualTo(expectedType));
            if (expectedRaw != null)
                Assert.That(op.Raw, Is.EqualTo(expectedRaw));
        });
}