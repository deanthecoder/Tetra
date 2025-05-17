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

namespace UnitTests;

[TestFixture]
public class ScopeFrameTests
{
    private ScopeFrame m_scopeFrame;

    [SetUp]
    public void Setup() =>
        m_scopeFrame = new ScopeFrame();

    [Test]
    public void CheckGettingMissingVariableThrows()
    {
        Assert.That(() => m_scopeFrame.GetVariable("test"), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckSettingVariable()
    {
        var operand = new Operand { Type = OperandType.Integer, IntValue = 23 };
        m_scopeFrame.SetVariable("test", operand);
        
        Assert.That(m_scopeFrame.GetVariable("test").Type, Is.EqualTo(OperandType.Integer));
        Assert.That(m_scopeFrame.GetVariable("test").IntValue, Is.EqualTo(23));
    }

    [Test]
    public void CheckOverwritingVariable()
    {
        var operand1 = new Operand { Type = OperandType.Integer, IntValue = 23 };
        m_scopeFrame.SetVariable("test", operand1);
        
        var operand2 = new Operand { Type = OperandType.Integer, IntValue = 42 };
        m_scopeFrame.SetVariable("test", operand2);
        
        Assert.That(m_scopeFrame.GetVariable("test").Type, Is.EqualTo(OperandType.Integer));
        Assert.That(m_scopeFrame.GetVariable("test").IntValue, Is.EqualTo(42));
    }

    [Test]
    public void CheckQueryingMissingVariable()
    {
        Assert.That(m_scopeFrame.IsDefined("test"), Is.False);
    }

    [Test]
    public void CheckQueryingExistingVariable()
    {
        var operand = new Operand { Type = OperandType.Integer, IntValue = 23 };
        m_scopeFrame.SetVariable("test", operand);
        
        Assert.That(m_scopeFrame.IsDefined("test"), Is.True);
    }
}