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
public class MatrixTests
{
    [Test]
    public void CheckVectorMatrixMultiplication()
    {
        const string code =
            """
            ld $v, 2.2, 3.3
            ld $m, 1.1, 2.2, 3.3, 4.4
            mul $v, $m
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Floats, Is.EqualTo(new[] { 9.68, 21.78 }).Within(0.001));
    }

    [Test]
    public void CheckVec3Mat3Multiplication()
    {
        const string code =
            """
            ld $v, 1.0, 2.0, 3.0
            ld $m, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0
            mul $v, $m
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Floats, Is.EqualTo(new[] { 1.0f, 2.0f, 3.0f }).Within(0.001));
    }

    [Test]
    public void CheckVec4Mat4Multiplication()
    {
        const string code =
            """
            ld $v, 1.0, 0.0, 0.0, 0.0
            ld $m, 2.0, 0.0, 0.0, 0.0, 0.0, 3.0, 0.0, 0.0, 0.0, 0.0, 4.0, 0.0, 0.0, 0.0, 0.0, 5.0
            mul $v, $m
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Floats, Is.EqualTo(new[] { 2.0f, 0.0f, 0.0f, 0.0f }).Within(0.001));
    }

    [Test]
    public void MatrixSizeMismatchShouldThrow()
    {
        const string code =
            """
            ld $v, 1.0, 2.0
            ld $m, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0
            mul $v, $m
            """;

        var vm = new TetraVm(Assembler.Assemble(code));
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void VectorComponentwiseMultiplication()
    {
        const string code =
            """
            ld $a, 2.0, 3.0
            ld $b, 4.0, 5.0
            mul $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats, Is.EqualTo(new[] { 8.0f, 15.0f }).Within(0.001));
    }
}