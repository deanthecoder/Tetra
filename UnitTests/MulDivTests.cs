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
public class MulDivTests
{
    [Test]
    public void CheckMultiplyingConstantThrows()
    {
        const string code = "mul 123, 69";
        
        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void CheckMultiplyIntAndInt()
    {
        const string code =
            """
            ld $a, 123
            mul $a, 69
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm.CurrentFrame.GetVariable("a").IntValue, Is.EqualTo(8487));
    }

    [Test]
    public void CheckMultiplyIntAndFloat()
    {
        const string code =
            """
            ld $a, -2
            mul $a, 6.9
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm.CurrentFrame.GetVariable("a").FloatValue, Is.EqualTo(-13.8).Within(0.001));
    }

    [Test]
    public void CheckMultiplyIntAndVariable()
    {
        const string code =
            """
            ld $a, 3
            ld $b, 2.3
            mul $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm.CurrentFrame.GetVariable("a").FloatValue, Is.EqualTo(6.9).Within(0.001));
    }

    [Test]
    public void CheckDividingConstantThrows()
    {
        const string code = "div 123, 69";

        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void CheckDivideIntAndInt()
    {
        const string code =
            """
            ld $a, 7
            div $a, 2
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm.CurrentFrame.GetVariable("a").IntValue, Is.EqualTo(3));
    }
    
    [Test]
    public void CheckDivideIntAndFloat()
    {
        const string code =
            """
            ld $a, 7
            div $a, 2.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("a").FloatValue, Is.EqualTo(3.5));
    }
    
    [Test]
    public void CheckDivideIntAndVariable()
    {
        const string code =
            """
            ld $a, 7
            ld $b, 3
            div $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("a").IntValue, Is.EqualTo(2));

    }
    
    [Test]
    public void CheckDivideByZeroThrows()
    {
        const string code =
            """
            ld $a, 7
            div $a, 0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }
}