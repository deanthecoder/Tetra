// // Code authored by Dean Edis (DeanTheCoder).
// // Anyone is free to copy, modify, use, compile, or distribute this software,
// // either in source code form or as a compiled binary, for any non-commercial
// // purpose.
// //
// // If you modify the code, please retain this copyright header,
// // and consider contributing back to the repository or letting us know
// // about your modifications. Your contributions are valued!
// //
// // THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using TetraCore;

namespace UnitTests;

[TestFixture]
public class NegTests
{
    [Test]
    public void CheckNegatingInteger()
    {
        const string code =
            """
            ld $a, 1
            neg $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("a").IntValue, Is.EqualTo(-1));
    }

    [Test]
    public void CheckNegatingFloat()
    {
        const string code =
            """
            ld $a, -1.2
            neg $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("a").FloatValue, Is.EqualTo(1.2).Within(0.001));
    }
}