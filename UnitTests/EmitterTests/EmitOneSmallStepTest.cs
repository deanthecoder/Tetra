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
using DTC.Core.Extensions;
using DTC.Core.UnitTesting;
using DTC.GLSLParser;

namespace UnitTests.EmitterTests;

[TestFixture]
public class EmitOneSmallStepTest : TestsBase
{
    [Test, Ignore("Not fully supported yet.")]
    public void EmitPiApproximationCode()
    {
        var code = ProjectDir.GetDir("Examples").GetFile("OneSmallStep.glsl").ReadAllText();

        string tetraCode = null;
        Assert.That(() => tetraCode = Compiler.CompileToTetraSource(code, "main"), Throws.Nothing);

        Assert.That(tetraCode, Is.Not.Null);
        Assert.That(tetraCode, Is.Not.Empty);
    }
}