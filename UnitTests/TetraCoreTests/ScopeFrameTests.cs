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
public class ScopeFrameTests
{
    private ScopeFrame m_scopeFrame;

    [SetUp]
    public void Setup() =>
        m_scopeFrame = new ScopeFrame();

    [Test]
    public void CheckGettingMissingVariableThrows()
    {
        Assert.That(() => m_scopeFrame.GetVariable("1"), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckSettingUndefinedVariableThrows()
    {
        var operand = new Operand(23);
        Assert.That(() => m_scopeFrame.SetVariable("23", operand), Throws.TypeOf<RuntimeException>());   
    }

    [Test]
    public void CheckSettingVariable()
    {
        var operand = new Operand(23);
        m_scopeFrame.DefineVariable("12", operand);
        
        Assert.That(m_scopeFrame.GetVariable("12").Type, Is.EqualTo(OperandType.Int));
        Assert.That(m_scopeFrame.GetVariable("12").Int, Is.EqualTo(23));
    }

    [Test]
    public void CheckOverwritingVariable()
    {
        var operand1 = new Operand(23);
        m_scopeFrame.DefineVariable("12", operand1);
        
        var operand2 = new Operand(42);
        m_scopeFrame.SetVariable("12", operand2);
        
        Assert.That(m_scopeFrame.GetVariable("12").Type, Is.EqualTo(OperandType.Int));
        Assert.That(m_scopeFrame.GetVariable("12").Int, Is.EqualTo(42));
    }

    [Test]
    public void CheckQueryingMissingVariable()
    {
        Assert.That(m_scopeFrame.IsDefined("21"), Is.False);
    }

    [Test]
    public void CheckQueryingExistingVariable()
    {
        var operand = new Operand(23);
        m_scopeFrame.DefineVariable("11", operand);
        
        Assert.That(m_scopeFrame.IsDefined("11"), Is.True);
    }

    [Test]
    public void CheckQueryingExistingVariableInParentScope()
    {
        var operand = new Operand(23);
        m_scopeFrame.DefineVariable("31", operand);
        var localScopeFrame = new ScopeFrame(m_scopeFrame);
        
        Assert.That(localScopeFrame.IsDefined("31"), Is.True);
        Assert.That(localScopeFrame.GetVariable("31").Int, Is.EqualTo(23));
    }

    [Test]
    public void GivenSoloFrameCheckIsRoot()
    {
        Assert.That(m_scopeFrame.IsRoot, Is.True);
    }
    
    [Test]
    public void GivenParentFrameCheckIsNotRoot()
    {
        var parentScopeFrame = new ScopeFrame();
        var localScopeFrame = new ScopeFrame(parentScopeFrame);
        
        Assert.That(localScopeFrame.IsRoot, Is.False);
    }
}