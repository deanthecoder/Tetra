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