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
using JetBrains.Annotations;

namespace TetraCore;

public static class Optimizer
{
    public static Program Optimize([NotNull] this Program program)
    {
        var originalSize = program.Instructions.Sum(o => o.Operands.Length + 1);
        var originalLoc = program.Instructions.Length;

        int postChangeSize;
        var allowReuseOfTemps = false;
        while (true)
        {
            var preChangeSize = program.Instructions.Sum(o => o.Operands.Length + 1);

            InlineConstantLoadIfUsedOnce(program);
            RemoveRedundantDimInstructions(program);
            FoldNegatedConstantLoads(program.Instructions);
            RemoveUnusedVariableDeclarations(program);
            RemoveSelfTargetedJump(program);
            InlineTempLoadUsedImmediately(program);
            CombineAdjacentDeclStatements(program.Instructions);
            FoldUnaryOpResultIntoDestination(program);
            InlineTemps(program);

            if (allowReuseOfTemps)
                ReuseExpiredTemporarySlots(program);
            
            RemoveRedundantSwapAssignments(program);
            RemoveUnusedDeclarations(program);
            FoldLoadIntoUnaryOp(program);

            StripNops(ref program);

            postChangeSize = program.Instructions.Sum(o => o.Operands.Length + 1);
            if (postChangeSize != preChangeSize)
                continue; // Iterate again.

            // No improvements detected.
            if (!allowReuseOfTemps)
            {
                // Reusing temps makes detecting variable usage difficult, so we
                // only do it after all other optimizations have occurred.
                allowReuseOfTemps = true;
                continue;
            }

            // We're done.
            break;
        }

        Console.WriteLine($"Optimized size: {postChangeSize:N0} (was {originalSize:N0})");
        Console.WriteLine($"                {program.Instructions.Length:N0} LOC (was {originalLoc:N0})");
        return program;
    }

    /// <summary>
    /// Optimizes variable usage by replacing a temporary variable's usage with its source value when possible.
    /// For example:
    ///   ld $tmp1,$c
    ///   ld $v,$tmp1,$tmp2  =>  ld $v,$c,$tmp2
    /// </summary>
    private static void InlineTemps(Program program)
    {
        var instructions = program.Instructions;
        for (var i = 0; i < instructions.Length - 1; i++)
        {
            var ldInstruction = instructions[i];
            if (ldInstruction.OpCode != OpCode.Ld || ldInstruction.Operands.Length != 2)
                continue;
            var toAssign = ldInstruction.Operands[1].Name;
            if (toAssign == null || !string.IsNullOrEmpty(toAssign.Swizzle) || toAssign.ArrIndex.HasValue)
                continue;
            var tmpN = ldInstruction.Operands[0].Name;
            if (!tmpN.IsTemporary(program.SymbolTable))
                continue;

            // Look ahead to find the next end of scope/return instruction.
            var indicesUntilEndOfScope = GetIndicesUntilEndOfScope(instructions, i + 1);
            if (indicesUntilEndOfScope.Count == 0)
                continue;

            // tmpN must only be used once.
            var instructionsUsingVariable =
                indicesUntilEndOfScope
                    .Select(o => program.Instructions[o])
                    .Where(o => o.Operands.Any(op => tmpN.IsNameEqual(op.Name)))
                    .ToArray();
            if (instructionsUsingVariable.Length != 1)
                continue; // More than one use - Ignore.
            var nextInstruction = instructionsUsingVariable[0];
                    
            // Variable must not be used anywhere else (as something could modify it).
            if (indicesUntilEndOfScope
                .Select(o => program.Instructions[o])
                .Any(o => o.Operands?.FirstOrDefault()?.Name?.IsNameEqual(toAssign) == true))
                continue;
                    
            for (var j = 0; j < nextInstruction.Operands.Length; j++)
            {
                var operand = nextInstruction.Operands[j];
                if (operand.Name?.IsNameEqual(tmpN) == true)
                    nextInstruction.Operands[j] = operand.RenamedTo(toAssign);
            }
            ReplaceWithNop(instructions, i);
        }
    }

    /// <summary>
    /// Optimizes instructions by merging load operations with direct assignments that follow them.
    /// For example, converts:
    ///   ld $a, $b
    ///   floor $a, $a
    /// Into:
    ///   floor $a, $b
    /// </summary>
    // FoldLoadIntoUnaryOp
    private static void FoldLoadIntoUnaryOp(Program program)
    {
        var instructions = program.Instructions;
        for (var i = 0; i < instructions.Length - 1; i++)
        {
            var ldInstruction = instructions[i];
            if (ldInstruction.OpCode != OpCode.Ld || ldInstruction.Operands.Length != 2)
                continue;
            var nextAssignmentInstruction = instructions[i + 1];
            if (!nextAssignmentInstruction.IsDirectAssignment())
                continue;
            if (nextAssignmentInstruction.Operands.Length != 2)
                continue;
                    
            // Instruction args must both be the same.
            var instrA1 = ldInstruction.Operands[0].Name;
            var instrA2 = nextAssignmentInstruction.Operands[0].Name;
            if (instrA1 == null || instrA2 == null || !instrA1.Equals(instrA2))
                continue;
                    
            // Instruction must be the target of the 'ld'.
            if (!instrA1.Equals(ldInstruction.Operands[0].Name))
                continue;
                    
            // Replace the 'ld' with the next instruction.
            nextAssignmentInstruction.Operands[1] = ldInstruction.Operands[1];
            ReplaceWithNop(instructions, i);
        }
    }

    /// <summary>
    /// Removes cyclic assignments where a variable's value is loaded then immediately loaded back, making the second load redundant.
    /// For example:
    ///   ld $a,$b
    ///   ld $b,$a // Can be removed.
    /// </summary>
    // RemoveRedundantSwapAssignments
    private static void RemoveRedundantSwapAssignments(Program program)
    {
        var instructions = program.Instructions;
        for (var i = 0; i < instructions.Length - 1; i++)
        {
            var ldInstruction = instructions[i];
            if (ldInstruction.OpCode != OpCode.Ld)
                continue;

            var srcVarName = ldInstruction.Operands[1].Name;
            if (srcVarName == null)
                continue; // Not a variable.
            var dstVarName = ldInstruction.Operands[0].Name;
            if (dstVarName == null)
                continue; // Not a variable.

            var secondLdInstruction = instructions[i + 1];
            if (secondLdInstruction.OpCode != OpCode.Ld)
                continue;

            if (!srcVarName.Equals(secondLdInstruction.Operands[0].Name))
                continue;
            if (!dstVarName.Equals(secondLdInstruction.Operands[1].Name))
                continue;

            // Found a cyclic assignment - Remove it.
            ReplaceWithNop(instructions, i + 1);
        }
    }

    private static void RemoveUnusedDeclarations(Program program)
    {
        var instructions = program.Instructions;

        // Skip globals - They can be used anywhere.
        var startIndex = instructions.TakeWhile(o => !HasJmpTarget(o)).Count();
        
        for (var i = startIndex; i < instructions.Length; i++)
        {
            var declInstruction = instructions[i];
            if (declInstruction.OpCode != OpCode.Decl)
                continue;

            // Look ahead to find the next end of scope/return instruction.
            var indicesUntilEndOfScope = GetIndicesUntilEndOfScope(instructions, i + 1);
            if (indicesUntilEndOfScope.Count == 0)
                continue;
            
            // Check if each variable name is used.
            var instructionsUntilEndOfScope =
                indicesUntilEndOfScope
                    .Select(o => instructions[o]);
            var operandsUntilEndOfScope =
                instructionsUntilEndOfScope
                    .SelectMany(o => o.Operands);

            var seenSlots = new HashSet<int>();
            var currentDeclOperands = declInstruction.Operands.Where(varName => seenSlots.Add(varName.Name.Slot));
            var usedDeclOperands =
                currentDeclOperands
                    .Where(o => operandsUntilEndOfScope.Any(op => o.Name.IsNameEqual(op.Name)))
                    .ToArray();
            
            if (usedDeclOperands.Length == 0)
            {
                // Nothing to declare - Remove the 'decl'.
                ReplaceWithNop(instructions, i);
                continue;
            }

            instructions[i] = declInstruction.WithOperands(usedDeclOperands);
        }
    }

    /// <summary>
    /// Optimizes temporary variable usage by reusing variables when their scope has ended.
    /// For example, given:
    ///   ld $tmp4,$p
    ///   floor $tmp4,$tmp4
    ///   ld $ip,$tmp4
    ///   ld $tmp5,$p
    /// The $tmp5 variable can reuse $tmp4's slot since $tmp4 is no longer used.
    /// </summary>
    // ReuseExpiredTemporarySlots
    private static void ReuseExpiredTemporarySlots(Program program)
    {
        var instructions = program.Instructions;
        for (var i = 0; i < instructions.Length; i++)
        {
            // Instruction needs to assign to a variable.
            var ldInstruction = instructions[i];
            if (!ldInstruction.IsDirectAssignment() || ldInstruction.Operands[0]?.Name.IsTemporary(program.SymbolTable) != true)
                continue;

            // Look ahead to find the next end of scope/return instruction.
            var indicesUntilEndOfScope = GetIndicesUntilEndOfScope(instructions, i);
            if (indicesUntilEndOfScope.Count == 0)
                continue;

            bool isModified;
            do
            {
                isModified = false;
                
                // Find all assignments to temp variables.
                var visited = new HashSet<VarName>();
                var ldTmpItems =
                    indicesUntilEndOfScope
                        .Where(o =>
                        {
                            // Must be assignment to a tmp variable.
                            var instruction = instructions[o];
                            if (!instruction.IsDirectAssignment())
                                return false;
                            var varName = instruction.Operands[0]?.Name;
                            if (varName?.IsTemporary(program.SymbolTable) != true)
                                return false;
                            return visited.Add(varName);
                        })
                        .Select(assignmentIndex =>
                        {
                            // Record where the assignment is, and find the last use of the tmp.
                            var ld = instructions[assignmentIndex];
                            var tmpName = ld.Operands[0].Name;
                            var lastUseIndex = indicesUntilEndOfScope.Last(o => instructions[o].Operands.Any(op => tmpName.IsNameEqual(op.Name)));
                            return (ld, assignmentIndex, lastUseIndex);
                        })
                        .ToArray();

                // Find first case where a tmp assignment is after the end of a previous one.
                foreach (var ldTmpItem in ldTmpItems)
                {
                    var toReplace = ldTmpItems.FirstOrDefault(o => o.assignmentIndex > ldTmpItem.lastUseIndex);
                    if (toReplace == default)
                        continue; // No later candidate found.

                    var srcVarName = ldTmpItem.ld.Operands[0].Name;
                    var dstVarName = toReplace.ld.Operands[0].Name;

                    // Replace later tmp name with the first one.
                    for (var j = toReplace.assignmentIndex; j <= toReplace.lastUseIndex; j++)
                    {
                        var o = instructions[j];
                        for (var k = 0; k < o.Operands.Length; k++)
                        {
                            if (dstVarName.IsNameEqual(o.Operands[k].Name))
                            {
                                o.Operands[k] = o.Operands[k].RenamedTo(srcVarName);
                                isModified = true;
                            }
                        }
                    }

                    if (isModified)
                        break;
                }
            }
            while (isModified);
        }
    }
    
    /// <summary>
    /// Optimizes the program by inlining load (ld) instructions that are only used on the next line.
    /// For example, if we have:
    ///   ld $tmp26,$p[0]
    ///   mix $tmp17,$tmp21,$tmp26
    /// This will be optimized to:
    ///   mix $tmp17,$tmp21,$p[0]
    /// </summary>
    /// <param name="program">The program to optimize</param>
    // InlineTempLoadUsedImmediately
    private static void InlineTempLoadUsedImmediately(Program program)
    {
        var instructions = program.Instructions;
        for (var i = 0; i < instructions.Length - 1; i++)
        {
            var ldInstruction = instructions[i];
            if (ldInstruction.OpCode != OpCode.Ld)
                continue;
            if (ldInstruction.Operands.Length != 2)
                continue;

            // Look ahead to find the next end of scope/return instruction.
            var indicesUntilEndOfScope = GetIndicesUntilEndOfScope(instructions, i + 1);
            if (indicesUntilEndOfScope.Count == 0)
                continue;

            // Find instructions using the variable.
            var varName = ldInstruction.Operands[0].Name;
            var instructionsUsingVariable =
                indicesUntilEndOfScope
                    .Select(o => program.Instructions[o])
                    .Where(o => o.Operands.Any(op => varName.IsNameEqual(op.Name)))
                    .ToArray();

            // The `ld` variable must be used on the line after the `ld`.
            if (instructionsUsingVariable.Length != 1)
                continue;
            var usageInstrIndex = Array.IndexOf(instructions, instructionsUsingVariable[0]);
            if (usageInstrIndex != i + 1)
                continue;

            // Replace variable usage with the source value.
            var valueOperand = ldInstruction.Operands[1];
            var srcHasArrayIndexOrSwizzle = valueOperand.Name.Swizzle != null || valueOperand.Name.ArrIndex != null;
            var nextInstruction = instructions[i + 1];
            var didChange = false;
            for (var j = 0; j < nextInstruction.Operands.Length; j++)
            {
                if (!varName.IsNameEqual(nextInstruction.Operands[j].Name))
                    continue; // Not replacing this operand.
                
                var targetHasArrayIndexOrSwizzle = nextInstruction.Operands[j].Name.Swizzle != null || nextInstruction.Operands[j].Name.ArrIndex != null;
                if (srcHasArrayIndexOrSwizzle || !targetHasArrayIndexOrSwizzle)
                {
                    // Replace target with the source operand, retaining any swizzle or array index
                    // the source may have.
                    nextInstruction.Operands[j] = valueOperand;
                }
                else
                {
                    // Target has swizzle or array index, so just replace its name component.
                    nextInstruction.Operands[j] = nextInstruction.Operands[j].RenamedTo(valueOperand.Name);
                }
                
                didChange = true;
            }
            
            if (didChange)
                ReplaceWithNop(instructions, i);
        }
    }

    /// <summary>
    /// Remove jump instructions that target the next instruction.
    /// </summary>
    // RemoveSelfTargetedJump
    private static void RemoveSelfTargetedJump(Program program)
    {
        var instructions = program.Instructions;
        instructions
            .Where(IsJmp)
            .Where(o => o.Operands[^1].Int == Array.IndexOf(instructions, o) + 1)
            .ForEach(jmp => ReplaceWithNop(instructions, Array.IndexOf(instructions, jmp)));
    }

    /// <summary>
    /// Remove dimension instructions that don't affect the actual size of arrays.
    /// </summary>
    // RemoveRedundantDimInstructions
    private static void RemoveRedundantDimInstructions(Program program)
    {
        var instructions = program.Instructions;
        var jumpTargets = FindJumpTargets(instructions);
        for (var i = 1; i < instructions.Length; i++)
        {
            var ldInstruction = instructions[i - 1];
            if (ldInstruction.OpCode != OpCode.Ld)
                continue;
            var dimInstruction = instructions[i];
            if (dimInstruction.OpCode != OpCode.Dim)
                continue;

            // `ld` and `dim` must have constant operands.
            var isLdWithConstants = ldInstruction.Operands.Skip(1).All(o => o.IsNumeric());
            if (!isLdWithConstants)
                continue;
            var isDimWithConstant = dimInstruction.Operands[^1].Type is OperandType.Int;
            if (!isDimWithConstant)
                continue;
                    
            // See if `dim` is a no-op.
            var dimOperand = dimInstruction.Operands[^1].Int;
            if (dimOperand != ldInstruction.Operands.Length - 1)
                continue;
                    
            // Can't remove a jump target.
            if (jumpTargets.Contains(i))
                continue;
                    
            // Remove `dim` - It's a no-op.
            ReplaceWithNop(instructions, i);
        }
    }

    /// <summary>
    /// Optimize away single-use constant loads by inlining them directly into their usage point.
    /// </summary>
    // InlineConstantLoadIfUsedOnce
    private static void InlineConstantLoadIfUsedOnce(Program program)
    {
        var instructions = program.Instructions;
        for (var i = 0; i < instructions.Length; i++)
        {
            var ldInstruction = instructions[i];
            if (ldInstruction.OpCode != OpCode.Ld)
                continue;

            // The `ld` operands must be a single constant.
            var isAllOperandsConstant = ldInstruction.Operands.Skip(1).All(o => o.IsNumeric());
            if (!isAllOperandsConstant)
                continue;
            
            // Can't be an 'argN' param - They're needed for function calls.
            var varName = ldInstruction.Operands[0].Name;
            if (varName.IsFunctionArgument(program.SymbolTable))
                continue;

            // Look ahead to find the next end of scope/return instruction.
            var indicesUntilEndOfScope = GetIndicesUntilEndOfScope(instructions, i + 1);
            if (indicesUntilEndOfScope.Count == 0)
                continue;
            
            // Find instructions using the variable.
            var instructionsUsingVariable =
                indicesUntilEndOfScope
                    .Select(o => program.Instructions[o])
                    .Where(o => o.Operands.Any(op => varName.IsNameEqual(op.Name)))
                    .ToArray();
            
            // The `ld` variable must be used exactly once.
            if (instructionsUsingVariable.Length != 1)
                continue;
            var usageCount = instructionsUsingVariable.Sum(o => o.Operands.Count(op => varName.IsNameEqual(op.Name)));
            if (usageCount != 1)
                continue;
            
            // Inline const `ld` definition into the usage point.
            var usageInstruction = instructionsUsingVariable[0];
            var usageOperandIndex = Array.FindIndex(usageInstruction.Operands, o => varName.IsNameEqual(o.Name));
            var newOperands = usageInstruction.Operands.Where(o => !Equals(o.Name, varName)).ToList();
            newOperands.InsertRange(usageOperandIndex, ldInstruction.Operands.Skip(1));
            var instToModifyIndex = Array.IndexOf(instructions, usageInstruction);
            instructions[instToModifyIndex] = usageInstruction.WithOperands(newOperands.ToArray());
            ReplaceWithNop(instructions, i);
        }
    }

    /// <summary>
    /// Remove declarations for variables that are never used
    /// </summary>
    // RemoveUnusedVariableDeclarations
    private static void RemoveUnusedVariableDeclarations(Program program)
    {
        var instructions = program.Instructions;
        
        // Skip globals - They can be used anywhere.
        var startIndex = instructions.TakeWhile(o => !HasJmpTarget(o)).Count();
        
        for (var i = startIndex; i < instructions.Length; i++)
        {
            var declInstruction = instructions[i];
            if (declInstruction.OpCode != OpCode.Decl)
                continue;

            // Look ahead to find the next end of scope/return instruction.
            var indicesUntilEndOfScope = GetIndicesUntilEndOfScope(instructions, i + 1);
            if (indicesUntilEndOfScope.Count == 0)
                continue;

            var unusedVariableNames = new List<VarName>();
            var declaredVariableNames = declInstruction.Operands.Select(o => o.Name).ToArray();
            foreach (var varName in declaredVariableNames)
            {
                // Find instructions using the variable.
                var isUsed =
                    indicesUntilEndOfScope
                        .Select(o => program.Instructions[o])
                        .Any(o => o.Operands.Any(op => varName.IsNameEqual(op.Name)));
                if (!isUsed)
                    unusedVariableNames.Add(varName);
            }

            if (unusedVariableNames.Count == 0)
                continue; // No unused variable names - Nothing to do.
            
            // Remove the unused entries.
            var newOperands = declInstruction.Operands.Where(o => !unusedVariableNames.Contains(o.Name)).ToArray();
            if (newOperands.Length == 0)
            {
                // Declaration is unused - Make it a `nop`.
                ReplaceWithNop(instructions, i);
                continue;
            }
            
            instructions[i] = declInstruction.WithOperands(newOperands);
        }
    }

    private static List<int> GetIndicesUntilEndOfScope(Instruction[] instructions, int startIndex)
    {
        // Look ahead to find the next end of scope/return instruction.
        var indicesUntilEndOfScope = new List<int>();
        for (var j = startIndex; j < instructions.Length; j++)
        {
            if (instructions[j].OpCode == OpCode.Ret || instructions[j].OpCode == OpCode.Halt)
                break;
            indicesUntilEndOfScope.Add(j);
        }
        return indicesUntilEndOfScope;
    }
    
    private static void StripNops(ref Program program)
    {
        var reducedInstructions = program.Instructions.ToList();

        var didModify = false;
        int nopIndex;
        while ((nopIndex = reducedInstructions.FindIndex(o => o.OpCode == OpCode.Nop)) >= 0)
        {
            didModify = true;
            
            // Remove the `nop`.
            reducedInstructions.RemoveAt(nopIndex);
            
            // Update the label table.
            foreach (var kvp in program.LabelTable.Where(kvp => kvp.Value > nopIndex))
                program.LabelTable[kvp.Key]--;
            
            // Update jmp targets.
            foreach (var instruction in program.Instructions)
            {
                var isJmp = IsJmp(instruction) || instruction.OpCode == OpCode.Call;
                if (!isJmp)
                    continue;
                
                var target = instruction.Operands[^1].Int;
                if (target > nopIndex)
                    instruction.Operands[^1].Floats[0]--;
            }
        }

        if (didModify)
            program = program.WithInstructions(reducedInstructions.ToArray());
    }

    /// <summary>
    /// Merge negation operation into load constant operations where possible
    /// </summary>
    // FoldNegatedConstantLoads
    private static void FoldNegatedConstantLoads(Instruction[] instructions)
    {
        // Inline negation of constants.
        var jumpTargets = FindJumpTargets(instructions);
        for (var i = 1; i < instructions.Length; i++)
        {
            if (instructions[i].OpCode != OpCode.Neg)
                continue;
            
            // Must be preceded by a `ld`.
            var prevInstruction = instructions[i - 1];
            if (prevInstruction.OpCode != OpCode.Ld)
                continue;
            
            // `neg` must be negating the `ld` target operand.
            if (!Equals(instructions[i].Operands[0].Name, prevInstruction.Operands[0].Name))
                continue;

            // We can't remove a jump target.
            if (jumpTargets.Contains(i))
                continue;

            // The `ld` operands must be constants.
            var isOperandsConstant = prevInstruction.Operands.Skip(1).All(o => o.Type is OperandType.Int or OperandType.Float);
            if (!isOperandsConstant)
                continue;
            
            // Negate the constants, to avoid the need for 'neg'.
            for (var operandIndex = 1; operandIndex < prevInstruction.Operands.Length; operandIndex++)
            {
                for (var j = 0; j < prevInstruction.Operands[operandIndex].Floats.Length; j++)
                    instructions[i - 1].Operands[operandIndex].Floats[j] *= -1;
            }

            // Replace the `ld` and `neg` with a `nop`.
            ReplaceWithNop(instructions, i);
        }
    }

    /// <summary>
    /// Combine consecutive declaration instructions into a single declaration
    /// </summary>
    // CombineAdjacentDeclStatements
    private static void CombineAdjacentDeclStatements(Instruction[] instructions)
    {
        var jumpTargets = FindJumpTargets(instructions);
        for (var i = 0; i < instructions.Length - 1; i++)
        {
            if (instructions[i].OpCode != OpCode.Decl)
                continue;
            
            // Count until a control flow statement.
            var count = 0;
            while (i + count < instructions.Length)
            {
                var isControlFlow = IsJmp(instructions[i + count]);
                if (isControlFlow)
                    break;
                
                switch (instructions[i + count].OpCode)
                {
                    case OpCode.Call:
                    case OpCode.Ret:
                    case OpCode.Halt:
                    case OpCode.PushFrame:
                    case OpCode.PopFrame:
                        isControlFlow = true;
                        break;
                }
                if (isControlFlow)
                    break;
                
                count++;
            }
            
            if (count == 0)
                continue;

            var startIndex = i + 1;
            var endIndex = i + count - 1;
            
            // If there's a jump target in the instruction range, we can't combine ops past this point.
            foreach (var jumpTarget in jumpTargets)
            {
                if (jumpTarget >= startIndex && jumpTarget <= endIndex)
                {
                    endIndex = jumpTarget - 1;
                    break;
                }
            }

            for (var j = startIndex; j <= endIndex; j++)
            {
                var nextInstruction = instructions[j];
                if (nextInstruction.OpCode == OpCode.Decl)
                {
                    instructions[i] = instructions[i].WithOperands(instructions[i].Operands.Union(nextInstruction.Operands).ToArray());
                    ReplaceWithNop(instructions, j);
                }
            }

            i = endIndex;
        }
    }

    private static bool IsJmp(Instruction instruction) =>
        instruction.OpCode is OpCode.Jmp or OpCode.Jmpz or OpCode.Jmpnz;

    private static bool HasJmpTarget(Instruction instruction) =>
        instruction.OpCode is OpCode.Call || IsJmp(instruction);

    private static void ReplaceWithNop(Instruction[] instructions, int i) =>
        instructions[i] = new Instruction { OpCode = OpCode.Nop };

    private static int[] FindJumpTargets(Instruction[] instructions) =>
        instructions.Where(HasJmpTarget).Select(o => o.Operands[^1].Int).Order().ToArray();

    /// <summary>
    /// Merges a direct assignment followed by a load into a single assignment.
    /// For example:
    ///   fract $tmpN,$b
    ///   ld $c,$tmpN
    /// becomes:
    ///   fract $c, $b
    /// </summary>
    // FoldUnaryOpResultIntoDestination
    private static void FoldUnaryOpResultIntoDestination(Program program)
    {
        var instructions = program.Instructions;
        for (var i = 0; i < instructions.Length - 2; i++)
        {
            var assignment = instructions[i];
            if (!assignment.IsDirectAssignment())
                continue;

            var ld = instructions[i + 1];
            if (ld.OpCode != OpCode.Ld || ld.Operands.Length != 2)
                continue;

            var tmpN = assignment.Operands[0].Name;
            if (!tmpN.IsTemporary(program.SymbolTable))
                continue;

            var ldFrom = ld.Operands[1].Name;
            if (!tmpN.Equals(ldFrom))
                continue;
            var ldTo = ld.Operands[0];

            // Look ahead to find the next end of scope/return instruction.
            var indicesUntilEndOfScope = GetIndicesUntilEndOfScope(instructions, i + 2);
            if (indicesUntilEndOfScope.Count == 0)
                continue;

            // Check no instructions using the tmpN variable.
            var isTmpUsedLater =
                indicesUntilEndOfScope
                    .Select(o => program.Instructions[o])
                    .Any(o => o.Operands.Any(op => tmpN.IsNameEqual(op.Name)));
            if (isTmpUsedLater)
                continue;

            assignment.Operands[0] = ldTo;
            ReplaceWithNop(instructions, i + 1);
        }
    }
}
