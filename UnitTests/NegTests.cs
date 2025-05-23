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

        Assert.That(vm["a"].Int, Is.EqualTo(-1));
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

        Assert.That(vm["a"].Float, Is.EqualTo(1.2).Within(0.001));
    }

    [Test]
    public void CheckNegatingVector()
    {
        const string code =
            """
            ld $a, 1.0, -2.0, 3.0
            neg $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
    
        Assert.That(vm["a"].Floats[0], Is.EqualTo(-1.0).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(2.0).Within(0.001));
        Assert.That(vm["a"].Floats[2], Is.EqualTo(-3.0).Within(0.001));
    }
}