/*************************************************************************************************
 * Execution Helper                                                                              *
 * ============================================================================================= *
 * This file contains the class that provides various helper functions that allow                *
 * the symbolic execution of a path in the flow graph of a Phoenix function unit.                *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Phx;
using Phx.Graphs;
using Phx.IR;

using Utilities;


namespace GameTime
{
    /// <summary>
    /// This class defines various helper functions that allow the symbolic
    /// execution of a path in the flow graph of a function unit.
    /// </summary>
    class ExecutionHelper
    {
        /// <summary>
        /// Traces the definition of the input operand and returns its symbolic
        /// Expression at the source level.
        /// </summary>
        /// 
        /// <param name="operand">Operand whose definition needs to be traced.</param>
        /// <param name="varOperand">Current variable operand whose definition is being traced,
        /// which may be different from <paramref name="operand"/>.</param>
        /// <param name="path">Path along which to trace the definition.</param>
        /// 
        /// <returns>Symbolic Expression that corresponds to the input operand.</returns>
        /// <remarks>Precondition: <paramref name="operand"/> and <paramref name="varOperand"/>
        /// are not null.</remarks>
        /// <remarks>Postcondition: The resulting Expression is not null.</remarks>
        public static Expression TraceOperandBackward(Operand operand,
            Operand varOperand, Path path)
        {
            Trace.Assert(operand != null, "PHOENIX: operand cannot be null.");
            Trace.Assert(varOperand != null, "PHOENIX: varOperand cannot be null.");

            if (path.ProjectConfig.debugConfig.DUMP_INSTRUCTION_TRACE)
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine(operand.Instruction.ToString());
                Console.Out.WriteLine("{ Exploring " + operand +
                    " while converting " + varOperand);
            }

            Expression result = null;

            if (path.OperandExpressions.ContainsKey(operand))
            {
                result = path.OperandExpressions[operand].Clone();
                if (path.ProjectConfig.debugConfig.DUMP_INSTRUCTION_TRACE)
                {
                    Console.Out.WriteLine("  Memoization provides the result.");
                    Console.Out.WriteLine("  Result is " + result + " }");
                }
                return result;
            }

            Instruction defInstruction = operand.DefinitionInstruction;
            if (operand.IsImmediateOperand)
            {
                string opValue = "";

                ImmediateOperand immOperand = operand.AsImmediateOperand;
                if (immOperand.Value.IsIntValue)
                {
                    opValue = immOperand.IntValue.ToString();
                }
                else if (immOperand.Value.IsFloatValue)
                {
                    double floatValue = immOperand.FloatValue64;
                    long longValue = Convert.ToInt64(floatValue);
                    Console.Out.WriteLine("PHOENIX: WARNING: Floating point constant " +
                        "discovered during backward symbolic execution: " + floatValue + ". " +
                        "It will be cast to the integer " + longValue + ".");
                    opValue = longValue.ToString();
                }
                else
                {
                    throw new NotSupportedException("PHOENIX: " +
                        "Support for this type of immediate operand not implemented.");
                }

                result = new Constant(opValue, operand.BitSize);
                result.Type = operand.Type;
            }
            else if (operand.IsMemoryOperand)
            {
                /* This case implies a source-level dereference. */
                result = TraceMemoryOperandBackward(operand, varOperand, path);
            }
            else if ((defInstruction != null) &&
                (defInstruction.Opcode.Id == Phx.Common.Opcode.Index.Chi) &&
                (defInstruction.SourceOperand1.DefinitionInstruction.Opcode.Id ==
                    Phx.Common.Opcode.Index.Start))
            {
                result = PhoenixHelper.MakeVariableExpression(operand,
                    PhoenixHelper.GetOperandName(operand), path);
            }
            else if (defInstruction == null)
            {
                /* We are tracing a global operand. */
                result = PhoenixHelper.MakeVariableExpression(operand,
                    PhoenixHelper.GetOperandName(operand), path);
            }
            else if (!path.Blocks.Contains(defInstruction.BasicBlock))
            {
                result = PhoenixHelper.MakeVariableExpression(operand,
                    PhoenixHelper.GetOperandName(operand), path);
            }
            else
            {
                switch (defInstruction.InstructionKind)
                {
                    case InstructionKind.ValueInstruction:
                        ValueInstruction valueInstruction = defInstruction.AsValueInstruction;
                        result =
                            ExecuteValueInstructionBackward(valueInstruction,
                                varOperand, false, path);
                        break;

                    case InstructionKind.CallInstruction:
                        result =
                            PhoenixHelper.MakeCallVariableExpression(operand, defInstruction,
                                varOperand, path);
                        break;

                    case InstructionKind.CompareInstruction:
                        CompareInstruction compareInstruction =
                            defInstruction.AsCompareInstruction;
                        result =
                            ExecuteComparisonInstructionBackward(compareInstruction,
                                varOperand, path);
                        break;

                    default:
                        result =
                            ExecuteSpecialInstructionBackward(operand, defInstruction,
                                varOperand, path);
                        break;
                }
            }

            Trace.Assert(result != null, "PHOENIX: Result Expression should not be null.");
            Trace.Assert(result.Type != null, "PHOENIX: Result Expression should have a type.");

            if (path.ProjectConfig.debugConfig.DUMP_INSTRUCTION_TRACE)
            {
                Console.Out.WriteLine("  Result is " + result + " }");
            }

            /* Remember the result for later, in case it is ever needed. */
            path.OperandExpressions[operand] = result;
            return result;
        }

        /// <summary>
        /// Traces the definition of the input memory operand (for a source-level dereference)
        /// and returns its symbolic Expression at the source level.
        /// </summary>
        /// 
        /// <param name="memoryOperand">Memory operand whose definition needs to be traced.</param>
        /// <param name="varOperand">Current variable operand whose definition is being traced,
        /// which may be different from <paramref name="memoryOperand"/>.</param>
        /// <param name="path">Path along which to trace the definition.</param>
        /// 
        /// <returns>Symbolic Expression that corresponds to the input memory operand.</returns>
        /// <remarks>Precondition: <paramref name="memoryOperand"/> and
        /// <paramref name="varOperand"/> are not null.</remarks>
        /// <remarks>Postcondition: The resulting Expression is not null.</remarks>
        private static Expression TraceMemoryOperandBackward(Operand memoryOperand,
            Operand varOperand, Path path)
        {
            Operand baseOperand = memoryOperand.BaseOperand;
            Expression basePointerExpr = TraceOperandBackward(baseOperand, varOperand, path);

            int offset = memoryOperand.Field.BitOffset;
            if (offset != 0)
            {
                /* If the bit offset of the field from the start of the memory location
                 * is not zero, add this to any existing offsets. */
                basePointerExpr = ExpressionHelper.AddOffsetToPointer(basePointerExpr,
                    new Constant(offset, path.Config.WORD_BITSIZE), path);
            }

            /* Do not dereference the aggregate access if the memory operand and the variable
             * operand, whose definition is being traced, are both aggregates and have the same
             * aggregate type. This case arises when an aggregate is aliased by another. */
            bool derefAggAccess = !(ExpressionHelper.IsAggregateExpressionType(varOperand.Type) &&
                ExpressionHelper.IsAggregateExpressionType(memoryOperand.Type) &&
                (PhoenixHelper.GetAggregateName(varOperand.Type.AsAggregateType) ==
                PhoenixHelper.GetAggregateName(memoryOperand.Type.AsAggregateType)));

            return ExpressionHelper.DereferencePointer(basePointerExpr, memoryOperand.Type,
                derefAggAccess, path);
        }

        /// <summary>
        /// Traces the definition of the operands occurring in the input value instruction
        /// backward and returns the source-level symbolic Expression that corresponds
        /// to the instruction.
        /// </summary>
        /// 
        /// <param name="valueInstruction">Value instruction.</param>
        /// <param name="varOperand">Current variable operand whose definition
        /// is being traced.</param>
        /// <param name="completeTrace">True, if non-temporary variables should be traced
        /// all the way back; false, otherwise. This flag is set to false when generating
        /// conditions, but true when collecting assignments; in the former, we only need
        /// the variable names being assigned to, whereas in the latter, we need the actual
        /// assignment on the right-hand side traced back completely.</param>
        /// <param name="path">Path along which to trace the definition.</param>
        /// <returns>Symbolic Expression that corresponds to the input instruction.</returns>
        public static Expression ExecuteValueInstructionBackward(ValueInstruction valueInstruction,
            Operand varOperand, bool completeTrace, Path path)
        {
            Expression result = null;
            Operand destOperand = valueInstruction.DestinationOperand;

            /* IR instructions that assign to temporary variables should be completely executed
             * backward. Other assignments are actual assignments in the source code. */
            if (!completeTrace && !destOperand.IsTemporary)
            {
                return (destOperand.IsMemoryOperand) ?
                    TraceMemoryOperandBackward(destOperand, varOperand, path) :
                    PhoenixHelper.MakeVariableExpression(destOperand,
                        PhoenixHelper.GetOperandName(destOperand), path);
            }

            /* We will be using the first and second operands considerably. We determine what
             * they are before entering the switch-case statement, to keep the code efficient
             * and clean. */
            List<Operand> srcOperands = new List<Operand>();
            List<Expression> srcExprs = new List<Expression>();
            foreach (Operand srcOperand in valueInstruction.SourceOperands)
            {
                srcOperands.Add(srcOperand);
                srcExprs.Add(TraceOperandBackward(srcOperand, varOperand, path));
            }

            switch (valueInstruction.Opcode.Id)
            {
                case Phx.Common.Opcode.Index.Parenthesis:
                    result = srcExprs[0];
                    break;

                case Phx.Common.Opcode.Index.Add:
                    result = new Expression(OperatorStore.AddOp,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.Subtract:
                    result = new Expression(OperatorStore.SubOp,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.Multiply:
                    result = new Expression(OperatorStore.MultOp,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.Divide:
                    /* Perform signed division only if the two operands are signed integers. */
                    Operator divOpToUse = (srcExprs[0].Type.IsSignedInt &&
                        srcExprs[1].Type.IsSignedInt) ?
                        OperatorStore.SDivOp : OperatorStore.UDivOp;
                    result = new Expression(divOpToUse,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.Remainder:
                    result = new Expression(OperatorStore.RemOp,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.Assign:
                    result = srcExprs[0];
                    break;

                case Phx.Common.Opcode.Index.Convert:
                    /* This case implies a source-level cast. */
                    if (destOperand.IsPointer && srcOperands[0].IsPointer)
                    {
                        /* If a pointer to one type is cast to a pointer of another type, the size
                         * of the pointer does not change. In this case, do not change the size
                         * or the type of the source operand. */
                        result = srcExprs[0];

                        /* Return the result now so that its type does not get modified later in
                         * this function to be that of the destination operand. */
                        return result;
                    }
                    else
                    {
                        result = ExpressionHelper.AdjustBitSize(srcExprs[0],
                            destOperand.BitSize, srcOperands[0].IsUnsignedInt, path);
                    }
                    break;

                case Phx.Common.Opcode.Index.Subscript:
                    /* Dereference the first source Expression, which is a pointer Expression
                     * that refers to the array whose element is being accessed. */
                    Expression arrayExpr = ExpressionHelper.DereferencePointer(srcExprs[0],
                        ExpressionHelper.GetPointerReferentType(srcExprs[0]), true, path);
                    /* Check if the dereferenced source Expression aliases another Expression. */
                    arrayExpr = path.FindAliasedExpression(arrayExpr);

                    /* Determine the indices that are needed to access the array element
                     * that the subscription Expression represents. */
                    List<Expression> accessIndexExprs =
                        ExpressionHelper.GetArrayAccessIndices(arrayExpr,
                            srcExprs.GetRange(1, srcExprs.Count - 1), path);

                    /* Construct an Expression that refers to the array element that is
                     * being accessed. This Expression will be dereferenced later. */
                    result = ExpressionHelper.GetArrayElementReference(arrayExpr,
                        accessIndexExprs, path);

                    /* Add conditions that enforce lower and upper bounds on the indices. */
                    List<int> upperBounds =
                        ExpressionHelper.GetArrayIndicesUpperBounds(arrayExpr.Type, path);
                    int currentBoundIndex = 0;
                    foreach (Expression accessIndexExpr in accessIndexExprs)
                    {
                        Expression boundCondition =
                            ExpressionHelper.AddBoundsOnIndex(accessIndexExpr,
                                upperBounds[currentBoundIndex], path);
                        path.AddCondition(boundCondition, valueInstruction.BasicBlock.Id);
                        currentBoundIndex++;
                    }
                    break;

                case Phx.Common.Opcode.Index.ShiftLeft:
                    result = new Expression(OperatorStore.ShiftLeftOp,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.ShiftRight:
                    /* (From K&R): If the first Expression is an unsigned
                     * integer, a logical right-shift is performed;
                     * otherwise, an arithmetic right-shift is performed. */
                    Operator srOpToUse = srcExprs[0].Type.IsUnsignedInt ?
                        OperatorStore.LShiftRightOp : OperatorStore.AShiftRightOp;
                    result = new Expression(srOpToUse,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.BitAnd:
                    result = new Expression(OperatorStore.BitAndOp,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.BitOr:
                    result = new Expression(OperatorStore.BitOrOp,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.BitXor:
                    result = new Expression(OperatorStore.BitXorOp,
                        srcExprs[0], srcExprs[1], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.BitComplement:
                    result = new Expression(OperatorStore.BitComplementOp,
                        srcExprs[0], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.Not:
                    List<Expression> paramList = new List<Expression>();

                    uint destBitSize = destOperand.BitSize;
                    paramList.Add(new Expression(OperatorStore.EqualOp, srcExprs[0],
                        new Constant(0, srcExprs[0].BitSize), path.Config.WORD_BITSIZE));
                    paramList.Add(new Constant(1, destBitSize));
                    paramList.Add(new Constant(0, destBitSize));

                    result = new Expression(OperatorStore.IteOp, paramList, destBitSize);
                    break;

                case Phx.Common.Opcode.Index.Negate:
                    result = new Expression(OperatorStore.NegateOp,
                        srcExprs[0], destOperand.BitSize);
                    break;

                case Phx.Common.Opcode.Index.Acquire:
                    // TODO: We have not implemented complete support for this type of instruction.
                    result = new Expression(OperatorStore.AcquireOp);
                    break;

                case Phx.Common.Opcode.Index.Release:
                    result = srcExprs[0];
                    break;

                case Phx.Common.Opcode.Index.Chi:
                default:
                    throw new NotImplementedException("PHOENIX: " +
                        "Value Instruction Opcode not implemented in symbolic tracer: " +
                        valueInstruction.OpcodeToString());
            }

            if (destOperand.IsPointer)
            {
                /* The operand that is being traced is a pointer. This implies that the result
                 * is at least a pointer Expression (or equivalently, a "dereferencing function"
                 * Expression), perhaps offset by a byte offset Expression. Add this offset to
                 * the pointer Expression. */
                Pair<Expression, Expression> baseAndOffset =
                    ExpressionHelper.GetAugendAndAddend(result, path);
                Expression baseExpr = baseAndOffset.First, offsetExpr = baseAndOffset.Second;

                if (!srcOperands[0].IsTemporary)
                {
                    /* Check if the base Expression is an alias for another Expression, but only
                     * if the base Expression results from tracing back a non-temporary operand.
                     * We do not need to check if an Expression that results from tracing
                     * a temporary operand back is an alias: this check would have already
                     * happened, since the process of tracing back a temporary operand should
                     * eventually find (and trace back) a non-temporary operand.
                     * 
                     * Also, checking if an Expression is an alias for another Expression
                     * is not an idempotent operation, so it should be done sparingly. */
                    baseExpr = path.FindAliasedExpression(baseExpr);
                }

                Phx.Types.Type referentType = ExpressionHelper.GetPointerReferentType(destOperand.Type);
                if (referentType.IsUnmanagedArrayType)
                {
                    Expression modifiedOffsetExpr = new Expression(OperatorStore.MultOp,
                        offsetExpr, new Constant(referentType.BitSize, offsetExpr.BitSize),
                        offsetExpr.BitSize);
                    modifiedOffsetExpr.Type = offsetExpr.Type;
                    offsetExpr = ExpressionHelper.SimplifyExpression(modifiedOffsetExpr, path);
                }

                Expression offsetBitsExpr = ExpressionHelper.ConvertToBits(offsetExpr, path);
                result = ExpressionHelper.AddOffsetToPointer(baseExpr, offsetBitsExpr, path);
            }

            result.Type = destOperand.Type;
            return result;
        }

        /// <summary>
        /// Traces the definition of the operands occurring in the input comparison instruction
        /// backward and returns the source-level symbolic Expression that corresponds
        /// to the instruction.
        /// </summary>
        /// 
        /// <param name="compareInstruction">Comparison instruction.</param>
        /// <param name="varOperand">Current variable operand whose definition
        /// is being traced.</param>
        /// <param name="path">Path along which to trace the definition.</param>
        /// <returns>Symbolic Expression that corresponds to the input instruction.</returns>
        public static Expression
            ExecuteComparisonInstructionBackward(CompareInstruction compareInstruction,
            Operand varOperand, Path path)
        {
            Expression result = null;
            Operator comparisonOperator = null;

            switch (compareInstruction.ConditionCode)
            {
                case ConditionCode.GT:
                    comparisonOperator = OperatorStore.GreaterThanOp;
                    break;
                case ConditionCode.UGT:
                    comparisonOperator = OperatorStore.UGreaterThanOp;
                    break;
                case ConditionCode.FGT:
                    comparisonOperator = OperatorStore.FGreaterThanOp;
                    break;

                case ConditionCode.GE:
                    comparisonOperator = OperatorStore.GreaterThanEqualOp;
                    break;
                case ConditionCode.UGE:
                    comparisonOperator = OperatorStore.UGreaterThanEqualOp;
                    break;
                case ConditionCode.FGE:
                    comparisonOperator = OperatorStore.FGreaterThanEqualOp;
                    break;

                case ConditionCode.LT:
                    comparisonOperator = OperatorStore.LessThanOp;
                    break;
                case ConditionCode.ULT:
                    comparisonOperator = OperatorStore.ULessThanOp;
                    break;
                case ConditionCode.FLT:
                    comparisonOperator = OperatorStore.FLessThanOp;
                    break;

                case ConditionCode.LE:
                    comparisonOperator = OperatorStore.LessThanEqualOp;
                    break;
                case ConditionCode.ULE:
                    comparisonOperator = OperatorStore.ULessThanEqualOp;
                    break;
                case ConditionCode.FLE:
                    comparisonOperator = OperatorStore.FLessThanEqualOp;
                    break;

                case ConditionCode.EQ:
                    comparisonOperator = OperatorStore.EqualOp;
                    break;
                case ConditionCode.NE:
                    comparisonOperator = OperatorStore.NotEqualOp;
                    break;

                default:
                    throw new NotImplementedException("PHOENIX: Compare instruction Opcode " +
                        "not implemented in symbolic tracer: " + compareInstruction.ToString());
            }

            Operand srcOp1 = compareInstruction.SourceOperand1;
            Operand srcOp2 = compareInstruction.SourceOperand2;
            Expression srcExpr1 = TraceOperandBackward(srcOp1, varOperand, path);
            Expression srcExpr2 = TraceOperandBackward(srcOp2, varOperand, path);

            result = new Expression(comparisonOperator, srcExpr1, srcExpr2,
                path.Config.WORD_BITSIZE);
            result.Type = compareInstruction.DestinationOperand.Type;

            BranchInstruction branchInstruction = compareInstruction.Next.AsBranchInstruction;
            if (branchInstruction != null)
            {
                if (path.Blocks[path.Blocks.IndexOf(compareInstruction.BasicBlock) + 1] ==
                    branchInstruction.FalseLabelInstruction.BasicBlock)
                {
                    result = new Expression(OperatorStore.NotOp, result, result.BitSize);
                    result.Type = compareInstruction.DestinationOperand.Type;
                }
            }

            return result;
        }

        /// <summary>
        /// Traces the definition of the operands occurring in the input special instruction
        /// backward and returns the source-level symbolic Expression that corresponds
        /// to the instruction.
        /// </summary>
        /// 
        /// <param name="operand">Operand whose definition needs to be traced.</param>
        /// <param name="specialInstruction">Special instruction.</param>
        /// <param name="varOperand">Current variable operand whose definition
        /// is being traced.</param>
        /// <param name="path">Path along which to trace the definition.</param>
        /// 
        /// <returns>Symbolic Expression that corresponds to the input instruction.</returns>
        public static Expression ExecuteSpecialInstructionBackward(Operand operand,
            Instruction specialInstruction, Operand varOperand, Path path)
        {
            Expression result = null;

            switch (specialInstruction.Opcode.Id)
            {
                case Phx.Common.Opcode.Index.EnterFunction:
                case Phx.Common.Opcode.Index.Start:
                    result = PhoenixHelper.MakeVariableExpression(operand,
                        PhoenixHelper.GetOperandName(operand), path);
                    break;

                case Phx.Common.Opcode.Index.Phi:
                    BasicBlock nearestBlock = path.SourceBlock;
                    foreach (Operand sourceOperand in specialInstruction.SourceOperands)
                    {
                        if (sourceOperand.DefinitionInstruction != null)
                        {
                            BasicBlock defBlock = sourceOperand.DefinitionInstruction.BasicBlock;
                            if (path.Blocks.Contains(defBlock) &&
                                path.IsLocatedAfter(defBlock, nearestBlock))
                            {
                                nearestBlock = defBlock;

                                Operand actualVarOperand =
                                    operand.IsTemporary ? varOperand : operand;
                                actualVarOperand =
                                    sourceOperand.IsTemporary ? actualVarOperand : sourceOperand;

                                result = TraceOperandBackward(sourceOperand,
                                    actualVarOperand, path);
                            }
                        }
                    }
                    break;

                case Phx.Common.Opcode.Index.Label:
                    break;

                default:
                    throw new NotImplementedException("ExecuteSpecialInstructionBackward: " +
                        "Special Instruction Opcode not implemented in symbolic tracer: " +
                        specialInstruction.OpcodeToString());
            }

            return result;
        }
    }
}
