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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTC.Core.Extensions;
using TetraCore;

namespace DTC.GLSLParser;

public class TetraEmitter
{
    private readonly StringBuilder m_sb = new();
    private readonly Stack<(string continueLabel, string breakLabel)> m_loopStack = [];
    private readonly Stack<FunctionNode> m_currentFunctionStack = [];
    private int m_tmpCounter;
    private int m_forLoopCounter;
    private int m_ifCounter;
    private int m_skipLabelCounter;
    private FunctionNode[] m_functionNodes;

    public string Emit(ProgramNode program, string entryPoint = "main")
    {
        m_sb.Clear();
        m_tmpCounter = 0;
        m_forLoopCounter = 0;
        m_ifCounter = 0;
        m_skipLabelCounter = 0;
        m_loopStack.Clear();
        m_functionNodes = program.Walk().OfType<FunctionNode>().ToArray();

        // Emit program statements.
        EmitProgram(program);

        if (string.IsNullOrEmpty(entryPoint))
            return m_sb.ToString(); // No entry point defined.

        // Implicit call to entry point function.
        var codeLines = m_sb.Replace("\r\n", "\n").ToString().Split('\n').ToList();
        if (codeLines.FastFindIndexOf($"{entryPoint}:") == -1)
            throw new EmitterException($"Entry point '{entryPoint}' not found.");
        var firstLabel = codeLines.FindIndex(o => o.EndsWith(':'));
        while (firstLabel > 0 && codeLines[firstLabel - 1].StartsWith('#'))
            firstLabel--;

        codeLines.InsertRange(firstLabel,
        [
            "# Entry point.",
            $"call {entryPoint}",
            "halt",
            string.Empty
        ]);
        return string.Join("\n", codeLines);
    }

    private void EmitNode(AstNode node)
    {
        // Should be primarily statements.
        switch (node)
        {
            case FunctionNode function:
                EmitFunction(function);
                break;

            case ReturnNode returnNode:
                EmitReturn(returnNode);
                break;

            case ExpressionStatementNode expr:
                EmitNode(expr.Expression);
                break;
            
            case CallExprNode call:
                EmitCall(call);
                break;

            case VariableDeclarationNode decl:
                EmitVariableDeclaration(decl);
                break;
            
            case MultiVariableDeclarationNode decl:
                EmitMultiVariableDeclaration(decl);
                break;
            
            case AssignmentNode assign:
                EmitAssignment(assign);
                break;
            
            case UnaryExprNode unary:
                EmitUnary(unary);
                break;
            
            case ForNode forNode:
                EmitFor(forNode);
                break;
            
            case BreakNode:
                EmitBreak();
                break;
            
            case ContinueNode:
                EmitContinue();
                break;
            
            case IfNode ifNode:
                EmitIf(ifNode);
                break;
            
            case BlockNode block:
                EmitBlock(block);
                break;

            default:
                throw new NotImplementedException($"Unsupported statement: '{node}' ({node.GetType().Name})");
        }
    }

    private void EmitBreak() =>
        WriteLine($"jmp {m_loopStack.Peek().breakLabel}");

    private void EmitContinue() =>
        WriteLine($"jmp {m_loopStack.Peek().continueLabel}");

    private void WriteLine(string s = "") =>
        m_sb.AppendLine(s);

    private void EmitProgram(ProgramNode program)
    {
        var nodes = program.Statements;

        if (nodes.OfType<FunctionNode>().Any())
        {
            // Move globals to the head of the code.
            nodes = nodes.OrderBy(o => o.GetType() == typeof(FunctionNode) ? 1 : 0).ToArray();
        }
        
        foreach (var statement in nodes)
            EmitNode(statement);
    }

    private void EmitFunction(FunctionNode function)
    {
        m_currentFunctionStack.Push(function);
        using var _ = new TempVariableBlock(ref m_tmpCounter);
        
        // Summary comment.
        WriteLine($"# {function.ReturnType.Value} {function.Name.Value}({function.Parameters.Select(o => $"{o.Modifier?.Value ?? string.Empty} {o.Type.Value} {o.Name.Value}".Trim()).ToCsv(addSpace: true)})");

        // Label
        WriteLine($"{function.Name.Value}:");

        // Parameter mapping.
        if (function.Parameters.Length > 0)
        {
            WriteLine($"decl {function.Parameters.Select(o => $"${o.Name.Value}").ToCsv()}");
            for (var i = 0; i < function.Parameters.Length; i++)
            {
                var parameter = function.Parameters[i];
                WriteLine($"ld ${parameter.Name.Value}, $arg{i}");
            }
        }

        // Body
        function.Body.Statements.ForEach(EmitNode);

        // Ensure a return statement exists.
        if (function.Body.Statements.LastOrDefault()?.GetType() != typeof(ReturnNode))
            EmitReturn(new ReturnNode(null));

        WriteLine($"__{function.Name.Value}_end:");

        // Reassign 'out' param values back to $argN.
        var outParams = GetFunctionOutParams(function).ToArray();
        for (var i = 0; i < outParams.Length; i++)
        {
            var param = outParams[i];
            if (param != null)
                WriteLine($"ldc $arg{i}, ${param}");
        }

        WriteLine(function.ReturnType.Value == "void" ? "ret" : "ret $retval");
        WriteLine();
        
        m_currentFunctionStack.Pop();
    }

    private static string[] GetFunctionInParams(FunctionNode function) =>
        function
            .Parameters
            .Select(o => string.IsNullOrEmpty(o.Modifier?.Value) || o.Modifier?.Value.StartsWith("in") == true ? o.Name.Value : null)
            .ToArray();

    private static string[] GetFunctionOutParams(FunctionNode function) =>
        function
            .Parameters
            .Select(o => o.Modifier?.Value.Contains("out") == true ? o.Name.Value : null)
            .ToArray();

    private void EmitReturn(ReturnNode returnNode)
    {
        if (m_currentFunctionStack.Count > 0 && GetFunctionOutParams(m_currentFunctionStack.Peek()).Length > 0)
        {
            // Support 'return' in functions with 'out' params.
            if (returnNode.Value != null)
                WriteLine($"ld $retval, {EmitExpression(returnNode.Value)}");

            var function = m_currentFunctionStack.Peek();
            WriteLine($"jmp __{function.Name.Value}_end");
            return;
        }
        
        if (returnNode.Value == null)
        {
            WriteLine("ret");
            return;
        }

        WriteLine($"ret {EmitExpression(returnNode.Value)}");
    }
    
    private string EmitCall(CallExprNode call)
    {
        // Handle intrinsic calls.
        var intrinsicOpCode = OpCodeToStringMap.GetIntrinsic(call.FunctionName.Value);
        if (intrinsicOpCode.HasValue)
            return EmitIntrinsicCall(call, intrinsicOpCode.Value);
        
        // User-defined function.
        FunctionNode currentFunction = null;
        var hasParams = call.Arguments.Length > 0;
        if (hasParams)
        {
            // 'inout' params need $arg0, ... assigning before the call.
            // ('out' params do not - They're one way only.)
            currentFunction = m_functionNodes.First(o => o.Name.Value == call.FunctionName.Value);
            var inParams = GetFunctionInParams(currentFunction);
            for (var i = 0; i < inParams.Length; i++)
            {
                if (inParams[i] != null)
                {
                    // 'in' param - Send in the value.
                    WriteLine($"ld $arg{i}, {EmitExpression(call.Arguments[i])}");
                }
                else
                {
                    // 'out' param - Send in a fake value.
                    WriteLine($"ld $arg{i}, 0.0");
                }
            }
        }
        
        WriteLine($"call {call.FunctionName.Value}");
        
        // Reassign '(in)out' params back to the original variable names.
        if (hasParams)
        {
            var outParams = GetFunctionOutParams(currentFunction);
            for (var i = 0; i < outParams.Length; i++)
            {
                var param = outParams[i];
                if (param != null)
                    WriteLine($"ld ${((VariableNode)call.Arguments[i]).Name.Value}, $arg{i}");
            }
        }
        
        return "$retval";
    }
    
    private void EmitVariableDeclaration(VariableDeclarationNode decl)
    {
        if (decl.Value == null)
        {
            WriteLine($"decl ${decl.Name.Value}");
            return;
        }

        // Support vector creation.
        if (decl.Value is ConstructorCallNode ctor)
        {
            var result = EmitConstructorCall(ctor);
            WriteLine($"decl ${decl.Name.Value}");
            WriteLine($"ld ${decl.Name.Value}, {result}");
            return;
        }

        WriteLine($"decl ${decl.Name.Value}");
        WriteLine($"ld ${decl.Name.Value}, {EmitExpression(decl.Value)}");
    }

    private string EmitConstructorCall(ConstructorCallNode ctor)
    {
        // Grab the component arguments.
        var tmpVars = AssignArgsToLocals(ctor.Arguments).ToList();
        
        // Construct the object.
        var result = $"$tmp{m_tmpCounter++}";
        WriteLine($"decl {result}");
        WriteLine($"ld {result}, {tmpVars.ToCsv(addSpace: true)}");

        // Support vectors.
        var typeName = ctor.FunctionName.Value;
        if (typeName.Contains("vec"))
        {
            var dimension = int.Parse(typeName.Last().ToString());
            WriteLine($"dim {result}, {dimension}");
        }

        return result;
    }

    private string[] AssignArgsToLocals(ExprStatementNode[] argNodes)
    {
        var tmpVars = new string[argNodes.Length];
        for (var i = 0; i < argNodes.Length; i++)
        {
            var v = argNodes[i];
            var tmpName = $"$tmp{m_tmpCounter++}";
            tmpVars[i] = tmpName;
            WriteLine($"decl {tmpName}");
            WriteLine($"ld {tmpName}, {EmitExpression(v)}");
        }
        return tmpVars;
    }

    private void EmitMultiVariableDeclaration(MultiVariableDeclarationNode decl) =>
        decl.Declarations.ForEach(EmitVariableDeclaration);

    private string EmitExpression(ExprStatementNode exprNode)
    {
        if (exprNode is VariableNode variable)
            return $"${variable.Name.Value}";

        if (exprNode is IndexExprNode index)
            return $"{EmitExpression(index.Target)}[{index.Index}]";

        if (exprNode is SwizzleExprNode swizzle)
            return EmitSwizzle(swizzle);

        if (exprNode is LiteralNode literal)
            return $"{literal.Value.Value}";

        if (exprNode is ConstructorCallNode ctor)
            return EmitConstructorCall(ctor);
        
        if (exprNode is CallExprNode call)
            return EmitCall(call);

        if (exprNode is UnaryExprNode unaryExpr)
            return EmitUnary(unaryExpr);
        
        if (exprNode is BinaryExprNode binaryExpr)
            return EmitBinary(binaryExpr);

        if (exprNode is TernaryNode ternary)
            return EmitTernary(ternary);

        throw new EmitterException($"Unexpected expression '{exprNode}' ({exprNode?.GetType().Name})");
    }

    private string EmitTernary(TernaryNode ternaryNode)
    {
        var tmpName = $"$tmp{m_tmpCounter++}";
        m_ifCounter++;
        var elseLabel = $"__if{m_ifCounter}_else";
        var endLabel = $"__if{m_ifCounter}_end";
        
        // If
        WriteLine($"decl {tmpName}");
        WriteLine($"test {tmpName}, {EmitExpression(ternaryNode.Condition)}");
        WriteLine($"jmpz {tmpName}, {elseLabel}");
        
        // Then
        WriteLine($"ld {tmpName}, {EmitExpression(ternaryNode.ThenExpr)}");
        WriteLine($"jmp {endLabel}");
        
        // Else
        WriteLine($"{elseLabel}:");
        WriteLine($"ld {tmpName}, {EmitExpression(ternaryNode.ElseExpr)}");

        // End
        WriteLine($"{endLabel}:");
        
        return tmpName;
    }
    
    private string EmitBinary(BinaryExprNode binaryExpr)
    {
        var tmpName = $"$tmp{m_tmpCounter++}";
        WriteLine($"decl {tmpName}");

        var op = binaryExpr.Operator.Value switch
        {
            "+" => "add",
            "-" => "sub",
            "*" => "mul",
            "/" => "div",
            "%" => "mod",
            "&" => "bit_and",
            "|" => "bit_or",
            "==" => "eq",
            "!=" => "ne",
            "<" => "lt",
            "<=" => "le",
            ">" => "gt",
            ">=" => "ge",
            "&&" => "and",
            "||" => "or",
            "<<" => "shiftl",
            ">>" => "shiftr",
            _ => throw new InvalidOperationException($"Unsupported operator '{binaryExpr.Operator.Value}'")
        };

        if (op == "and")
        {
            // Special case - The second expression should not be executed if the first is false.
            var skipLabel = $"__logic{m_skipLabelCounter++}_skip";
            WriteLine($"test {tmpName}, {EmitExpression(binaryExpr.Left)}");
            WriteLine($"jmpz {tmpName}, {skipLabel}");
            WriteLine($"{op} {tmpName}, {EmitExpression(binaryExpr.Right)}");
            WriteLine($"test {tmpName}, {tmpName}");
            WriteLine($"{skipLabel}:");
        } else if (op == "or")
        {
            // Special case - The second expression should not be executed if the first is true.
            var skipLabel = $"__logic{m_skipLabelCounter++}_skip";
            WriteLine($"test {tmpName}, {EmitExpression(binaryExpr.Left)}");
            WriteLine($"jmpnz {tmpName}, {skipLabel}");
            WriteLine($"{op} {tmpName}, {EmitExpression(binaryExpr.Right)}");
            WriteLine($"test {tmpName}, {tmpName}");
            WriteLine($"{skipLabel}:");
        }
        else
        {
            WriteLine($"ld {tmpName}, {EmitExpression(binaryExpr.Left)}");
            WriteLine($"{op} {tmpName}, {EmitExpression(binaryExpr.Right)}");
        }
            
        return tmpName;
    }

    private string EmitUnary(UnaryExprNode unaryExpr)
    {
        // Check if RHS is not to be directly modified.
        if (unaryExpr.Operator.Value == "-")
        {
            var rhs = unaryExpr.Operand switch
            {
                LiteralNode literalExpr => literalExpr.Value.Value,
                VariableNode variableExpr => $"${variableExpr.Name.Value}",
                CallExprNode callExpr => EmitCall(callExpr),
                SwizzleExprNode swizzleExpr => EmitSwizzle(swizzleExpr),
                _ => throw new EmitterException($"Unexpected expression '{unaryExpr.Operand}' ({unaryExpr.Operand.GetType().Name})")
            };

            var tmpName = $"$tmp{m_tmpCounter++}";
            WriteLine($"decl {tmpName}");
            WriteLine($"ld {tmpName}, {rhs}");
            WriteLine($"neg {tmpName}");
            return tmpName;
        }

        // These operators change the RHS value, so RHS must be a variable.
        if (unaryExpr.Operand is VariableNode v)
        {
            var op = unaryExpr.Operator.Value switch
            {
                "-" => "neg",
                "--" => "dec",
                "++" => "inc",
                _ => throw new InvalidOperationException($"Unsupported operator '{unaryExpr.Operator.Value}'")
            };

            if (unaryExpr.IsPostfix)
            {
                // Return the value, then modify the original.
                var tmpName = $"$tmp{m_tmpCounter++}";
                WriteLine($"decl {tmpName}");
                WriteLine($"ld {tmpName}, ${v.Name.Value}");
                WriteLine($"{op} ${v.Name.Value}");
                return tmpName;
            }
                
            // Modify the original, then return the value.
            WriteLine($"{op} ${v.Name.Value}");
            return $"${v.Name.Value}";
        }
            
        throw new EmitterException($"Unexpected expression '{unaryExpr.Operand}' ({unaryExpr.Operand.GetType().Name})");
    }

    private string EmitSwizzle(SwizzleExprNode swizzle)
    {
        var indexLookup = new Dictionary<char, int>
        {
            { 'x', 0 },
            { 'y', 1 },
            { 'z', 2 },
            { 'w', 3 },
            { 'r', 0 },
            { 'g', 1 },
            { 'b', 2 },
            { 'a', 3 },
            { 's', 0 },
            { 't', 1 },
            { 'p', 2 },
            { 'q', 3 }
        };
        var indices = swizzle.Swizzle.Value.Select(o => indexLookup[o]).ToArray();
        string s;
        if (indices.Length == 1)
            s = $"{EmitExpression(swizzle.Target)}[{indices[0]}]";
        else
        {
            WriteLine($"ld $_swz, {EmitExpression(swizzle.Target)}");
            s = indices.Select(o => $"$_swz[{o}]").ToCsv(addSpace: true);
        }
        return s;
    }

    private void EmitAssignment(AssignmentNode assign)
    {
        WriteLine($"ld ${assign.Target}, {EmitExpression(assign.Value)}");
    }

    private void EmitFor(ForNode forNode)
    {
        WriteLine("push_frame");
        
        // Setup.
        EmitNode(forNode.Init);
        
        // Loop start label.
        var labelSuffix = m_forLoopCounter++;
        var startLabel = $"__for{labelSuffix}_start";
        WriteLine($"{startLabel}:");

        // Support break/continue.
        var incrementLabel = $"__for{labelSuffix}_incr";
        var endLabel = $"__for{labelSuffix}_end";
        m_loopStack.Push((continueLabel: incrementLabel, breakLabel: endLabel));

        // Condition check.
        var repeat = EmitExpression(forNode.Condition);
        WriteLine($"jmpz {repeat}, __for{labelSuffix}_end");

        // Loop body.
        if (forNode.Body is BlockNode blockNode)
            blockNode.Statements.ForEach(EmitNode);
        else
            EmitNode(forNode.Body);
        
        // Loop increment.
        WriteLine($"{incrementLabel}:");
        EmitExpression(forNode.Step);
        
        // Loop end.
        WriteLine($"jmp {startLabel}");
        WriteLine($"{endLabel}:");
        
        // Cleanup.
        WriteLine("pop_frame");
        
        m_loopStack.Pop();
    }

    private void EmitIf(IfNode ifNode)
    {
        // Condition check.
        var check = EmitExpression(ifNode.Condition);
        var tmpName = $"$tmp{m_tmpCounter++}";
        WriteLine($"decl {tmpName}");
        WriteLine($"test {tmpName}, {check}");
        
        // Support 'if' with no else.
        m_ifCounter++;
        if (ifNode.ElseBlock == null)
        {
            var endLabel = $"__if{m_ifCounter}_end";
            WriteLine($"jmpz {tmpName}, {endLabel}");
            EmitNode(ifNode.ThenBlock);
            WriteLine($"{endLabel}:");
        }
        else
        {
            var elseLabel = $"__if{m_ifCounter}_else";
            var endLabel = $"__if{m_ifCounter}_end";
            WriteLine($"jmpz {tmpName}, {elseLabel}");
            EmitNode(ifNode.ThenBlock);
            WriteLine($"jmp {endLabel}");
            
            WriteLine($"{elseLabel}:");
            EmitNode(ifNode.ElseBlock);
            WriteLine($"{endLabel}:");
        }
    }

    private void EmitBlock(BlockNode blockNode)
    {
        if (blockNode.Statements.Length == 0)
            return; // Nothing to do.
        
        WriteLine("push_frame");
        blockNode.Statements.ForEach(EmitNode);
        WriteLine("pop_frame");
    }

    private string EmitIntrinsicCall(CallExprNode call, OpCode intrinsicOpCode)
    {
        var tmpVars = AssignArgsToLocals(call.Arguments);
        var functionName = OpCodeToStringMap.GetString(intrinsicOpCode);
        
        // Special-case intrinsic statements (that don't return a value).
        if (functionName == "debug" || functionName == "print")
        {
            WriteLine($"{functionName} {tmpVars.ToCsv(addSpace: true)}");
            return null;
        }
        
        if (tmpVars.Length == 1)
        {
            // One arg:
            // ld $a, ...
            // sin $a, $a
            // Return: $a
            WriteLine($"{functionName} {tmpVars[0]}, {tmpVars[0]}");
            return tmpVars[0];
        }

        if (tmpVars.Length == 2)
        {
            // Two args:
            // ld $a, ...
            // ld $b, ...
            // min $a, $b
            // Return: $a
            WriteLine($"{functionName} {tmpVars[0]}, {tmpVars[1]}");
            return tmpVars[0];
        }

        if (tmpVars.Length == 3)
        {
            // Three args:
            // ld $a, ...
            // ld $from, ...
            // ld $to, ...
            // clamp $a, $from, $to
            // Return: $a
            
            // GLSL smoothstep(from, to, f) => tetra (f, from, to).
            if (functionName == "smoothstep")
                WriteLine($"{functionName} {tmpVars[2]}, {tmpVars[0]}, {tmpVars[1]}");
            else
                WriteLine($"{functionName} {tmpVars[0]}, {tmpVars[1]}, {tmpVars[2]}");
            return tmpVars[0];
        }
        
        throw new EmitterException($"Unsupported intrinsic call '{functionName}'");
    }
}