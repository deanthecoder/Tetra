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
public class VarNameTests
{
    [Test]
    public void GivenNullNameCheckConstructionThrows()
    {
        Assert.That(() => new VarName(null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void GivenEmptyStringCheckConstructionThrows()
    {
        Assert.That(() => new VarName(string.Empty), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void GivenWhitespaceCheckConstructionThrows()
    {
        Assert.That(() => new VarName(" "), Throws.TypeOf<SyntaxErrorException>());
        Assert.That(() => new VarName("\t"), Throws.TypeOf<SyntaxErrorException>());
        Assert.That(() => new VarName("\n"), Throws.TypeOf<SyntaxErrorException>());
        Assert.That(() => new VarName("\r"), Throws.TypeOf<SyntaxErrorException>());
        Assert.That(() => new VarName("\r\n"), Throws.TypeOf<SyntaxErrorException>());
    }
    
    [Test]
    public void GivenValidNameWhenParsedShouldParseCorrectly()
    {
        var v = new VarName("23");
        Assert.That(v.Slot, Is.EqualTo(23));
        Assert.That(v.ArrIndex, Is.Null);
    }

    [Test]
    public void GivenValidIndexedNameWhenParsedShouldParseNameAndIndex()
    {
        var v = new VarName("10[3]");
        Assert.That(v.Slot, Is.EqualTo(10));
        Assert.That(v.ArrIndex, Is.EqualTo(3));
    }

    [Test]
    public void GivenNameWithDollarPrefixCheckConstructionThrows()
    {
        Assert.That(() => new VarName("$foo"), Throws.TypeOf<SyntaxErrorException>());
    }
    
    [Test]
    public void GivenNameWithNonNumericIndexCheckConstructionThrows()
    {
        Assert.That(() => new VarName("foo[nope]"), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void GivenNameWithEmptyIndexCheckConstructionThrows()
    {
        Assert.That(() => new VarName("foo[]"), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void GivenNameStartingWithDigitCheckConstructionThrows()
    {
        Assert.That(() => new VarName("123abc"), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void GivenNameWithTrailingCharactersAfterIndexCheckConstructionThrows()
    {
        Assert.That(() => new VarName("foo[2]bar"), Throws.TypeOf<SyntaxErrorException>());
    }
}