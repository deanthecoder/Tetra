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

using DTC.GLSLLexer;

namespace DTC.GLSLParser;

/// <summary>
/// Compiles GLSL code into Tetra code.
/// </summary>
public static class Compiler
{
    public static string CompileToTetraSource(string glslCode, string entryPoint = null)
    {
        var tokens = new Lexer().Tokenize(glslCode);
        var ast = new Parser().Parse(tokens);
        var emitter = new TetraEmitter();
        var tetraCode = emitter.Emit(ast, entryPoint);
        return tetraCode;
    }
}