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
    private int m_tmpCounter;
    private int m_forLoopCounter;
    private int m_skipLabelCounter;

    public string Emit(ProgramNode program, string entryPoint = "main")
    {
        m_sb.Clear();
        m_tmpCounter = 0;
        m_forLoopCounter = 0;
        m_skipLabelCounter = 0;
        m_loopStack.Clear();

        // Emit program statements.
        EmitNode(program);

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
            case ProgramNode program:
                EmitProgram(program);
                break;

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
            
            case ForNode forNode:
                EmitFor(forNode);
                break;
            
            case BreakNode:
                EmitBreak();
                break;
            
            case ContinueNode:
                EmitContinue();
                break;

            default:
                throw new NotImplementedException($"Unsupported statement: '{node}' ({node.GetType().Name})");
        }
    }

    private void EmitBreak() =>
        WriteLine($"jmp {m_loopStack.Peek().breakLabel}");

    private void EmitContinue() =>
        WriteLine($"jmp {m_loopStack.Peek().continueLabel}");

    private void WriteLine(string s = "")
    {
        m_sb.AppendLine(s);
        Console.WriteLine(s); // Useful for unit testing.
    }

    private void EmitProgram(ProgramNode program)
    {
        foreach (var statement in program.Statements)
            EmitNode(statement);
    }

    private void EmitFunction(FunctionNode function)
    {
        using var _ = new TempVariableBlock(ref m_tmpCounter);
        
        // Summary comment.
        WriteLine(
            $"# {function.ReturnType.Value} {function.Name.Value}({function.Parameters.Select(o => $"{o.Modifier?.Value ?? string.Empty} {o.Type.Value} {o.Name.Value}".Trim()).ToCsv(addSpace: true)})");

        // Label
        WriteLine($"{function.Name.Value}:");

        // Parameter mapping.
        for (var i = 0; i < function.Parameters.Length; i++)
        {
            var parameter = function.Parameters[i];
            WriteLine($"ld ${parameter.Name.Value}, $arg{i}");
        }

        // Body
        function.Body.Statements.ForEach(EmitNode);

        // Ensure a return statement exists.
        if (function.Body.Statements.LastOrDefault()?.GetType() != typeof(ReturnNode))
            EmitReturn(new ReturnNode(null));

        WriteLine();
    }

    private void EmitReturn(ReturnNode returnNode)
    {
        if (returnNode.Value == null)
        {
            WriteLine("ret");
            return;
        }

        WriteLine($"ret {EmitExpression(returnNode.Value)}");
    }
    
    private string EmitCall(CallExprNode call)
    {
        var intrinsicOpCode = OpCodeToStringMap.GetIntrinsic(call.FunctionName.Value);
        if (intrinsicOpCode.HasValue)
        {
            var tmpVars = AssignArgsToLocals(call.Arguments);
            var functionName = OpCodeToStringMap.GetString(intrinsicOpCode.Value);
            var tmpName = $"$tmp{m_tmpCounter++}";
            WriteLine($"{functionName} {tmpName}, {tmpVars.ToCsv(addSpace: true)}");
            return tmpName;
        }
        
        // User-defined function.
        for (var i = 0; i < call.Arguments.Length; i++)
            WriteLine($"ld $arg{i}, {EmitExpression(call.Arguments[i])}");

        WriteLine($"call {call.FunctionName.Value}");
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
            var tmpVars = AssignArgsToLocals(ctor.Arguments);
            WriteLine($"decl ${decl.Name.Value}");
            WriteLine($"ld ${decl.Name.Value}, {tmpVars.ToCsv(addSpace: true)}");
            return;
        }

        WriteLine($"decl ${decl.Name.Value}");
        WriteLine($"ld ${decl.Name.Value}, {EmitExpression(decl.Value)}");
    }

    private string[] AssignArgsToLocals(ExprStatementNode[] argNodes)
    {
        var tmpVars = new string[argNodes.Length];
        for (var i = 0; i < argNodes.Length; i++)
        {
            var v = argNodes[i];
            var tmpName = $"$tmp{m_tmpCounter++}";
            tmpVars[i] = tmpName;
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
            if (indices.Length == 1)
                return $"{EmitExpression(swizzle.Target)}[{indices[0]}]";
            WriteLine($"ld $_swz, {EmitExpression(swizzle.Target)}");
            return indices.Select(o => $"$_swz[{o}]").ToCsv(addSpace: true);
        }

        if (exprNode is LiteralNode literal)
            return $"{literal.Value.Value}";

        if (exprNode is CallExprNode call)
            return EmitCall(call);

        if (exprNode is UnaryExprNode unaryExpr)
        {
            // Check if RHS is not to be directly modified.
            if (unaryExpr.Operator.Value == "-")
            {
                var rhs = unaryExpr.Operand switch
                {
                    LiteralNode literalExpr => literalExpr.Value.Value,
                    VariableNode variableExpr => $"${variableExpr.Name.Value}",
                    CallExprNode callExpr => EmitCall(callExpr),
                    _ => throw new EmitterException($"Unexpected expression '{unaryExpr.Operand}' ({unaryExpr.Operand.GetType().Name})")
                };

                var tmpName = $"tmp{m_tmpCounter++}";
                WriteLine($"ld ${tmpName}, {rhs}");
                WriteLine($"neg ${tmpName}");
                return $"${tmpName}";
            }

            // These operators change the RHS value, so RHS must be a variable.
            if (unaryExpr.Operand is VariableNode v)
            {
                var op = unaryExpr.Operator.Value switch
                {
                    "-" => "neg",
                    "--" => "dec",
                    "++" => "inc",
                    _ => throw new NotImplementedException($"Unsupported operator '{unaryExpr.Operator.Value}'")
                };

                if (unaryExpr.IsPostfix)
                {
                    // Return the value, then modify the original.
                    var tmpName = $"tmp{m_tmpCounter++}";
                    WriteLine($"ld ${tmpName}, ${v.Name.Value}");
                    WriteLine($"{op} ${v.Name.Value}");
                    return $"${tmpName}";
                }
                
                // Modify the original, then return the value.
                WriteLine($"{op} ${v.Name.Value}");
                return $"${v.Name.Value}";
            }
            
            throw new EmitterException($"Unexpected expression '{unaryExpr.Operand}' ({unaryExpr.Operand.GetType().Name})");
        }
        
        if (exprNode is BinaryExprNode binaryExpr)
        {
            var tmpName = $"tmp{m_tmpCounter++}"; 
            WriteLine($"ld ${tmpName}, {EmitExpression(binaryExpr.Left)}");

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
                _ => throw new NotImplementedException($"Unsupported operator '{binaryExpr.Operator.Value}'")
            };

            if (op == "and")
            {
                // Special case - The second expression should not be executed if the first is false.
                var skipLabel = $"logic_skip{m_skipLabelCounter++}";
                WriteLine($"test ${tmpName}");
                WriteLine($"jmp_z ${tmpName}, {skipLabel}");
                WriteLine($"{op} ${tmpName}, {EmitExpression(binaryExpr.Right)}");
                WriteLine($"test ${tmpName}");
                WriteLine($"{skipLabel}:");
            } else if (op == "or")
            {
                // Special case - The second expression should not be executed if the first is true.
                var skipLabel = $"logic_skip{m_skipLabelCounter++}";
                WriteLine($"test ${tmpName}");
                WriteLine($"jmp_nz ${tmpName}, {skipLabel}");
                WriteLine($"{op} ${tmpName}, {EmitExpression(binaryExpr.Right)}");
                WriteLine($"test ${tmpName}");
                WriteLine($"{skipLabel}:");
            }
            else
            {
                WriteLine($"{op} ${tmpName}, {EmitExpression(binaryExpr.Right)}");
            }
            
            return $"${tmpName}";
        }

        throw new EmitterException($"Unexpected expression '{exprNode}' ({exprNode?.GetType().Name})");
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
        var startLabel = $"for{labelSuffix}_start";
        WriteLine($"{startLabel}:");

        // Support break/continue.
        var incrementLabel = $"for{labelSuffix}_incr";
        var endLabel = $"for{labelSuffix}_end";
        m_loopStack.Push((continueLabel: incrementLabel, breakLabel: endLabel));

        // Condition check.
        var repeat = EmitExpression(forNode.Condition);
        WriteLine($"jmp_z {repeat}, for{labelSuffix}_end");

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
}