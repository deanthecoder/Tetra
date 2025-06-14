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
public class JmpTests
{
    [Test]
    public void CheckJumpingToMissingLabelThrows()
    {
        const string code = "jmp missing";

        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void CheckJumpingToValueThrows()
    {
        const string code = "jmp 123";

        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void CheckJumpingToVariableThrows()
    {
        const string code =
            """
            ld $a, 123
            jmp $a
            """;

        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }

    [Test]
    public void CheckJumpingToMissingVariableThrows()
    {
        const string code = "jmp $unknown";

        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }
    
    [Test]
    public void CheckJmpJumpsUnconditionally()
    {
        const string code =
            """
                jmp skip
                ld $a, 1
                halt
            skip:
                ld $a, 42
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(42));
    }

    [Test]
    public void CheckJmpZJumpsWhenZero()
    {
        const string code =
            """
                ld $x, 0
                jmp_z $x, zero
                ld $a, 0
                halt
            zero:
                ld $a, 1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }

    [Test]
    public void CheckJmpNzJumpsWhenNotZero()
    {
        const string code =
            """
                ld $x, 5
                jmp_nz $x, notzero
                ld $a, 0
                halt
            notzero:
                ld $a, 1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }
}