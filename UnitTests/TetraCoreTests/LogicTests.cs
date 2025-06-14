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

namespace UnitTests.TetraCoreTests;

[TestFixture]
public class LogicTests
{
    [Test]
    public void CheckIntEquality()
    {
        const string code =
            """
            ld $a, 1
            eq $a, 1
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }
    
    [Test]
    public void CheckIntInequality()
    {
        const string code =
            """
            ld $a, 1
            eq $a, 2
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }
    
    [Test]
    public void CheckFloatEquality()
    {
        const string code =
            """
            ld $a, 1.0
            eq $a, 1.0
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }
    
    [Test]
    public void CheckFloatInequality()
    {
        const string code =
            """
            ld $a, 1.0
            eq $a, 2.0
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }

    [Test]
    public void CheckIntFloatEquality()
    {
        const string code =
            """
            ld $a, 1
            eq $a, 1.0
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }
    
    [Test]
    public void CheckVectorEquality()
    {
        const string code =
            """
            ld $a, 1.0, 2.0
            eq $a, 1.0, 2.0
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }
    
    [Test]
    public void CheckVectorInequality()
    {
        const string code =
            """
            ld $a, 2.0, 1.0
            eq $a, 1.0, 2.0
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Float, Is.Zero);
    }
    
    [Test]
    public void CheckLessThanTrue()
    {
        const string code =
            """
            ld $a, 2
            lt $a, 3
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }
    
    [Test]
    public void CheckLessThanFalse()
    {
        const string code =
            """
            ld $a, 2
            lt $a, 1
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }

    [Test]
    public void CheckLessThanOrEqualTrue()
    {
        const string code =
            """
            ld $a, 2
            le $a, 2
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }

    [Test] 
    public void CheckLessThanOrEqualFalse()
    {
        const string code =
            """
            ld $a, 2
            le $a, 1
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }

    [Test]
    public void CheckGreaterThanTrue()
    {
        const string code =
            """
            ld $a, 2
            gt $a, 1
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }

    [Test]
    public void CheckGreaterThanFalse()
    {
        const string code =
            """
            ld $a, 2
            gt $a, 3
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }

    [Test]
    public void CheckGreaterThanOrEqualTrue()
    {
        const string code =
            """
            ld $a, 2
            ge $a, 2
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }

    [Test]
    public void CheckGreaterThanOrEqualFalse()
    {
        const string code =
            """
            ld $a, 2
            ge $a, 3
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }
    [Test]
    public void CheckNotEqualTrue()
    {
        const string code =
            """
            ld $a, 2
            ne $a, 1
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }

    [Test]
    public void CheckNotEqualFalse()
    {
        const string code =
            """
            ld $a, 2
            ne $a, 2
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }

    [Test]
    public void CheckAndTrue()
    {
        const string code =
            """
            ld $a, 1
            and $a, 1
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }

    [Test]
    public void CheckAndFalse()
    {
        const string code =
            """
            ld $a, 1
            and $a, 0
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }

    [Test]
    public void CheckOrTrue()
    {
        const string code =
            """
            ld $a, 1
            or $a, 0
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }

    [Test]
    public void CheckOrFalse()
    {
        const string code =
            """
            ld $a, 0
            or $a, 0
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }
    
    [Test]
    public void CheckNotTrue()
    {
        const string code =
            """
            ld $a, 0
            not $a
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1));
    }

    [Test]
    public void CheckNotFalse()
    {
        const string code =
            """
            ld $a, 1
            not $a
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Float, Is.Zero);
    }
}