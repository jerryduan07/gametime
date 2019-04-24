/*************************************************************************************************
 * PhoenixHelper                                                                                 *
 * ============================================================================================= *
 * This file contains the class that defines various helper functions                            *
 * to interact with the Phoenix infrastructure.                                                  *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Phx;
using Phx.Graphs;
using Phx.IR;
using Phx.Symbols;
using Phx.Types;

using Utilities;


namespace GameTime
{
    /// <summary>
    /// This class defines various helper functions to interact with the Phoenix infrastructure.
    /// </summary>
    public class PhoenixHelper
    {
        #region Variable Methods

        /// <summary>
        /// Returns the operand name at the source level.
        /// </summary>
        /// 
        /// <param name="operand">Operand whose name needs to be returned.</param>
        /// <returns>Operand name at the source level.</returns>
        public static string GetOperandName(Phx.IR.Operand operand)
        {
            Trace.Assert(operand.Symbol != null,
                "PHOENIX: The operand should have an associated symbol.");

            string operandString = operand.Symbol.NameString;
            if (operandString.StartsWith("_"))
            {
                operandString = operandString.Substring(1);
            }
            else if (operandString.StartsWith("&_"))
            {
                operandString = operandString.Substring(2);
            }
            else if (operandString.StartsWith("&"))
            {
                operandString = operandString.Substring(1);
            }
            
            return Phx.Utility.Undecorate(operandString, false);
        }

        /// <summary>
        /// Returns a variable Expression with the input value, which represents the variable
        /// corresponding to the input operand.
        /// </summary>
        /// 
        /// <param name="operand">Operand for the variable Expression.</param>
        /// <param name="value">Value of the variable Expression.</param>
        /// <param name="path">Path that contains the operand.</param>
        /// <returns>Variable Expression with value <paramref name="value"/>,
        /// which represents the operand <paramref name="operand"/>.</returns>
        public static Expression MakeVariableExpression(Phx.IR.Operand operand,
            string value, Path path)
        {
            uint nativeWordBitSize = path.Config.WORD_BITSIZE;
            Expression result = null;

            /* Find the type of this variable, if the operand is not an address, or
             * the type of the variable the operand refers to, otherwise. */
            Phx.Types.Type variableType = operand.IsAddress ?
                operand.Type.AsPointerType.ReferentType : operand.Type;
            /* Add an identifier to the names of aggregate objects. */
            value = variableType.IsAggregateType ? path.Config.IDENT_AGGREGATE + value : value;

            result = ExpressionHelper.IsPointerExpressionType(variableType) ?
                new Expression(OperatorStore.ArrayVariableOp, value, nativeWordBitSize) :
                new Expression(OperatorStore.VariableOp, value,
                    operand.IsAggregate ? nativeWordBitSize : operand.BitSize);
            result.Type = variableType;

            /* Replace a pointer Expression with the corresponding "dereferencing function". */
            result = ExpressionHelper.IsPointerExpression(result) ?
                ExpressionHelper.MakeDereferencingFunction(result, path) : result;

            /* If the operand uses the address of an existing variable, determine if there
             * is already a temporary pointer that points to this "address-taken" variable.
             * If not, make a new temporary pointer that points to this variable. */
            if (operand.IsAddress)
            {
                if (path.AddressTaken.ContainsKey(result))
                {
                    result = path.AddressTaken[result].Clone();
                }
                else
                {
                    Phx.Types.Type referentType =
                        ExpressionHelper.GetPointerReferentType(operand.Type);

                    /* Determine the basic block that contains the operand and the associated
                     * information. */
                    BasicBlock operandBasicBlock = operand.Instruction.BasicBlock;
                    BasicBlockAddendum operandBasicBlockAddendum =
                        operandBasicBlock.FindExtensionObject(typeof(BasicBlockAddendum))
                        as BasicBlockAddendum;

                    /* Generate a new temporary pointer and log the relationship between
                     * the temporary pointer and the "address-taken" variable. */
                    Expression newTempPtr = path.GetNewTemporaryPointer(operand.Type);
                    path.AddressTaken[result] = newTempPtr;

                    /* Generate and log the assignment between the dereferenced temporary pointer
                     * and the "address-taken" variable. This way, the generated SMT queries will
                     * never have to use the Address-Of operator. */
                    Expression dereferencedNewTempPtr =
                        ExpressionHelper.DereferencePointer(newTempPtr, operand.Type,
                            false, path);
                    List<Expression> assignExprs =
                        path.GenerateAndLogAssignment(dereferencedNewTempPtr, result,
                            operandBasicBlock);

                    /* Add the conditional Expressions that correspond to this new assignment. */
                    foreach (Expression assignExpr in assignExprs)
                    {
                        path.AddCondition(assignExpr, operandBasicBlock.Id);
                    }

                    result = newTempPtr;
                }
            }

            /* If the variable is an "address-taken" variable, which means that its address
             * has been taken before, then we replace the variable with a dereference of
             * the temporary pointer that refers to the variable. */
            if (path.AddressTaken.ContainsKey(result))
            {
                result = ExpressionHelper.DereferencePointer(path.AddressTaken[result],
                    operand.Type, false, path);
            }

            path.AddVariable(result);
            return result;
        }

        /// <summary>
        /// Creates a variable that corresponds to the input call instruction.
        /// </summary>
        /// 
        /// <param name="operand">Operand whose definition needs to be traced.</param>
        /// <param name="callInstruction">Call instruction.</param>
        /// <param name="varOperand">Current variable operand whose definition
        /// is being traced, which may be different from "operand".</param>
        /// <param name="path">Path along which to trace the definition.</param>
        /// 
        /// <returns>Variable Expression corresponding to the call instruction provided.</returns>
        public static Expression MakeCallVariableExpression(Operand operand,
            Instruction callInstruction, Operand varOperand, Path path)
        {
            string funcName = PhoenixHelper.GetOperandName(callInstruction.SourceOperand1);
            string lineNumber = callInstruction.GetLineNumber().ToString();

            return PhoenixHelper.MakeVariableExpression(operand,
                (path.Config.IDENT_EFC + funcName + "@" + lineNumber), path);
        }

        #endregion

        #region Aggregate Methods

        /// <summary>
        /// Returns the "clean" string (without the extra characters added by Phoenix)
        /// that represents the name of the aggregate.
        /// </summary>
        /// 
        /// <param name="aggregateType">Aggregate type.</param>
        /// <returns>"Clean" string that represents the name of the aggregate type.</returns>
        public static string GetAggregateName(Phx.Types.AggregateType aggregateType)
        {
            string result = aggregateType.TypeSymbol.ToString();
            if (result.EndsWith("@@")) { result = result.Substring(0, result.Length - 2); }
            if (result.StartsWith("U")) { result = result.Substring(1); }
            return result;
        }

        /// <summary>
        /// Returns a list of AggregateField objects, each of which represents
        /// the aggregate fields that the input aggregate access overlaps.
        /// </summary>
        ///
        /// <param name="aggregateExpr">Expression that represents the aggregate
        /// that is being accessed.</param>
        /// <param name="offset">Offset of the access from the start of the aggregate,
        /// in bits.</param>
        /// <param name="accessType">Type of the aggregate access.</param>
        /// <param name="getArrayElements">True if, and only if, the elements of
        /// a fixed-size array should be treated as separate fields of the aggregate.</param>
        /// <param name="path">Path that contains the aggregate.</param>
        /// <returns>List of AggregateField objects, each of which represents
        /// the aggregate fields that the input aggregate access overlaps.</returns>
        public static List<AggregateField> GetAggregateFields(Expression aggregateExpr,
            int offset, Phx.Types.Type accessType, bool getArrayElements, Path path)
        {
            return PhoenixHelper.GetAggregateFieldsHelper(aggregateExpr.Type.AsAggregateType,
                offset, accessType, (int)accessType.BitSize, getArrayElements,
                aggregateExpr.Clone(), path);
        }

        /// <summary>
        /// Recursive workhorse method for the method <see cref="GetAggregateFields"/>.
        /// </summary>
        ///
        /// <param name="aggregateType">Type of the aggregate that is being accessed.</param>
        /// <param name="offset">Offset of the access from the start of the aggregate,
        /// in bits.</param>
        /// <param name="accessType">Type of the aggregate access.</param>
        /// <param name="accessBitSize">Bit-size of the aggregate access.</param>
        /// <param name="getArrayElements">True if, and only if, the elements of
        /// a fixed-size array should be treated as separate fields of the aggregate.</param>
        /// <param name="accessSoFar">Expression that represents the access so far.</param>
        /// <param name="path">Path that contains the aggregate.</param>
        /// <returns>List of AggregateField objects, each of which represents
        /// the aggregate fields that the input aggregate access overlaps.</returns>
        private static List<AggregateField> GetAggregateFieldsHelper(AggregateType aggregateType,
            int offset, Phx.Types.Type accessType, int accessBitSize, bool getArrayElements,
            Expression accessSoFar, Path path)
        {
            Utilities.Configuration config = path.Config;
            List<AggregateField> result = new List<AggregateField>();

            int completedOffset = 0;
            FieldSymbol currentFieldSymbol = aggregateType.FieldSymbolList;
            while (currentFieldSymbol != null)
            {
                /* Obtain the necessary information from the current field symbol. */
                string fieldName = currentFieldSymbol.ToString();
                int fieldStartOffset = currentFieldSymbol.BitOffset;
                Phx.Types.Type fieldType = currentFieldSymbol.Type;
                int fieldEndOffset = (fieldStartOffset + (int)fieldType.BitSize) - 1;

                /* Move to the next field symbol for the subsequent iteration. */
                currentFieldSymbol = currentFieldSymbol.NextFieldSymbol;
                if (fieldStartOffset < completedOffset)
                {
                    /* Skip this field if it starts at an offset in the part of
                     * the aggregate that has already been examined. */
                    continue;
                }
                else
                {
                    /* Move the completed offset pointer to the end of the field currently
                     * being examined. This indicates that, after this iteration
                     * is complete, the part of the aggregate prior to the pointer
                     * has already been examined. */
                    completedOffset = fieldEndOffset;
                }

                if (((offset <= fieldStartOffset) &&
                        (fieldStartOffset <= (offset + accessBitSize) - 1)) ||
                    ((offset > fieldStartOffset) && (offset <= fieldEndOffset)))
                {
                    /* Attach a unique identifier to each field name. */
                    string newFieldName = String.Format("{0}{1}{2}{3}",
                        config.IDENT_FIELD, fieldName,
                        config.IDENT_AGGREGATE, GetAggregateName(aggregateType));

                    /* Make an array variable Expression for the field, since we represent
                     * aggregate accesses as accesses into an array. */
                    Expression newArrayVar =
                        new Expression(OperatorStore.ArrayVariableOp, newFieldName);
                    newArrayVar.Type = Phx.Types.PointerType.New(aggregateType.TypeTable,
                        Phx.Types.PointerTypeKind.UnmanagedPointer, path.Config.WORD_BITSIZE,
                        fieldType, fieldType.TypeSymbol);
                    path.AddVariable(newArrayVar);

                    /* Convert the array variable Expression into the equivalent
                     * pointer Expression, represented as a dereferencing function. */
                    newArrayVar = ExpressionHelper.MakeDereferencingFunction(newArrayVar, path);

                    /* Apply the dereferencing function on the Expression generated so far. */
                    List<Expression> argExprs = new List<Expression>();
                    argExprs.Add(accessSoFar);
                    argExprs.Add(new Constant(0, config.WORD_BITSIZE));

                    Expression fieldAccessExpr =
                        ExpressionHelper.ApplyFunction(newArrayVar, argExprs, path);
                    fieldAccessExpr = ExpressionHelper.LookupAndReplaceOffset(fieldAccessExpr,
                        fieldType, false, path);

                    if (fieldType.IsAggregateType)
                    {
                        /* Recurse into the field, if the field is itself an aggregate. */
                        AggregateType innerAggregateType = fieldType.AsAggregateType;
                        List<AggregateField> innerAggregateFields =
                            GetAggregateFieldsHelper(innerAggregateType,
                                Math.Max((offset - fieldStartOffset), 0),
                                accessType,
                                (accessBitSize - Math.Max(fieldStartOffset - offset, 0)),
                                getArrayElements, fieldAccessExpr, path);
                        foreach (AggregateField innerAggregateField in innerAggregateFields)
                        {
                            /* Include the offset of the field inside
                             * the enclosing aggregate type. */
                            innerAggregateField.StartOffset += fieldStartOffset;
                            result.Add(innerAggregateField);
                        }
                    }
                    else if (fieldType.IsUnmanagedArrayType &&
                        !ExpressionHelper.AreSameTypes(fieldType, accessType) &&
                        getArrayElements)
                    {
                        List<Pair<Expression, int>> arrayElements =
                            ExpressionHelper.GetArrayElementsInRange(fieldAccessExpr,
                                Math.Max((offset - fieldStartOffset), 0),
                                (accessBitSize - Math.Max(fieldStartOffset - offset, 0)),
                                path);
                        foreach (Pair<Expression, int> arrayElementAndOffset in arrayElements)
                        {
                            Expression arrayElementExpr = arrayElementAndOffset.First;
                            int arrayElementOffset = arrayElementAndOffset.Second;

                            result.Add(new AggregateField(aggregateType,
                                arrayElementExpr, (fieldStartOffset + arrayElementOffset),
                                arrayElementExpr.BitSize));
                        }
                    }
                    else
                    {
                        result.Add(new AggregateField(aggregateType,
                            fieldAccessExpr, fieldStartOffset, fieldType.BitSize));
                    }
                }
            }

            Trace.Assert(result.Count > 0, "PHOENIX: Field(s) not found.");
            return result;
        }

        #endregion
    }
}
