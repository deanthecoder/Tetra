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

using System.Linq;
using DTC.Core.Extensions;
using DTC.GLSLLexer;
using TetraCore;

namespace DTC.GLSLParser;

/// <summary>
/// Compiles GLSL code into Tetra code.
/// </summary>
public static class Compiler
{
    public static string CompileToTetraSource(string glslCode, string entryPoint = null)
    {
        var preprocessedCode = new Preprocessor.Preprocessor().Preprocess(glslCode);
        var tokens = new Lexer().Tokenize(preprocessedCode);
        var ast = new Parser().Parse(tokens);
        CheckForUnresolvedExternals(ast);
        
        var emitter = new TetraEmitter();
        var tetraCode = emitter.Emit(ast, entryPoint);
        return tetraCode;
    }

    private static void CheckForUnresolvedExternals(ProgramNode program)
    {
        var ast = program.Walk().ToArray();
        var definedFunctions = ast.OfType<FunctionNode>().Select(o => o.Name.Value);
        var expectedFunctions = ast.OfType<CallExprNode>().Select(o => o.FunctionName.Value).Distinct().ToArray();
        var intrinsics = expectedFunctions.Where(o => OpCodeToStringMap.GetIntrinsic(o) != null);
        var nativeTypeCreations = ast.OfType<ConstructorCallNode>().Select(o => o.FunctionName.Value).Distinct();
        
        var unresolvedFunctions =
            expectedFunctions
                .Except(intrinsics)
                .Except(definedFunctions)
                .Except(nativeTypeCreations)
                .OrderBy(o => o)
                .Select(o => $"{o}()")
                .ToArray();
        if (unresolvedFunctions.Length > 0)
            throw new CompilerException($"Unresolved externals: {unresolvedFunctions.ToCsv(addSpace: true)}");
    }
}