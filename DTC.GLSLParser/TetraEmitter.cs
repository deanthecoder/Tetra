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
using System.Linq;
using System.Text;
using DTC.Core.Extensions;

namespace DTC.GLSLParser;

public class TetraEmitter
{
    private readonly StringBuilder m_sb = new();
    private int m_tmpCounter;
    private int m_forLoopCounter;

    public string Emit(ProgramNode program)
    {
        m_sb.Clear();
        m_tmpCounter = 0;
        m_forLoopCounter = 0;

        // Implicit call to main().
        WriteLine("# Entry point.");
        WriteLine("call main");
        WriteLine("halt");
        WriteLine();
        
        // Emit program statements.
        EmitNode(program);

        return m_sb.ToString();
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
            
            case AssignmentNode assign:
                EmitAssignment(assign);
                break;
            
            case ForNode forNode:
                EmitFor(forNode);
                break;

            default:
                throw new NotImplementedException($"Unsupported statement: '{node}' ({node.GetType().Name})");
        }
    }
    
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

    private void EmitCall(CallExprNode call)
    {
        for (var i = 0; i < call.Arguments.Length; i++)
            WriteLine($"ld $arg{i}, {EmitExpression(call.Arguments[i])}");

        WriteLine($"call {call.FunctionName.Value}");
    }

    private void EmitVariableDeclaration(VariableDeclarationNode decl)
    {
        WriteLine($"ld ${decl.Name.Value}, {EmitExpression(decl.Value)}");
    }

    private string EmitExpression(ExprStatementNode exprNode)
    {
        if (exprNode is VariableNode variable)
            return $"${variable.Name.Value}";

        if (exprNode is LiteralNode literal)
            return $"{literal.Value.Value}";

        if (exprNode is CallExprNode call)
        {
            EmitCall(call);
            return "$retval";
        }

        if (exprNode is UnaryExprNode unaryExpr)
        {
            // Check if RHS is not to be directly modified.
            if (unaryExpr.Operator.Value == "-")
            {
                var tmpName = $"tmp{m_tmpCounter++}";
                var rhs = unaryExpr.Operand switch
                {
                    LiteralNode literalExpr => literalExpr.Value.Value,
                    VariableNode variableExpr => $"${variableExpr.Name.Value}",
                    _ => throw new EmitterException($"Unexpected expression '{unaryExpr.Operand}' ({unaryExpr.Operand.GetType().Name})")
                };

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
                "==" => "eq",
                "!=" => "ne",
                "<" => "lt",
                "<=" => "le",
                ">" => "gt",
                ">=" => "ge",
                "&&" => "and",
                "||" => "or",
                _ => throw new NotImplementedException($"Unsupported operator '{binaryExpr.Operator.Value}'")
            };
            WriteLine($"{op} ${tmpName}, {EmitExpression(binaryExpr.Right)}");
            
            return $"${tmpName}";
        }

        throw new EmitterException($"Unexpected expression '{exprNode}' ({exprNode.GetType().Name})");
    }
    
    private void EmitAssignment(AssignmentNode assign)
    {
        WriteLine($"ld ${assign.Target}, {EmitExpression(assign.Value)}");
    }

    private void EmitFor(ForNode forNode)
    {
        //WriteLine("push_frame");
        
        // Setup.
        EmitNode(forNode.Init);
        
        // Loop start label.
        var labelSuffix = m_forLoopCounter++;
        var startLabel = $"for_start_{labelSuffix}";
        WriteLine($"{startLabel}:");
        
        // Condition check.
        var repeat = EmitExpression(forNode.Condition);
        WriteLine($"jmp_z {repeat}, for_end_{labelSuffix}");

        // Loop body.
        if (forNode.Body is BlockNode blockNode)
            blockNode.Statements.ForEach(EmitNode);
        else
            EmitNode(forNode.Body);
        
        // Loop increment.
        EmitExpression(forNode.Step);
        
        // Loop end.
        WriteLine($"jmp {startLabel}");
        WriteLine($"for_end_{labelSuffix}:");
        
        // Cleanup.
        //WriteLine("pop_frame");
    }
}