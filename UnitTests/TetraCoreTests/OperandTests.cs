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
public class OperandTests
{
    [Test]
    public void CheckCanCreateFloatOperand()
    {
        var op = new Operand(1.23f);

        Assert.That(op.Type, Is.EqualTo(OperandType.Float));
        Assert.That(op.Float, Is.EqualTo(1.23f).Within(0.0001f));
        Assert.That(op.ToString(), Is.EqualTo("1.23"));
    }

    [Test]
    public void CheckCanCreateIntOperand()
    {
        var op = new Operand(42);

        Assert.That(op.Type, Is.EqualTo(OperandType.Int));
        Assert.That(op.Int, Is.EqualTo(42));
        Assert.That(op.ToString(), Is.EqualTo("42"));
    }

    [Test]
    public void CheckCanCreateVectorOperand()
    {
        var op = new Operand(1.0f, 2.0f, 3.0f);

        Assert.That(op.Type, Is.EqualTo(OperandType.Vector));
        Assert.That(op.Length, Is.EqualTo(3));
        Assert.That(op.Floats, Is.EqualTo(new[] { 1.0f, 2.0f, 3.0f }));
        Assert.That(op.ToString(), Is.EqualTo("[1.0,2.0,3.0]"));
    }

    [Test]
    public void CheckCanCreateOperandFromSingleFloatArray()
    {
        var op = Operand.FromOperands([new Operand(2.5f)]);

        Assert.That(op.Type, Is.EqualTo(OperandType.Float));
        Assert.That(op.Float, Is.EqualTo(2.5f).Within(0.001));
    }

    [Test]
    public void CheckCanCreateOperandFromMultipleFloats()
    {
        var op = Operand.FromOperands([new Operand(1.0f), new Operand(2.0f)]);

        Assert.That(op.Type, Is.EqualTo(OperandType.Vector));
        Assert.That(op.Length, Is.EqualTo(2));
        Assert.That(op.Floats[0], Is.EqualTo(1.0f));
        Assert.That(op.Floats[1], Is.EqualTo(2.0f));
    }

    [Test]
    public void CheckThrowsIfNonFloatUsedInFromOperands()
    {
        Assert.That(() => Operand.FromOperands([new Operand(1.0f), new Operand { Type = OperandType.Label, Name = "label"}]), Throws.TypeOf<SyntaxErrorException>());
    }
}
