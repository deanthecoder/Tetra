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
public class NormalizeTests
{
    [Test]
    public void CheckNormalizeVector()
    {
        const string code = "normalize $a, 3.0, 4.0";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.6f).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(0.8f).Within(0.001));
    }

    [Test]
    public void CheckNormalizeZeroVector()
    {
        const string code =
            """
            ld $b, 0.0, 0.0, 0.0
            normalize $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(3));
        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.0f).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(0.0f).Within(0.001));
        Assert.That(vm["a"].Floats[2], Is.EqualTo(0.0f).Within(0.001));
    }
    
    [Test]
    public void CheckNormalizeFloatThrows()
    {
        const string code = "normalize $a, 2.3";
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }
    
    [Test]
    public void CheckNormalizeConstantThrows()
    {
        Assert.That(() => Assembler.Assemble("normalize 2.3"), Throws.TypeOf<SyntaxErrorException>());
        Assert.That(() => Assembler.Assemble("normalize 2.3, 4.5"), Throws.TypeOf<SyntaxErrorException>());
    }
}