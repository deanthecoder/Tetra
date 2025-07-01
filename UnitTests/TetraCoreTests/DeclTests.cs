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
public class DeclTests
{
    [Test]
    public void CheckDeclaringVariable()
    {
        const string code = "decl $a";
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Float));
        Assert.That(vm["a"].IsUnassigned, Is.True);
    }

    [Test]
    public void CheckUnassignedOperandsAreSingletons()
    {
#pragma warning disable NUnit2009
        Assert.That(Operand.Unassigned, Is.SameAs(Operand.Unassigned));
#pragma warning restore NUnit2009
    }

    [Test]
    public void CheckUsingUnassignedSingleOperandThrows()
    {
        const string code = 
            """
            decl $a
            inc $a
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }
    
    [Test]
    public void CheckUsingUnassignedSecondOperandThrows()
    {
        const string code = 
            """
            decl $a, $b
            ld $a, 1.0
            add $a, $b
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }
    
    [Test]
    public void CheckDeclaringMultipleVariables()
    {
        const string code = "decl $a,$b";
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Float));
        Assert.That(vm["b"].Type, Is.EqualTo(OperandType.Float));
    }
}