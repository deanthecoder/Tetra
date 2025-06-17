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
public class BitShiftTests
{
    [Test]
    public void CheckShiftingIntRight()
    {
        const string code =
            """
            ld $a, 5
            shiftr $a, 1
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);
        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Int));
        Assert.That(vm["a"].Int, Is.EqualTo(2));
    }
    
    [Test]
    public void CheckShiftingIntLeft()
    {
        const string code =
            """
            ld $a, 5
            shiftl $a, 1
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);
        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Int));
        Assert.That(vm["a"].Int, Is.EqualTo(10));
    }
}