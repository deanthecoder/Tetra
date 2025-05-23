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
    public void CheckJmpEqJumpsWhenEqual()
    {
        const string code =
            """
                ld $x, 5
                ld $y, 5
                jmp_eq $x, $y, equal
                ld $a, 0
                halt
            equal:
                ld $a, 1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }

    [Test]
    public void CheckJmpNeJumpsWhenNotEqual()
    {
        const string code =
            """
                ld $x, 5
                ld $y, 6
                jmp_ne $x, $y, notequal
                ld $a, 0
                halt
            notequal:
                ld $a, 1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }

    [Test]
    public void CheckJmpLtJumpsWhenLessThan()
    {
        const string code =
            """
                ld $x, 3
                ld $y, 5
                jmp_lt $x, $y, less
                ld $a, 0
                halt
            less:
                ld $a, 1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }

    [Test]
    public void CheckJmpLeJumpsWhenLessThanOrEqual()
    {
        const string code =
            """
                ld $x, 5
                ld $y, 5
                jmp_le $x, $y, le
                ld $a, 0
                halt
            le:
                ld $a, 1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }

    [Test]
    public void CheckJmpGtJumpsWhenGreaterThan()
    {
        const string code =
            """
                ld $x, 10
                ld $y, 5
                jmp_gt $x, $y, greater
                ld $a, 0
                halt
            greater:
                ld $a, 1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }

    [Test]
    public void CheckJmpGeJumpsWhenGreaterThanOrEqual()
    {
        const string code =
            """
                ld $x, 8
                ld $y, 8
                jmp_ge $x, $y, ge
                ld $a, 0
                halt
            ge:
                ld $a, 1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }
}