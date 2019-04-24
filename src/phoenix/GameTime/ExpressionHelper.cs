/*************************************************************************************************
 * ExpressionHelper                                                                              *
 * ============================================================================================= *
 * This file contains the class that provides various helper functions to extract                *
 * information from, and modify, an Expression object.                                           *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Phx.Types;

using Utilities;


namespace GameTime
{
    /// <summary>
    /// This class provides various helper functions to extract information from, and modify,
    /// an <see cref="Expression"/> object.
    /// </summary>
    class ExpressionHelper
    {
        #region Array Expression Methods

        /// <summary>
        /// Returns the array variable Expression present in the input Expression.
        /// </summary>
        /// 
        /// <param name="expr">Expression to extract the array variable Expression from.</param>
        /// 
        /// <returns>Array variable Expression in the input Expression.</returns>
        /// <remarks>Precondition: The input Expression is either an array access
        /// Expression or an array variable Expression.</remarks>
        public static Expression GetArrayVariable(Expression expr)
        {
            Expression result = null;

            switch (expr.Op.Type)
            {
                /* Base case: Expression is already an array variable Expression. */
                case OperatorType.ArrayVariable:
                    result = expr;
                    break;

                /* Recursive case: Recurse on the first parameter of an array access Expression. */
                case OperatorType.Array:
                    result = GetArrayVariable(expr.GetParameter(0));
                    break;
            }

            Trace.Assert(result != null, "PHOENIX: Result should not be null.");
            return result;
        }

        /// <summary>
        /// Returns the dimensions of the array that the input Expression refers to.
        /// </summary>
        /// 
        /// <param name="expr">Expression that refers to an array.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Dimensions of the array that the input Expression refers to.</returns>
        /// <remarks>Precondition: The input Expression refers to an array.</remarks>
        public static List<uint> GetArrayDimensions(Expression expr, Path path)
        {
            if (path.ArrayDimensions.ContainsKey(expr))
            {
                return path.ArrayDimensions[expr];
            }

            if (expr.Op.Type == OperatorType.Variable ||
                expr.Op.Type == OperatorType.ArrayVariable)
            {
                Expression originalExpr = BasicBlockAddendum.GetOriginalVariable(expr);
                if (path.ArrayDimensions.ContainsKey(originalExpr))
                {
                    List<uint> originalDimensions = path.ArrayDimensions[originalExpr];
                    path.ArrayDimensions[expr] = originalDimensions;
                    return originalDimensions;
                }
            }

            Phx.Types.Type pointerType = expr.Type;
            Trace.Assert(IsPointerExpression(expr),
                "PHOENIX: Cannot find the dimension of an Expression that does " +
                "not refer to an array.");

            uint nativeWordBitSize = path.Config.WORD_BITSIZE;
            List<uint> dimensions = new List<uint>();

            Phx.Types.Type referentType = pointerType;
            while (referentType != null)
            {
                if (referentType.IsUnmanagedArrayType)
                {
                    dimensions.Add(nativeWordBitSize);
                    referentType = referentType.AsUnmanagedArrayType.ElementType;
                }
                else if (referentType.IsAggregateType)
                {
                    /* If the Expression refers to an array of aggregates, we do not add the size
                     * of the aggregate to the dimensions of the array. This is because we will
                     * never actually use the aggregate itself, but only the fields within it.
                     * This makes it easier to, for example, use a struct to index into the array
                     * for a field. */
                    dimensions.Add(nativeWordBitSize);
                    referentType = null;
                }
                else if (referentType.IsPointerType)
                {
                    dimensions.Add(referentType.BitSize);
                    referentType = referentType.AsPointerType.ReferentType;
                }
                else
                {
                    dimensions.Add(referentType.BitSize);
                    referentType = null;
                }
            }

            /* Memoize the dimensions of the array that this Expression refers to. */
            path.ArrayDimensions[expr] = dimensions;
            return dimensions;
        }

        /// <summary>
        /// Returns a list of the index Expressions that are needed to access an element
        /// of the array that the input pointer Expression represents. The input list of
        /// Expressions represents the (possibly incomplete) list of index Expressions
        /// currently available, which may need to be corrected.
        /// </summary>
        /// 
        /// <param name="pointerExpr">Pointer Expression that represents an array.</param>
        /// <param name="indexExprs">List of the index Expressions currently available.</param>
        /// <param name="path">Path that contains the Pointer Expression.</param>
        /// <returns>List of index Expressions, as described above.</returns>
        public static List<Expression> GetArrayAccessIndices(Expression pointerExpr,
            List<Expression> indexExprs, Path path)
        {
            Phx.Types.Type arrayType = pointerExpr.Type;
            Trace.Assert(arrayType.IsUnmanagedArrayType,
                "PHOENIX: Subscription is done on either an object that is not an array " +
                "or an array whose size is not fixed.");

            uint nativeWordBitSize = path.Config.WORD_BITSIZE;
            List<Expression> result = new List<Expression>();

            int currentIndexExprPos = 0;
            Expression currentIndexExpr = null;
            Phx.Types.Type referentType = arrayType;
            while (referentType.IsUnmanagedArrayType)
            {
                /* If there are no more indices, use the remainder from the previous iteration. */
                currentIndexExpr = (currentIndexExprPos < indexExprs.Count) ?
                    indexExprs[currentIndexExprPos++] : currentIndexExpr;
                uint innerArrayByteSize = referentType.AsUnmanagedArrayType.ElementType.ByteSize;
                Expression innerArrayByteSizeExpr =
                    new Constant(innerArrayByteSize, nativeWordBitSize);

                Expression newIndexExpr = new Expression(OperatorStore.SDivOp,
                    currentIndexExpr, innerArrayByteSizeExpr, currentIndexExpr.BitSize);
                newIndexExpr.Type = currentIndexExpr.Type;
                newIndexExpr = SimplifyExpression(newIndexExpr, path);
                result.Add(newIndexExpr);

                referentType = referentType.AsUnmanagedArrayType.ElementType;
                if (currentIndexExprPos == indexExprs.Count && referentType.IsUnmanagedArrayType)
                {
                    /* There are no more indices. Use the remainder as
                     * an index for the next iteration. */
                    Expression remainderExpr = new Expression(OperatorStore.RemOp,
                        currentIndexExpr, innerArrayByteSizeExpr, currentIndexExpr.BitSize);
                    remainderExpr.Type = currentIndexExpr.Type;
                    currentIndexExpr = SimplifyExpression(remainderExpr, path);
                }
            }

            return result;
        }

        /// <summary>
        /// Constructs an Expression that refers to a specific element within an array.
        /// Dereferencing this Expression should produce an Expression that accesses the element.
        /// </summary>
        /// 
        /// <param name="pointerExpr">Pointer Expression that represents an array.</param>
        /// <param name="indexExprs">List of index Expressions that access a specific
        /// element in the array that the input pointer Expression represents.</param>
        /// <param name="path">Path that contains the input pointer Expression.</param>
        /// <returns>Expression that refers to a specific element within the array.</returns>
        public static Expression GetArrayElementReference(Expression pointerExpr,
            List<Expression> indexExprs, Path path)
        {
            Expression result = pointerExpr;
            for(int currentIndexPos = 0; currentIndexPos < indexExprs.Count; currentIndexPos++)
            {
                Phx.Types.Type referentType = GetPointerReferentType(result);

                /* Offset the pointer Expression, which should be a pointer to
                 * the array that is being accessed. */
                Expression indexExpr = indexExprs[currentIndexPos];
                Expression indexBitsExpr = new Expression(OperatorStore.MultOp,
                    indexExpr, new Constant(referentType.BitSize, indexExpr.BitSize),
                    indexExpr.BitSize);
                indexBitsExpr.Type = indexExpr.Type;
                indexBitsExpr = SimplifyExpression(indexBitsExpr, path);
                result = AddOffsetToPointer(result, indexBitsExpr, path);

                /* Dereference the pointer Expression, unless the pointer Expression refers to
                 * the specific element in the array that the result should refer to. */
                if (currentIndexPos < indexExprs.Count - 1)
                {
                    result =
                        DereferencePointer(result, GetPointerReferentType(result), true, path);
                    /* Check if the dereferenced pointer Expression aliases another Expression. */
                    result = path.FindAliasedExpression(result);
                }
            }

            return result;
        }

        /// <summary>
        /// Determines the elements of a fixed-size array that are present in a specified
        /// range of bits. This method takes in the offset (in bits) of the start of
        /// this range from the start of the array, along with the bit-size of this range.
        /// It then returns a list of Expressions, each of which represents an element of
        /// the provided fixed-size array that is present in the specified range, and each of
        /// which is paired with the offset (in bits) of that element from the start of the array.
        /// </summary>
        /// 
        /// <param name="arrayExpr">Expression that represents a fixed-size array.</param>
        /// <param name="startOffset">Offset (in bits) of the start of a range of bits.</param>
        /// <param name="rangeBitSize">Bit-size of the range of bits.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>List of pairs, as described above.</returns>
        public static List<Pair<Expression, int>> GetArrayElementsInRange(Expression arrayExpr,
            int startOffset, int rangeBitSize, Path path)
        {
            Phx.Types.Type arrayExprType = arrayExpr.Type;
            Trace.Assert(arrayExprType.IsUnmanagedArrayType,
                "PHOENIX: Only the elements of a fixed-size array in " +
                "a specified range can be obtained.");
            Phx.Types.Type referentType = GetPointerReferentType(arrayExprType);

            List<Pair<Expression, int>> result = new List<Pair<Expression, int>>();
            if ((rangeBitSize <= 0) || (startOffset >= arrayExprType.BitSize))
            {
                /* Base case: The range of bits provided is zero, or the offset of
                 * the start of the range has exceeded the bit-size of the array. */
                return result;
            }

            uint nativeWordBitSize = path.Config.WORD_BITSIZE;

            /* Move the start offset to the nearest (and lower) start of an array element. */
            int originalStartOffset = startOffset;
            startOffset -= (int)(startOffset % referentType.BitSize);
            Expression startOffsetExpr = new Constant(startOffset, nativeWordBitSize);

            /* Construct an Expression that refers to the array element, and
             * then dereference the Expression to obtain the required array
             * access Expression. */
            Expression arrayElementReferenceExpr =
                ExpressionHelper.AddOffsetToPointer(arrayExpr, startOffsetExpr, path);
            Expression arrayAccessExpr =
                DereferencePointer(arrayElementReferenceExpr, referentType, false, path);

            if (arrayAccessExpr.Type.IsUnmanagedArrayType)
            {
                /* Recurse into the array element if the element is an array itself. */
                List<Pair<Expression, int>> innerArrayElementsAndOffsets =
                    GetArrayElementsInRange(arrayAccessExpr,
                        (originalStartOffset - startOffset), rangeBitSize, path);
                foreach (Pair<Expression, int> elementAndOffset in innerArrayElementsAndOffsets)
                {
                    Expression innerArrayElement = elementAndOffset.First;
                    int offset = elementAndOffset.Second;
                    offset += startOffset;
                    result.Add(new Pair<Expression,int>(innerArrayElement, offset));
                }
            }
            else
            {
                result.Add(new Pair<Expression, int>(arrayAccessExpr, startOffset));
            }

            /* Subtract the bits in the range that have been accounted for. */
            int difference = (int)((startOffset - originalStartOffset) + referentType.BitSize);
            rangeBitSize -= difference;

            /* Recursively call the function on the rest of the range. */
            result.AddRange(GetArrayElementsInRange(arrayExpr,
                (int)(startOffset + referentType.BitSize), rangeBitSize, path));
            return result;
        }

        /// <summary>
        /// Returns the list of array accesses present in the input Expression, if any.
        /// Each element of the list is a pair that maps an array variable Expression to
        /// a list of numbers of the temporary index variables.
        /// </summary>
        /// 
        /// <param name="expr">Expression that may contain array accesses.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>List of array accesses present in the input Expression as described
        /// above, if any.</returns>
        public static List<Pair<Expression, List<uint>>> FindArrayAccesses(Expression expr,
            Path path)
        {
            Utilities.Configuration config = path.Config;
            OperatorType exprOpType = expr.Op.Type;
            List<Pair<Expression, List<uint>>> result = new List<Pair<Expression, List<uint>>>();

            /* Base case: Zero-arity operators. */
            if (expr.Op.Arity == OperatorArity.Nil && exprOpType != OperatorType.ArrayVariable)
            {
                return result;
            }

            switch (exprOpType)
            {
                /* Base case: Expression is an array variable Expression. */
                case OperatorType.ArrayVariable:
                    Expression origVariable = BasicBlockAddendum.GetOriginalVariable(expr);
                    result.Add(new Pair<Expression, List<uint>>(origVariable, new List<uint>()));
                    break;

                /* Recursive case: Recurse on the first parameter of an array access Expression. */
                case OperatorType.Array:
                    List<Pair<Expression, List<uint>>> baseArrayAccesses =
                        FindArrayAccesses(expr.GetParameter(0), path);
                    Pair<Expression, List<uint>> currentAccess = baseArrayAccesses[0];

                    Expression arrayVariableExpr = currentAccess.First;
                    List<uint> baseTemporaryIndices = currentAccess.Second;

                    /* Find the number of the current temporary index. */
                    string tempIndex = expr.GetParameter(1).Value;
                    uint tempIndexNumber =
                        Convert.ToUInt32(tempIndex.Substring(config.IDENT_TEMPINDEX.Length));

                    /* Add this number to the array access of the base Expression. */
                    baseTemporaryIndices.Add(tempIndexNumber);
                    Pair<Expression, List<uint>> newPair = new Pair<Expression, List<uint>>();
                    newPair.First = arrayVariableExpr;
                    newPair.Second = baseTemporaryIndices;
                    result.Add(newPair);
                    break;

                /* Recursive case: Recurse on the parameters of the Expression. */
                default:
                    foreach (Expression param in expr.ParameterList)
                    {
                        List<Pair<Expression, List<uint>>> paramArrayAccesses =
                            FindArrayAccesses(param, path);
                        result.AddRange(paramArrayAccesses);
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// Converts the array access Expressions in the input Expression into
        /// the appropriate array selection Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Expression that may contain array access Expressions.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>New Expression where the array access Expressions in the input Expression
        /// are converted into the appropriate array selection Expressions.</returns>
        public static Expression ConvertArrayAccesses(Expression expr, Path path)
        {
            /* Base case: Zero-arity operators. */
            if (expr.Op.Arity == OperatorArity.Nil) { return expr; }

            Expression result = null;
            OperatorType exprOpType = expr.Op.Type;
            switch (exprOpType)
            {
                /* Recursive case: Convert the parameters of an array access Expression. */
                case OperatorType.Array:
                    Expression baseExpr = expr.GetParameter(0);
                    Expression convertedBaseExpr = ConvertArrayAccesses(baseExpr, path);

                    Expression indexExpr = expr.GetParameter(1);
                    Expression convertedIndexExpr = ConvertArrayAccesses(indexExpr, path);

                    if (!path.ProjectConfig.MODEL_AS_NESTED_ARRAYS)
                    {
                        if (convertedBaseExpr.Op.Type == OperatorType.Select)
                        {
                            /* The base of this array Expression is itself an array Expression.
                             * Concatenate the index obtained from converting the base
                             * and the converted index of this Expression. */
                            Expression convertedBaseIndexExpr = convertedBaseExpr.GetParameter(1);
                            convertedIndexExpr = new Expression(OperatorStore.ConcatenateOp,
                                convertedBaseIndexExpr, convertedIndexExpr,
                                convertedBaseExpr.BitSize + convertedIndexExpr.BitSize);
                            convertedBaseExpr = convertedBaseExpr.GetParameter(0);
                        }
                    }

                    result = new Expression(OperatorStore.SelectOp,
                        convertedBaseExpr, convertedIndexExpr);
                    break;

                /* Recursive case: Convert the parameters of the Expression. */
                default:
                    List<Expression> newParamList = new List<Expression>();
                    foreach (Expression paramExpr in expr.ParameterList)
                    {
                        Expression convertedParamExpr = ConvertArrayAccesses(paramExpr, path);
                        newParamList.Add(convertedParamExpr);
                    }
                    result = new Expression(expr.Op, newParamList, expr.BitSize);
                    break;
            }

            Trace.Assert(result != null, "PHOENIX: Result should not be null.");
            return result;
        }

        /// <summary>
        /// Creates an Expression that stores the input element in a particular
        /// position in an array, as specified by the input Expression.
        /// </summary>
        /// 
        /// <param name="expr">Expression that specifies a particular position in an array.</param>
        /// <param name="toStore">Element to store.</param>
        /// <param name="path">Path that contains the input Expressions.</param>
        /// <returns>Expression that stores the input element in a particular position in an array,
        /// as specified by the input Expression.</returns>
        public static Expression CreateArrayStore(Expression expr, Expression toStore, Path path)
        {
            Expression result = null;

            if (path.ProjectConfig.MODEL_AS_NESTED_ARRAYS)
            {
                switch (expr.Op.Type)
                {
                    case OperatorType.ArrayVariable:
                        result = toStore;
                        break;

                    case OperatorType.Array:
                        Expression baseExpr = expr.GetParameter(0);
                        Expression indexExpr = expr.GetParameter(1);

                        List<Expression> paramList = new List<Expression>();
                        paramList.Add(baseExpr);
                        paramList.Add(indexExpr);
                        paramList.Add(toStore);

                        result = CreateArrayStore(baseExpr,
                            new Expression(OperatorStore.StoreOp, paramList, baseExpr.BitSize),
                            path);
                        break;
                }
            }
            else
            {
                switch (expr.Op.Type)
                {
                    case OperatorType.ArrayVariable:
                        result = expr;
                        break;

                    case OperatorType.Array:
                        Expression baseExpr = expr.GetParameter(0);
                        Expression indexExpr = expr.GetParameter(1);

                        baseExpr = CreateArrayStore(baseExpr, toStore, path);
                        if (baseExpr.Op.Type == OperatorType.Store)
                        {
                            /* The base of this array Expression is itself an array Expression.
                             * Concatenate the index obtained from converting the base
                             * and the index of this array Expression. */
                            Expression baseIndexExpr = baseExpr.GetParameter(1);
                            indexExpr = new Expression(OperatorStore.ConcatenateOp,
                                baseIndexExpr, indexExpr,
                                baseIndexExpr.BitSize + indexExpr.BitSize);

                            // TODO: Do the indices have types?
                            baseExpr = baseExpr.GetParameter(0);
                        }

                        List<Expression> paramList = new List<Expression>();
                        paramList.Add(baseExpr);
                        paramList.Add(indexExpr);
                        paramList.Add(toStore);

                        result = new Expression(OperatorStore.StoreOp,
                            paramList, baseExpr.BitSize);
                        break;
                }
            }

            Trace.Assert(result != null, "PHOENIX: Result should not be null.");
            return result;
        }

        /// <summary>
        /// Returns the size (in number of array elements) of the array that the input Expression
        /// refers to, if the array has a fixed size: the indices of array accesses must be less
        /// than this size. If the array does not have a fixed size, a value of -1 is returned.
        /// </summary>
        /// 
        /// <param name="expr">Expression that refers to an array.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Size (in number of array elements) of the array that the input Expression
        /// refers to, if the array has a fixed size; -1, otherwise.</returns>
        public static int GetArrayIndexUpperBound(Expression expr, Path path)
        {
            Trace.Assert(IsPointerExpression(expr),
                "PHOENIX: The input Expression must be a pointer Expression.");
            return GetArrayIndexUpperBound(expr.Type, path);
        }

        /// <summary>
        /// Returns the size (in number of array elements) of the array whose type is provided,
        /// if the array has a fixed size: the indices of array accesses must be less than
        /// this size. If the array does not have a fixed size, a value of -1 is returned.
        /// </summary>
        /// 
        /// <param name="exprType">Type of an Expression that refers to an array.</param>
        /// <param name="path">Path that contains the Expression.</param>
        /// <returns>Size (in number of array elements) of the array that the input Expression
        /// refers to, if the array has a fixed size; -1, otherwise.</returns>
        public static int GetArrayIndexUpperBound(Phx.Types.Type exprType, Path path)
        {
            Trace.Assert(IsPointerExpressionType(exprType),
                "PHOENIX: The input Expression type must be a pointer type.");

            int result = -1;
            if (exprType.IsUnmanagedArrayType)
            {
                UnmanagedArrayType arrayType = exprType.AsUnmanagedArrayType;
                result = (int)(arrayType.BitSize / arrayType.ElementType.BitSize);
            }
            return result;
        }

        /// <summary>
        /// Returns a list of the sizes (in number of array elements) along each dimension of
        /// the array whose type is provided: the index of an array access along a dimension
        /// must be less than these sizes. If the array does not have a fixed size along
        /// a dimension, a value of -1 is added to the list.
        /// </summary>
        /// 
        /// <param name="exprType">Type of an Expression that refers to an array.</param>
        /// <param name="path">Path that contains the Expression.</param>
        /// <returns>List of integers as described.</returns>
        public static List<int> GetArrayIndicesUpperBounds(Phx.Types.Type exprType, Path path)
        {
            Trace.Assert(IsPointerExpressionType(exprType),
                "PHOENIX: The input Expression type must be a pointer type.");

            List<int> result = new List<int>();
            Phx.Types.Type referentType = exprType;
            while (referentType != null)
            {
                if (IsPointerExpressionType(referentType))
                {
                    result.Add(GetArrayIndexUpperBound(referentType, path));
                    referentType = GetPointerReferentType(referentType);
                }
                else
                {
                    referentType = null;
                }
            }

            return result;
        }

        /// <summary>
        /// Generates a condition that imposes a lower and upper bound on the input temporary
        /// index variable Expression, if the array that is being accessed has a fixed size.
        /// If no extra condition is required, null is returned.
        /// </summary>
        /// 
        /// <param name="indexExpr">Original index of an array access Expression.</param>
        /// <param name="temporaryIndexExpr">Temporary index variable Expression used to replace
        /// the original index of an array access Expression.</param>
        /// <param name="numElements">Number of elements in the array that is
        /// being accessed. If the array does not have a fixed size, this argument
        /// should be -1.</param>
        /// <param name="path">Path that contains the input Expressions.</param>
        /// <returns>Condition that imposes a lower and upper bound on the input temporary index
        /// variable Expression, if the array that is being accessed has a fixed size.
        /// If no extra condition is required, null is returned.</returns>
        private static Expression AddBoundsOnIndex(Expression indexExpr,
            Expression temporaryIndexExpr, int numElements, Path path)
        {
            if (numElements == -1) { return null; }

            if (IsConstantExpression(indexExpr))
            {
                /* The index Expression is a constant Expression. Check if
                 * the constant is within the bounds of the array. */
                int indexConstant = ConvertToConstant(indexExpr);
                if (!(indexConstant >= 0 && indexConstant < numElements))
                {
                    /* The constant does not lie within the bounds of the array.
                     * Add a "false" condition to "prevent" this access. */
                    return new Expression(OperatorStore.FalseOp, path.Config.WORD_BITSIZE);
                }
                return null;
            }

            /* The index Expression is a non-constant Expression. Add constraints to ensure that
             * the Expression evaluates to an index that is within the bounds of the array. */
            Expression zeroExpr = new Constant(0, temporaryIndexExpr.BitSize);
            Expression lowerBoundCondition = new Expression(OperatorStore.LessThanEqualOp,
                zeroExpr, temporaryIndexExpr, temporaryIndexExpr.BitSize);
            Expression upperBoundExpr = new Constant(numElements, temporaryIndexExpr.BitSize);
            Expression upperBoundCondition = new Expression(OperatorStore.LessThanOp,
                    temporaryIndexExpr, upperBoundExpr, temporaryIndexExpr.BitSize);
            return new Expression(OperatorStore.AndOp,
                lowerBoundCondition, upperBoundCondition, path.Config.WORD_BITSIZE);
        }

        /// <summary>
        /// Generates a condition that imposes a lower and upper bound on the input
        /// index variable Expression, if the array that is being accessed has a fixed size.
        /// If no extra condition is required, null is returned.
        /// </summary>
        /// 
        /// <param name="indexExpr">Index of an array access Expression.</param>
        /// <param name="numElements">Number of elements in the array that is
        /// being accessed. If the array does not have a fixed size, this argument
        /// should be -1.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Condition that imposes a lower and upper bound on the input index
        /// variable Expression, if the array that is being accessed has a fixed size.
        /// If no extra condition is required, null is returned.</returns>
        public static Expression AddBoundsOnIndex(Expression indexExpr, int numElements, Path path)
        {
            return AddBoundsOnIndex(indexExpr, indexExpr, numElements, path);
        }

        /// <summary>
        /// Returns a pair whose first element is the Expression that will be used to replace
        /// the input Expression, which is the index of an array access, and whose second element
        /// is a list of conditions: these comprise equality Expressions between old index
        /// Expressions and new temporary index variable Expressions, and any extra conditions on
        /// these new temporary index variable Expressions.
        /// </summary>
        /// 
        /// <param name="indexExpr">Expression that is the index of an array access.</param>
        /// <param name="bounds">List of bounds (in number of elements) on each dimension
        /// of the array that is being accessed. If a dimension is not bounded, the list
        /// contains the value -1 for this dimension.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Pair as described.</returns>
        private static Pair<Expression, List<Expression>>
            ReplaceAndBoundIndex(Expression indexExpr, List<int> bounds, Path path)
        {
            Expression originalIndexExpr = indexExpr;
            Expression resultExpr = null;
            List<Expression> conditions = new List<Expression>();
            int arrayIndexUpperBound = bounds[0];

            if (!path.ProjectConfig.MODEL_AS_NESTED_ARRAYS &&
                originalIndexExpr.Op.Type == OperatorType.Concatenate)
            {
                /* The input Expression is constructed from the concatenation
                 * of indices: recursively replace all of these indices but the last,
                 * and collect the conditions that result from these replacements.
                 * The rest of the function will replace the final index. */
                Pair<Expression, List<Expression>> replacementAndBoundConditions =
                    ReplaceAndBoundIndex(originalIndexExpr.GetParameter(0),
                        bounds.GetRange(0, bounds.Count - 1), path);
                Expression replacementExpr = replacementAndBoundConditions.First;
                resultExpr = new Expression(OperatorStore.ConcatenateOp,
                    replacementExpr, originalIndexExpr.GetParameter(1),
                    originalIndexExpr.BitSize);
                conditions.AddRange(replacementAndBoundConditions.Second);

                indexExpr = originalIndexExpr.GetParameter(1);
                arrayIndexUpperBound = bounds[bounds.Count - 1];
            }

            Expression temporaryIndexExpr = path.GetNewTemporaryIndex(indexExpr.BitSize);
            temporaryIndexExpr.Type = indexExpr.Type;

            Expression equality = new Expression(OperatorStore.EqualOp,
                temporaryIndexExpr, indexExpr);
            conditions.Add(equality);

            /* Remember the expression that has been assigned to a temporary index. */
            string indexExprValue = temporaryIndexExpr.Value;
            string tempIndexIdent = path.Config.IDENT_TEMPINDEX;
            uint tempIndex = Convert.ToUInt32(indexExprValue.Substring(tempIndexIdent.Length));
            path.TemporaryIndexExpressions[tempIndex] = indexExpr;

            Expression boundCondition =
                AddBoundsOnIndex(indexExpr, temporaryIndexExpr, arrayIndexUpperBound, path);
            if (boundCondition != null) { conditions.Add(boundCondition); }

            if (!path.ProjectConfig.MODEL_AS_NESTED_ARRAYS &&
                originalIndexExpr.Op.Type == OperatorType.Concatenate)
            {
                resultExpr.UpdateParameter(1, temporaryIndexExpr);
            }
            else
            {
                resultExpr = temporaryIndexExpr;
            }

            return new Pair<Expression, List<Expression>>(resultExpr, conditions);
        }

        /// <summary>
        /// Replaces the index Expressions in all array access Expressions of the input Expression
        /// with temporary index variable Expressions. This method returns a pair whose first
        /// element is the new Expression and whose second element is a list of conditions: these
        /// comprise equality Expressions between the old index Expressions and the new temporary
        /// index variable Expressions, and any extra conditions on the new temporary index
        /// variable Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Expression that may contain array access Expressions.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Pair as described.</returns>
        public static Pair<Expression, List<Expression>> ReplaceIndices(Expression expr, Path path)
        {
            List<Expression> conditions = new List<Expression>();

            /* Base case: Zero-arity operators. */
            if (expr.Op.Arity == OperatorArity.Nil)
            {
                return new Pair<Expression, List<Expression>>(expr, conditions);
            }

            Expression resultExpr = null;
            OperatorType exprOpType = expr.Op.Type;
            switch (exprOpType)
            {
                /* Recursive case: Replace the index Expression of an array access Expression or
                 * of an array store Expression with a temporary index variable Expression. */
                case OperatorType.Array:
                case OperatorType.Store:
                    {
                        List<Expression> newParamList = new List<Expression>();
                        foreach (Expression paramExpr in expr.ParameterList)
                        {
                            Pair<Expression, List<Expression>> paramResult =
                                ReplaceIndices(paramExpr, path);
                            Expression newParamExpr = paramResult.First;
                            List<Expression> paramExprConditions = paramResult.Second;

                            newParamList.Add(newParamExpr);
                            conditions.AddRange(paramExprConditions);
                        }
                        resultExpr = new Expression(expr.Op, newParamList, expr.BitSize);
                        resultExpr.Type = expr.Type;

                        Expression newBaseExpr = resultExpr.GetParameter(0);
                        Expression newIndexExpr = resultExpr.GetParameter(1);

                        List<int> upperBounds = new List<int>();
                        if (path.ProjectConfig.MODEL_AS_NESTED_ARRAYS)
                        {
                            upperBounds.Add(GetArrayIndexUpperBound(newBaseExpr.Type, path));
                        }
                        else
                        {
                            upperBounds = GetArrayIndicesUpperBounds(newBaseExpr.Type, path);
                        }

                        Pair<Expression, List<Expression>> newIndexExprAndBoundConditions =
                            ReplaceAndBoundIndex(newIndexExpr, upperBounds, path);
                        newIndexExpr = newIndexExprAndBoundConditions.First;
                        resultExpr.UpdateParameter(1, newIndexExpr);
                        List<Expression> boundConditions = newIndexExprAndBoundConditions.Second;
                        conditions.AddRange(newIndexExprAndBoundConditions.Second);
                    }
                    break;

                /* Recursive case: Replace the indices in any array access Expressions that
                 * may occur in the parameters of the Expression. */
                default:
                    {
                        List<Expression> newParamList = new List<Expression>();
                        foreach (Expression paramExpr in expr.ParameterList)
                        {
                            Pair<Expression, List<Expression>> paramResult =
                                ReplaceIndices(paramExpr, path);
                            Expression newParamExpr = paramResult.First;
                            newParamList.Add(newParamExpr);

                            List<Expression> paramExprEqualities = paramResult.Second;
                            conditions.AddRange(paramExprEqualities);
                        }

                        resultExpr = new Expression(expr.Op, newParamList, expr.BitSize);
                        resultExpr.Type = expr.Type;
                    }
                    break;
            }

            Trace.Assert(resultExpr != null, "PHOENIX: The result Expression should not be null.");
            return new Pair<Expression, List<Expression>>(resultExpr, conditions);
        }

        #endregion

        #region Pointer Expression Methods

        /// <summary>
        /// Determines if the input Expression is a pointer Expression,
        /// or if it refers to an array.
        /// </summary>
        /// 
        /// <param name="expr">Expression that may be a pointer Expression,
        /// or that may refer to an array.</param>
        /// <returns>True if the input Expression is a pointer Expression,
        /// or if it refers to an array; false otherwise.</returns>
        public static bool IsPointerExpression(Expression expr)
        {
            return IsPointerExpressionType(expr.Type);
        }

        /// <summary>
        /// Determines if the input Expression type is a pointer type,
        /// or if the corresponding Expression refers to an array.
        /// </summary>
        /// 
        /// <param name="exprType">Expression type that may be a pointer type,
        /// or whose corresponding Expression may refer to an array.</param>
        /// <returns>True if the input Expression type is a pointer type,
        /// or if the corresponding Expression refers to an array; false otherwise.</returns>
        public static bool IsPointerExpressionType(Phx.Types.Type exprType)
        {
            return exprType.IsPointerType || exprType.IsUnmanagedArrayType;
        }

        /// <summary>
        /// Returns the type of the referent that the input pointer Expression refers to.
        /// </summary>
        /// 
        /// <param name="expr">Pointer Expression.</param>
        /// <returns>Type of the referent that the input pointer Expression refers to.</returns>
        public static Phx.Types.Type GetPointerReferentType(Expression expr)
        {
            Trace.Assert(IsPointerExpression(expr),
                "PHOENIX: Can only find the referent type for pointer Expressions.");
            return GetPointerReferentType(expr.Type);
        }

        /// <summary>
        /// Returns the type of the referent that the input pointer Expression type refers to.
        /// </summary>
        /// 
        /// <param name="exprType">Pointer Expression type.</param>
        /// <returns>Type of the referent that the input pointer Expression refers to.</returns>
        public static Phx.Types.Type GetPointerReferentType(Phx.Types.Type exprType)
        {
            Trace.Assert(IsPointerExpressionType(exprType),
                "PHOENIX: Can only find the referent type for pointer Expression types.");
            return exprType.IsUnmanagedArrayType ?
                exprType.AsUnmanagedArrayType.ElementType : exprType.AsPointerType.ReferentType;
        }

        /// <summary>
        /// Returns true if, and only if, the two input Expression types are the same.
        /// </summary>
        /// 
        /// <param name="oneType">One Expression type.</param>
        /// <param name="otherType">Another Expression type.</param>
        /// <returns>True if, and only if, the two input Expression types are the same.</returns>
        public static Boolean AreSameTypes(Phx.Types.Type oneType, Phx.Types.Type otherType)
        {
            if (oneType == null && otherType == null) { return true; }
            else if (oneType == null || otherType == null) { return false; }
            else if (oneType.IsUnmanagedArrayType && otherType.IsUnmanagedArrayType)
            {
                Phx.Types.Type oneReferentType = GetPointerReferentType(oneType);
                Phx.Types.Type otherReferentType = GetPointerReferentType(otherType);
                return (oneReferentType.BitSize == otherReferentType.BitSize) &&
                    AreSameTypes(oneReferentType, otherReferentType);
            }
            else if (IsPointerExpressionType(oneType) && IsPointerExpressionType(otherType))
            {
                Phx.Types.Type oneReferentType = GetPointerReferentType(oneType);
                Phx.Types.Type otherReferentType = GetPointerReferentType(otherType);
                return AreSameTypes(oneReferentType, otherReferentType);
            }
            else { return oneType.Equals(otherType); }
        }

        /// <summary>
        /// Creates a function Expression that takes two arguments and that, when applied,
        /// dereferences the input pointer Expression with the first argument as the index
        /// and the second argument as a bit offset from this dereference.
        /// 
        /// For the purposes of GameTime, this function Expression is equivalent to the pointer
        /// Expression itself.
        /// </summary>
        /// 
        /// <param name="pointerExpr">Pointer Expression.</param>
        /// <param name="path">Path that contains the input pointer Expression.</param>
        /// <returns>Function Expression as described.</returns>
        public static Expression MakeDereferencingFunction(Expression pointerExpr, Path path)
        {
            uint nativeWordBitSize = path.Config.WORD_BITSIZE;
            List<uint> arrayDims = GetArrayDimensions(pointerExpr, path);

            /* Construct a list of arguments for the function Expression, the dereferenced pointer
             * itself, and a list of pointer types that correspond to each level of
             * the dereference. */
            List<Expression> argumentList = new List<Expression>();
            List<Phx.Types.Type> pointerTypeList = new List<Phx.Types.Type>();

            Expression arrayAccessExpr = pointerExpr;
            for (int i = 1; i < arrayDims.Count; i++)
            {
                /* Create a temporary variable for the array access. */
                Expression indexArgExpr = path.GetNewTemporaryVariable(path.Config.WORD_BITSIZE);
                argumentList.Add(indexArgExpr);

                Phx.Types.Type referentType = GetPointerReferentType(arrayAccessExpr);
                pointerTypeList.Add(arrayAccessExpr.Type);

                arrayAccessExpr = new Expression(OperatorStore.ArrayOp, arrayAccessExpr,
                    indexArgExpr, arrayDims[i]);
                arrayAccessExpr.Type = referentType;

                /* Create a temporary variable for a bit offset from the array access. */
                Expression offsetArgExpr = path.GetNewTemporaryVariable(path.Config.WORD_BITSIZE);
                argumentList.Add(offsetArgExpr);

                Expression offsetExpr = new Expression(OperatorStore.OffsetOp,
                    arrayAccessExpr, offsetArgExpr, arrayDims[i]);
                offsetExpr.Type = referentType;

                arrayAccessExpr = offsetExpr;
            }

            /* Create the anonymous function. */
            argumentList.Reverse();
            pointerTypeList.Reverse();

            Expression funcExpr = arrayAccessExpr;
            for (int i = 0; i < pointerTypeList.Count; i++)
            {
                List<Expression> funcParamList = new List<Expression>();
                funcParamList.Add(argumentList[2*i + 1]);
                funcParamList.Add(argumentList[2*i]);
                funcParamList.Add(funcExpr);

                Phx.Types.Type pointerType = pointerTypeList[i];
                funcExpr = new Expression(OperatorStore.FunctionOp,
                    funcParamList, nativeWordBitSize);
                funcExpr.Type = pointerType;
            }
            return funcExpr;
        }

        /// <summary>
        /// Offsets the input pointer Expression by the amount specified in the input bit offset
        /// Expression. The input pointer Expression is a "dereferencing function", and
        /// this method modifies the index and the offset of the dereference Expression in
        /// the body of the function to include the new offset. This process helps to account
        /// for pointer aliasing.
        /// </summary>
        /// 
        /// <param name="pointerExpr">Pointer Expression to offset.</param>
        /// <param name="offsetExpr">Expression that specifies the amount to offset the input
        /// Pointer Expression by, in bits.</param>
        /// <param name="path">Path that contains the input pointer Expression.</param>
        /// <returns>New Expression that offsets the input pointer Expression by the amount
        /// specified in the input bit offset Expression. The original Expression is
        /// not modified.</returns>
        public static Expression AddOffsetToPointer(Expression pointerExpr,
            Expression offsetExpr, Path path)
        {
            uint nativeWordBitSize = path.Config.WORD_BITSIZE;

            Trace.Assert(IsPointerExpression(pointerExpr),
                "PHOENIX: Can only add offsets to pointers.");
            Trace.Assert((pointerExpr.Op.Type == OperatorType.Function &&
                pointerExpr.ParameterList.Count >= 3) ||
                (pointerExpr.Op.Type == OperatorType.Ite),
                "PHOENIX: The pointer Expression should be a dereferencing function.");

            if (pointerExpr.Op.Type == OperatorType.Ite)
            {
                /* If the input Expression is an if-then-else Expression, then the consequent
                 * and alternative Expressions are pointer Expressions. Create a new
                 * if-then-else Expression that offsets these pointer Expressions. */
                Expression conditionalExpr = pointerExpr.GetParameter(0);
                Expression consequentExpr = pointerExpr.GetParameter(1);
                Expression alternativeExpr = pointerExpr.GetParameter(2);

                List<Expression> iteParamList = new List<Expression>();
                iteParamList.Add(conditionalExpr);
                iteParamList.Add(AddOffsetToPointer(consequentExpr, offsetExpr, path));
                iteParamList.Add(AddOffsetToPointer(alternativeExpr, offsetExpr, path));

                Expression result = new Expression(OperatorStore.IteOp,
                    iteParamList, pointerExpr.BitSize);
                result.Type = pointerExpr.Type;
                return result;
            }

            pointerExpr = pointerExpr.Clone();
            Expression indexArgumentExpr = pointerExpr.GetParameter(0);
            Expression offsetArgumentExpr = pointerExpr.GetParameter(1);
            Expression bodyExpr = pointerExpr.GetParameter(2);

            /* Find the type and bit-size of the referent of the pointer Expression. */
            Phx.Types.Type referentType = GetPointerReferentType(pointerExpr);
            uint referentBitSize = referentType.BitSize;
            Expression referentBitSizeExpr = new Constant(referentBitSize, nativeWordBitSize);

            /* Find the array index that corresponds to the input offset Expression. */
            Expression arrayIndexExpr = new Expression(OperatorStore.SDivOp,
                offsetExpr, referentBitSizeExpr, offsetExpr.BitSize);
            arrayIndexExpr.Type = offsetExpr.Type;
            arrayIndexExpr = SimplifyExpression(arrayIndexExpr, path);

            /* Add this number to any indices that may already exist. */
            Expression newIndexExpr =
                new Expression(OperatorStore.AddOp, indexArgumentExpr, arrayIndexExpr,
                    indexArgumentExpr.BitSize);
            bodyExpr = bodyExpr.Replace(indexArgumentExpr, newIndexExpr);

            /* Find any extra bit offset. */
            Expression extraOffsetExpr = new Expression(OperatorStore.RemOp,
                offsetExpr, referentBitSizeExpr, offsetExpr.BitSize);
            extraOffsetExpr.Type = offsetExpr.Type;
            extraOffsetExpr = SimplifyExpression(extraOffsetExpr, path);

            /* Add this number to any offsets that may already exist. */
            Expression newOffsetExpr =
                new Expression(OperatorStore.AddOp, offsetArgumentExpr, extraOffsetExpr,
                    offsetArgumentExpr.BitSize);
            newOffsetExpr.Type = offsetArgumentExpr.Type;
            bodyExpr = bodyExpr.Replace(offsetArgumentExpr, newOffsetExpr);

            bodyExpr = SimplifyExpression(bodyExpr, path);
            pointerExpr.UpdateParameter(2, bodyExpr);
            return pointerExpr;
        }

        /// <summary>
        /// Returns a new Expression that results from dereferencing the input pointer Expression.
        /// </summary>
        /// 
        /// <param name="pointerExpr">Pointer Expression.</param>
        /// <param name="derefType">Type of the result.</param>
        /// <param name="derefAggAccesses">True if the dereference should return accesses to
        /// fields inside aggregates; false if only the aggregate itself should
        /// be returned.</param>
        /// <param name="path">Path that contains the input pointer Expression.</param>
        /// <returns>New Expression that results from dereferencing the input
        /// pointer Expression. The original Expression is not modified.</returns>
        /// <remarks>Precondition: The Expression should refer to a memory location.</remarks>
        public static Expression DereferencePointer(Expression pointerExpr,
            Phx.Types.Type derefType, bool derefAggAccesses, Path path)
        {
            uint nativeWordBitSize = path.Config.WORD_BITSIZE;

            /* Check if the input pointer Expression aliases another Expression. */
            pointerExpr = path.FindAliasedExpression(pointerExpr);

            Trace.Assert(IsPointerExpression(pointerExpr),
                "PHOENIX: Only pointer Expressions can be dereferenced.");

            /* A pointer is represented as a "dereferencing function" Expression. As a result,
             * dereferencing a pointer is equivalent to applying this function on
             * appropriate arguments: a zero index and zero offset. */
            List<Expression> argumentList = new List<Expression>();
            argumentList.Add(new Constant(0, nativeWordBitSize));
            argumentList.Add(new Constant(0, nativeWordBitSize));

            Expression result = ApplyFunction(pointerExpr, argumentList, path);
            return LookupAndReplaceOffset(result, derefType, derefAggAccesses, path);
        }

        /// <summary>
        /// Replaces each offset Expression in the input Expression with the memory location that
        /// it refers to, which could either be a variable, an array access or an aggregate access.
        /// 
        /// Offsets within a function Expression are not touched, since complete information is
        /// not yet available to determine the memory location: for example, the offset could
        /// be one of the arguments to the function Expression.
        /// </summary>
        /// 
        /// <param name="expr">Expression that may contain offset Expressions.</param>
        /// <param name="derefType">Type of the result.</param>
        /// <param name="derefAggAccesses">True if the offset replacement should return accesses
        /// to fields inside aggregates; false if only the aggregate itself should
        /// be returned.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>New Expression that results from replacing each offset Expression in
        /// the input Expression with the memory location that it refers to, which could
        /// either be a variable, an array access or an aggregate access. The original Expression
        /// is not modified.</returns>
        public static Expression LookupAndReplaceOffset(Expression expr, Phx.Types.Type derefType,
            bool derefAggAccesses, Path path)
        {
            if (expr.Op.Arity == OperatorArity.Nil) { return expr; }
            if (expr.Op.Type == OperatorType.Function) { return expr; }

            Expression result = null;

            if (expr.Op.Type == OperatorType.Offset)
            {
                Expression baseExpr = expr.GetParameter(0);
                Expression offsetExpr = expr.GetParameter(1);

                result = LookupAndReplaceOffset(baseExpr, derefType, derefAggAccesses, path);
                if (IsAggregateExpression(result) && derefAggAccesses)
                {
                    /* If the array access results in an aggregate, the offset Expression is
                     * actually a bit offset from the start of this aggregate. Find the field
                     * in the aggregate that is accessed by this dereference. */
                    result = MakeAggregateAccess(result, offsetExpr, derefType, path);
                }
                else
                {
                    if (IsConstantExpression(offsetExpr) && ConvertToConstant(offsetExpr) != 0)
                    {
                        /* There is still a non-zero offset that must be dealt with. If the offset
                         * is done on an aggregate, but fields are not accessed, then the
                         * aggregate is being aliased by another. Return the original Expression
                         * for use by other methods. */
                        result = expr;
                    }
                }
                return result;
            }

            List <Expression> newParamList = new List<Expression>();
            foreach (Expression paramExpr in expr.ParameterList)
            {
                Expression newParamExpr =
                    LookupAndReplaceOffset(paramExpr, derefType, derefAggAccesses, path);
                newParamList.Add(newParamExpr);
            }
            result = new Expression(expr.Op, newParamList, expr.BitSize);
            result.Type = expr.Type;
            return result;
        }

        #endregion

        #region Aggregate Expression Methods

        /// <summary>
        /// Determines if the input Expression is an aggregate Expression:
        /// an Expression that represents an aggregate (a struct or a union).
        /// </summary>
        /// 
        /// <param name="expr">Expression that may be an aggregate Expression.</param>
        /// <returns>True, if the input Expression is an aggregate Expression;
        /// false, otherwise.</returns>
        public static bool IsAggregateExpression(Expression expr)
        {
            return IsAggregateExpressionType(expr.Type);
        }

        /// <summary>
        /// Determines if the input Expression type is an aggregate type:
        /// a type that represents an aggregate (a struct or a union).
        /// </summary>
        /// 
        /// <param name="exprType">Expression type that may be an aggregate type.</param>
        /// <returns>True if the input Expression type is an aggregate type:
        /// a type that represents an aggregate (a struct or a union);
        /// false otherwise.</returns>
        public static bool IsAggregateExpressionType(Phx.Types.Type exprType)
        {
            return exprType.IsAggregateType;
        }

        /// <summary>
        /// Returns the Expression that corresponds to the input aggregate access. Note that
        /// the aggregate access is converted into an access into an array. For example,
        /// the aggregate access "a.b" is converted into the Expression "b[a]" (annotated
        /// uniquely to distinguish it from other arrays that may have the name "b").
        /// This conversion has the following semantics: "b" is an array of all of the "b"
        /// fields in aggregates that have the same type as that of "a", and "b[a]" is
        /// the particular value of the "b" field located in the aggregate "a".
        /// </summary>
        /// 
        /// <param name="aggregateExpr">Expression for the aggregate that
        /// is being accessed.</param>
        /// <param name="bitOffsetExpr">Bit offset of the field from the start of
        /// the aggregate.</param>
        /// <param name="accessType">Type of the accessed field.</param>
        /// <param name="path">Path that contains the operands.</param>
        /// <returns>Expression for the input aggregate access.</returns>
        public static Expression MakeAggregateAccess(Expression aggregateExpr,
            Expression bitOffsetExpr, Phx.Types.Type accessType, Path path)
        {
            Configuration.Endianness endianness = path.Config.ENDIANNESS;
            Expression result = null;

            Trace.Assert(IsConstantExpression(bitOffsetExpr),
                "PHOENIX: Only constant offsets from the beginning of " +
                "an aggregate are supported.");
            int bitOffset = ConvertToConstant(bitOffsetExpr);

            /* Determine the actual type of the field of the input aggregate type that is being
             * accessed. This can be different from the input type when, for example, the field
             * that is being accessed is a fixed-size array: in this case, the input type will
             * be a pointer type. (TODO: This could change once we start to model the casting of
             * variables with pointer types to variables with primitive types.) */
            List<AggregateField> aggregateFields = PhoenixHelper.GetAggregateFields(aggregateExpr,
                bitOffset, accessType, false, path);
            Trace.Assert(aggregateFields.Count == 1,
                "PHOENIX: Only one field of an aggregate can be accessed at a time.");
            AggregateField originalField = aggregateFields[0];
            Phx.Types.Type originalFieldType = originalField.AccessExpr.Type;
            accessType = AreSameTypes(accessType, originalFieldType) ?
                originalFieldType : accessType;

            /* Find the size of the field of the input aggregate type that is being accessed. */
            uint accessBitSize = accessType.BitSize;

            /* Find the "base aggregate" for this aggregate, if any. */
            Pair<Expression, Expression> aggregateBaseAndOffset =
                path.FindAggregateBaseAndOffset(aggregateExpr);
            Expression baseAggregateExpr = aggregateBaseAndOffset.First;
            Expression baseOffsetExpr = aggregateBaseAndOffset.Second;

            Trace.Assert(IsConstantExpression(baseOffsetExpr),
                "PHOENIX: Only constant offsets from the beginning of " +
                "an aggregate are supported.");
            int baseOffset = ConvertToConstant(baseOffsetExpr);
            int totalOffset = baseOffset + bitOffset;

            /* Find the fields of the base aggregate type that overlap with
             * the field being accessed (of an aggregate that may of may not be
             * the same as the base aggregate). */
            List<AggregateField> baseAggregateFields =
                PhoenixHelper.GetAggregateFields(baseAggregateExpr, totalOffset,
                    accessType, true, path);

            if (accessType.IsUnmanagedArrayType &&
                !AreSameTypes(accessType, baseAggregateFields[0].AccessExpr.Type))
            {
                /* The field of the input aggregate type that is being accessed is
                 * a fixed-size array. However, either multiple fields of the base
                 * aggregate type overlap with this access, or the field of the base
                 * aggregate type that overlaps is not a fixed-size array. In both cases,
                 * construct a function Expression that, when called with the appropriate
                 * arguments, accesses different fields of the base aggregate type. */
                return MakeAggregateFieldFunction(baseAggregateExpr, totalOffset,
                    accessType, path);
            }

            /* Store the start offset and the end offset of the aggregate access. */
            int accessStartOffset = totalOffset;
            int accessEndOffset = totalOffset + (int)accessBitSize - 1;

            /* Construct the aggregate access Expression. */
            while (baseAggregateFields.Count > 0)
            {
                AggregateField baseAggregateField = baseAggregateFields[0];
                baseAggregateFields.RemoveAt(0);

                /* Obtain the Expression that corresponds to the current field of the base
                 * aggregate type. This Expression will be modified below as needed. */
                Expression fieldAccessExpr = baseAggregateField.AccessExpr;

                /* Obtain the bit-size of the current field of the base aggregate type. */
                uint fieldBitSize = fieldAccessExpr.BitSize;

                /* The body of the conditional statement is not executed if
                 * the current field of the base aggregate type is a fixed-size array,
                 * and the type of the input aggregate access is also a fixed-size array
                 * of 'the same type'. */
                if (!(fieldAccessExpr.Type.IsUnmanagedArrayType &&
                      AreSameTypes(fieldAccessExpr.Type, accessType)))
                {
                    /* Find the start offset and the end offset of the current field of
                     * the base aggregate type. */
                    int fieldStartOffset = baseAggregateField.StartOffset;
                    int fieldEndOffset = baseAggregateField.StartOffset + (int)fieldBitSize - 1;

                    /* Determine the start and end indices of the range of bits that is being
                     * accessed in the current field of the base aggregate type. */
                    uint startIndex = (uint)Math.Max(0, accessStartOffset - fieldStartOffset);
                    uint endIndex =
                        (uint)(Math.Min(fieldEndOffset, accessEndOffset) - fieldStartOffset);
                    uint range = endIndex - startIndex + 1;

                    /* Adjust the start and end indices to account for the endianness of
                     * the target machine. */
                    startIndex = (endianness == Configuration.Endianness.LITTLE) ? startIndex :
                        (uint)((fieldBitSize - 1) - endIndex);
                    endIndex = (endianness == Configuration.Endianness.LITTLE) ? endIndex :
                        (startIndex + range) - 1;
                    range = endIndex - startIndex + 1;

                    /* Extract the range of bits. */
                    fieldAccessExpr = ExtractBits(fieldAccessExpr, startIndex, endIndex, path);

                    if ((baseAggregateFields.Count == 0) && (range < accessBitSize))
                    {
                        /* If no more fields of the base aggregate type overlap, pad
                         * the Expression with as many zeros as needed to account for
                         * all of the bits of the access.
                         * TODO: This is a temporary fix. We should be able to assign
                         * a variable that ensures that these bits really are zero,
                         * because they may otherwise be garbage. */
                        fieldAccessExpr = (endianness == Configuration.Endianness.LITTLE) ?
                            PadWithZeros(fieldAccessExpr, accessBitSize - range, 0, path) :
                            PadWithZeros(fieldAccessExpr, 0, accessBitSize - range, path);
                    }
                }

                if (result == null)
                {
                    result = fieldAccessExpr;
                }
                else
                {
                    result = (endianness == Configuration.Endianness.LITTLE) ?
                        new Expression(OperatorStore.ConcatenateOp, fieldAccessExpr, result,
                            fieldAccessExpr.BitSize + result.BitSize) :
                        new Expression(OperatorStore.ConcatenateOp, result, fieldAccessExpr,
                            result.BitSize + fieldAccessExpr.BitSize);
                }
                
                /* Remove the size of the current field of the base aggregate type from
                 * the size of the accessed field of the input aggregate type. */
                accessBitSize -= Math.Min(accessBitSize, fieldBitSize);
            }

            if (!AreSameTypes(result.Type, accessType)) { result.Type = accessType; }
            return result;
        }

        /// <summary>
        /// Recursive helper method for the method <see cref="MakeAggregateAccess"/>.
        /// This method constructs a function Expression that, when called with
        /// the appropriate arguments, returns accesses to different fields within
        /// the input aggregate. The fields that are included in the function Expression
        /// are those that overlap with a field that is being accessed, whose bit offset
        /// (from the start of the aggregate) and type are provided.
        /// 
        /// Since this method is only used in <see cref="MakeAggregateAccess"/> if
        /// the field is a fixed-size array, the type provided should be that of
        /// a fixed-size array.
        /// </summary>
        /// 
        /// <param name="aggregateExpr">Expression for the aggregate that is
        /// being accessed.</param>
        /// <param name="bitOffset">Bit offset of the field from the start of
        /// the aggregate.</param>
        /// <param name="accessType">Type of the accessed field. Note that this
        /// field may not be a member of the aggregate provided, but possibly a member of
        /// an aliasing aggregate.</param>
        /// <param name="path">Path that contains the aggregate Expression.</param>
        /// <returns>Function Expression, as described above.</returns>
        /// <remarks>Type of the accessed field is that of a fixed-size array.</remarks>
        private static Expression MakeAggregateFieldFunction(Expression aggregateExpr,
            int bitOffset, Phx.Types.Type accessType, Path path)
        {
            Trace.Assert(accessType.IsUnmanagedArrayType,
                "PHOENIX: Fixed-size array type expected.");

            uint nativeWordBitSize = path.Config.WORD_BITSIZE;

            /* Create the seed of the result Expression. The body of the resulting function
             * Expression will be an if-then-else Expression. This seed should never be
             * the result of calling the function Expression with valid arguments. */
            Phx.Types.Type referentType = GetPointerReferentType(accessType);
            Expression result = IsPointerExpressionType(referentType) ?
                path.GetNewTemporaryPointer(referentType) :
                path.GetNewTemporaryVariable(referentType.BitSize);
            result.Type = referentType;
            path.AddVariable(result);

            /* Create the Expressions for the the formal parameters of the result,
             * which represent the index of an array access and a bit offset from
             * the start of this access. */
            Expression indexArgExpr = path.GetNewTemporaryVariable(nativeWordBitSize);
            Expression offsetArgExpr = path.GetNewTemporaryVariable(nativeWordBitSize);

            List<Expression> funcParamList = new List<Expression>();
            funcParamList.Add(indexArgExpr);
            funcParamList.Add(offsetArgExpr);

            int upperBound = GetArrayIndexUpperBound(accessType, path);
            for (int currIndex = 0; currIndex < upperBound; currIndex++)
            {
                /* For each index of the accessed field, determine the fields of
                 * the provided aggregate that are overlapped. */
                int totalOffset = bitOffset + (int)(currIndex * referentType.BitSize);
                Expression fieldAccessExpr = null;

                if (referentType.IsUnmanagedArrayType)
                {
                    fieldAccessExpr = MakeAggregateFieldFunction(aggregateExpr,
                        totalOffset, referentType, path);
                }
                else
                {
                    Expression totalOffsetExpr = new Constant(totalOffset, nativeWordBitSize);
                    fieldAccessExpr = MakeAggregateAccess(aggregateExpr,
                        totalOffsetExpr, referentType, path);
                }

                Expression fieldAccessOffsetExpr = new Expression(OperatorStore.OffsetOp,
                    fieldAccessExpr, offsetArgExpr, fieldAccessExpr.BitSize);
                fieldAccessOffsetExpr.Type = fieldAccessExpr.Type;

                /* Create an if-then-else Expression that compares the formal
                 * parameter that represents the index of an array access with
                 * the index that is currently being examined. */
                Expression currIndexExpr = new Constant(currIndex, nativeWordBitSize);

                List<Expression> iteParamList = new List<Expression>();
                iteParamList.Add(new Expression(OperatorStore.EqualOp,
                    indexArgExpr, currIndexExpr, nativeWordBitSize));
                iteParamList.Add(fieldAccessOffsetExpr);
                iteParamList.Add(result);

                result = new Expression(OperatorStore.IteOp,
                    iteParamList, fieldAccessExpr.BitSize);
                result.Type = fieldAccessOffsetExpr.Type;
            }
            funcParamList.Add(result);

            result = new Expression(OperatorStore.FunctionOp, funcParamList, nativeWordBitSize);
            result.Type = accessType;
            return result;
        }

        #endregion

        #region Function Expression Methods

        /// <summary>
        /// Determines if the input Expression represents a function.
        /// </summary>
        /// 
        /// <param name="expr">Expression that may represent a function.</param>
        /// <returns>True if the Expression represents a function; false otherwise.</returns>
        public static bool IsFunctionExpression(Expression expr)
        {
            return expr.Op.Type == OperatorType.Function;
        }

        /// <summary>
        /// Returns a new Expression that represents the result of applying the function
        /// Expression provided on the list of argument Expressions provided.
        /// </summary>
        /// 
        /// <param name="funcExpr">Function Expression to apply.</param>
        /// <param name="argExprs">List of Argument Expressions to apply the function on.</param>
        /// <param name="path">Path that contains the input Expressions.</param>
        /// <returns>New Expression that results from applying the function Expression
        /// provided on the argument Expressions provided.</returns>
        /// <remarks>Precondition: The input Expression is a function Expression.</remarks>
        public static Expression ApplyFunction(Expression funcExpr,
            List<Expression> argExprs, Path path)
        {
            Trace.Assert(IsFunctionExpression(funcExpr) || funcExpr.Op.Type == OperatorType.Ite,
                "PHOENIX: Cannot apply non-functions.");

            Expression result = null;

            if (funcExpr.Op.Type == OperatorType.Ite)
            {
                /* If the input Expression is an if-then-else Expression, then the consequent
                 * and alternative Expressions are function Expressions. Apply these function
                 * Expressions on the argument Expressions provided. */
                Expression conditionalExpr = funcExpr.GetParameter(0);

                Expression consequentExpr = funcExpr.GetParameter(1);
                Expression appliedConsequentExpr = ApplyFunction(consequentExpr, argExprs, path);

                Expression alternativeExpr = funcExpr.GetParameter(2);
                Expression appliedAlternativeExpr = ApplyFunction(alternativeExpr, argExprs, path);

                List<Expression> iteParamList = new List<Expression>();
                iteParamList.Add(conditionalExpr);
                iteParamList.Add(appliedConsequentExpr);
                iteParamList.Add(appliedAlternativeExpr);

                result = new Expression(OperatorStore.IteOp,
                    iteParamList, appliedConsequentExpr.BitSize);
                result.Type = appliedConsequentExpr.Type;
                return result;
            }

            int lastParamLoc = funcExpr.ParameterList.Count - 1;
            List<Expression> formalArgExprs = funcExpr.ParameterList.GetRange(0, lastParamLoc);
            Expression bodyExpr = funcExpr.ParameterList[lastParamLoc];
            result = bodyExpr;

            foreach (Expression argExpr in argExprs)
            {
                Expression formalArgExpr = formalArgExprs[0];
                formalArgExprs.RemoveAt(0);

                result = SimplifyExpression(result.Replace(formalArgExpr, argExpr), path);
            }

            Trace.Assert(result != null,
                "PHOENIX: The result of a function application should not be null.");
            return result;
        }

        #endregion

        #region Expression Modification Methods

        /// <summary>
        /// Converts the input Expression, which represents a number of bytes,
        /// into another Expression that represents the equivalent number of bits.
        /// </summary>
        /// 
        /// <param name="expr">Expression to convert, which represents a number of bytes.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Expression that represents the equivalent number of bits.</returns>
        public static Expression ConvertToBits(Expression expr, Path path)
        {
            Expression result = new Expression(OperatorStore.MultOp,
                expr, new Constant(8, expr.BitSize), expr.BitSize);
            result.Type = expr.Type;
            return SimplifyExpression(result, path);
        }

        /// <summary>
        /// Converts the input Expression, which represents a number of bits,
        /// into another Expression that represents the equivalent number of
        /// bytes, rounded down.
        /// </summary>
        /// 
        /// <param name="expr">Expression to convert, which represents a number of bits.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Expression that represents the equivalent number of bytes,
        /// rounded down.</returns>
        public static Expression ConvertToBytes(Expression expr, Path path)
        {
            Expression result = new Expression(OperatorStore.SDivOp,
                expr, new Constant(8, expr.BitSize), expr.BitSize);
            result.Type = expr.Type;
            return SimplifyExpression(result, path);
        }


        /// <summary>
        /// Adjusts the bit-size of the input Expression to match the input target bit-size.
        /// This adjustment involves either sign-extension or bit-extraction.
        /// </summary>
        /// 
        /// <param name="expr">Expression to adjust.</param>
        /// <param name="targetBitSize">Target bit-size for the input Expression.</param>
        /// <param name="zeroExtend">True if the input Expression should be zero-extended if smaller
        /// than the input target bit-size; false otherwise.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>New Expression that adjusts the bit-size of the input Expression to match
        /// the input target bit-size.</returns>
        public static Expression AdjustBitSize(Expression expr, uint targetBitSize,
            bool zeroExtend, Path path)
        {
            Expression result = expr;
            uint nativeWordBitSize = path.Config.WORD_BITSIZE;

            uint exprBitSize = expr.BitSize;
            int diff = (int)targetBitSize - (int)exprBitSize;
            if (diff > 0)
            {
                Operator extOpToUse = zeroExtend ?
                    OperatorStore.ZeroExtendOp : OperatorStore.SignExtendOp;
                result = new Expression(extOpToUse, expr, new Constant(diff, nativeWordBitSize),
                    targetBitSize);
            }
            else if (diff < 0)
            {
                result = ExtractBits(expr, 0, (targetBitSize - 1), path);
            }

            return result;
        }

        /// <summary>
        /// Returns a new Expression that extracts a contiguous range of bits from
        /// the input Expression.
        /// </summary>
        /// 
        /// <param name="expr">Expression to extract from.</param>
        /// <param name="start">Start index of the contiguous range.</param>
        /// <param name="end">End index of the contiguous range.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>New Expression that extracts a contiguous range of bits from
        /// the input Expression.</returns>
        public static Expression ExtractBits(Expression expr, uint start, uint end, Path path)
        {
            uint nativeWordBitSize = path.Config.WORD_BITSIZE;

            /* Optimization: If the range includes the input Expression,
             * return the Expression untouched. */
            if ((start == 0) && (end >= (expr.BitSize - 1))) { return expr; }

            Expression startIndex = new Constant(start, nativeWordBitSize);
            Expression endIndex = new Constant(end, nativeWordBitSize);

            List<Expression> paramExprList = new List<Expression>();
            paramExprList.Add(expr);
            paramExprList.Add(startIndex);
            paramExprList.Add(endIndex);

            uint resultBitSize = end - start + 1;
            return new Expression(OperatorStore.BitExtractOp, paramExprList, resultBitSize);
        }

        /// <summary>
        /// Returns a new Expression that pads the input Expression with zeros.
        /// </summary>
        /// 
        /// <param name="expr">Expression to pad.</param>
        /// <param name="leftPadSize">Number of zeros to pad to the left of the Expression
        /// (beyond the most significant bit).</param>
        /// <param name="rightPadSize">Number of zeros to pad to the right of the Expression
        /// (beyond the least significant bit).</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>New Expression that pads the input Expression with zeros.</returns>
        public static Expression PadWithZeros(Expression expr,
            uint leftPadSize, uint rightPadSize, Path path)
        {
            Expression result = expr;
            if (leftPadSize != 0)
            {
                result = new Expression(OperatorStore.ZeroExtendOp, result,
                    new Constant(leftPadSize, path.Config.WORD_BITSIZE),
                    result.BitSize + leftPadSize);
            }
            if (rightPadSize != 0)
            {
                result = new Expression(OperatorStore.ConcatenateOp, result,
                    new Constant(0, rightPadSize), result.BitSize + rightPadSize);
            }
            return result;
        }

        /// <summary>
        /// Determines if the input Expression is a constant Expression.
        /// </summary>
        /// 
        /// <param name="expr">Expression that may be a constant Expression.</param>
        /// <returns>True if the input Expression is a constant Expression;
        /// false otherwise.</returns>
        public static bool IsConstantExpression(Expression expr)
        {
            return expr.Op.Type == OperatorType.Constant ||
                (expr.Op.Type == OperatorType.Negate &&
                    IsConstantExpression(expr.GetParameter(0)));
        }

        /// <summary>
        /// Converts a constant Expression to the integer constant that it represents.
        /// </summary>
        /// 
        /// <param name="constantExpr">Constant Expression to convert.</param>
        /// <returns>Integer constant that the input constant Expression represents.</returns>
        public static int ConvertToConstant(Expression constantExpr)
        {
            Trace.Assert(IsConstantExpression(constantExpr),
                "PHOENIX: Cannot convert non-constant Expressions to constants.");
            return (constantExpr.Op.Type == OperatorType.Constant) ?
                Convert.ToInt32(constantExpr.Value) :
                (-1 * Convert.ToInt32(constantExpr.GetParameter(0).Value));
        }

        /// <summary>
        /// Simplifies the input Expression as much as possible. This method does not perform
        /// exhaustive simplification: it is meant to simplify commonly occurring types
        /// of Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Expression to simplify.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Simplified (as much as possible) version of the input Expression.
        /// The original Expression is not modified.</returns>
        public static Expression SimplifyExpression(Expression expr, Path path)
        {
            uint nativeWordBitSize = path.Config.WORD_BITSIZE;
            Expression result = expr;

            OperatorType exprOpType = expr.Op.Type;
            switch (exprOpType)
            {
                case OperatorType.Addition:
                    result = SimplifyAdditionExpression(expr, path);
                    break;

                case OperatorType.Subtraction:
                    result = SimplifySubtractionExpression(expr, path);
                    break;

                case OperatorType.Multiplication:
                    result = SimplifyMultiplicationExpression(expr, path);
                    break;

                case OperatorType.SDivision:
                case OperatorType.UDivision:
                    result = SimplifyDivisionExpression(expr, path);
                    break;

                case OperatorType.Remainder:
                    result = SimplifyRemainderExpression(expr, path);
                    break;

                case OperatorType.Equal:
                    result = SimplifyEqualityExpression(expr, path);
                    break;

                case OperatorType.Ite:
                    result = SimplifyIteExpression(expr, path);
                    break;

                default:
                    if (expr.Op.Arity == OperatorArity.Nil) { break; }

                    /* Recursive case: Simplify the parameters of this Expression. */
                    List<Expression> newParamList = new List<Expression>();
                    foreach (Expression paramExpr in expr.ParameterList)
                    {
                        Expression newParamExpr = SimplifyExpression(paramExpr, path);
                        newParamList.Add(newParamExpr);
                    }
                    result = new Expression(expr.Op, newParamList, expr.BitSize);
                    break;
            }

            result.Type = expr.Type;
            return result;
        }

        /// <summary>
        /// Simplifies the input addition Expression as much as possible. This method does not
        /// perform exhaustive simplification: it is meant to simplify commonly occurring types
        /// of addition Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Addition Expression to simplify.</param>
        /// <param name="path">Path that contains the input addition Expression.</param>
        /// <returns>Simplified (as much as possible) version of the input addition Expression.
        /// The original Expression is not modified.</returns>
        private static Expression SimplifyAdditionExpression(Expression expr, Path path)
        {
            Expression simplerAugend = SimplifyExpression(expr.GetParameter(0), path);
            Expression simplerAddend = SimplifyExpression(expr.GetParameter(1), path);
            Expression result = new Expression(OperatorStore.AddOp,
                simplerAugend, simplerAddend, expr.BitSize);

            /* Simplify the Expression even further. */
            if (IsConstantExpression(simplerAugend) && IsConstantExpression(simplerAddend))
            {
                int newConstant =
                    ConvertToConstant(simplerAugend) + ConvertToConstant(simplerAddend);
                result = new Constant(newConstant, expr.BitSize);
            }
            else if (IsConstantExpression(simplerAddend) &&
                simplerAugend.Op.Type == OperatorType.Addition)
            {
                Expression augendAugend = simplerAugend.GetParameter(0);
                Expression augendAddend = simplerAugend.GetParameter(1);
                if (IsConstantExpression(augendAddend))
                {
                    /* We might be trying to add more bits to a previous pointer bit offset. */
                    int augendAddendConstant = ConvertToConstant(augendAddend);
                    int addendConstant = ConvertToConstant(simplerAddend);
                    int newOffsetConstant = augendAddendConstant + addendConstant;
                    result = (newOffsetConstant != 0) ?
                        new Expression(OperatorStore.AddOp, augendAugend,
                            new Constant(newOffsetConstant, expr.BitSize), expr.BitSize) :
                        augendAugend;
                }
            }
            else if (IsConstantExpression(simplerAugend))
            {
                int augendConstant = ConvertToConstant(simplerAugend);
                if (augendConstant == 0) { result = simplerAddend; }
            }
            else if (IsConstantExpression(simplerAddend))
            {
                int addendConstant = ConvertToConstant(simplerAddend);
                if (addendConstant == 0) { result = simplerAugend; }
            }

            result.Type = expr.Type;
            return result;
        }

        /// <summary>
        /// Simplifies the input subtraction Expression as much as possible. This method does not
        /// perform exhaustive simplification: it is meant to simplify commonly occurring types
        /// of subtraction Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Subtraction Expression to simplify.</param>
        /// <param name="path">Path that contains the input subtraction Expression.</param>
        /// <returns>Simplified (as much as possible) version of the input subtraction Expression.
        /// The original Expression is not modified.</returns>
        private static Expression SimplifySubtractionExpression(Expression expr, Path path)
        {
            Expression simplerMinuend = SimplifyExpression(expr.GetParameter(0), path);
            Expression simplerSubtrahend = SimplifyExpression(expr.GetParameter(1), path);
            Expression result = new Expression(OperatorStore.SubOp,
                simplerMinuend, simplerSubtrahend, expr.BitSize);

            /* Simplify the Expression even further. */
            if (IsConstantExpression(simplerMinuend) && IsConstantExpression(simplerSubtrahend))
            {
                int newConstant =
                    ConvertToConstant(simplerMinuend) - ConvertToConstant(simplerSubtrahend);
                result = new Constant(newConstant, expr.BitSize);
            }
            else if (IsConstantExpression(simplerMinuend))
            {
                int minuendConstant = ConvertToConstant(simplerMinuend);
                if (minuendConstant == 0)
                {
                    result = new Expression(OperatorStore.NegateOp,
                        simplerSubtrahend, expr.BitSize);
                }
            }
            else if (IsConstantExpression(simplerSubtrahend))
            {
                int subtrahendConstant = ConvertToConstant(simplerSubtrahend);
                if (subtrahendConstant == 0) { result = simplerMinuend; }
            }

            result.Type = expr.Type;
            return result;
        }

        /// <summary>
        /// Simplifies the input multiplication Expression as much as possible. This method
        /// does not perform exhaustive simplification: it is meant to simplify commonly
        /// occurring types of multiplication Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Multiplication Expression to simplify.</param>
        /// <param name="path">Path that contains the input multiplication Expression.</param>
        /// <returns>Simplified (as much as possible) version of the input multiplication
        /// Expression. The original Expression is not modified.</returns>
        private static Expression SimplifyMultiplicationExpression(Expression expr, Path path)
        {
            Expression simplerMultiplicand = SimplifyExpression(expr.GetParameter(0), path);
            Expression simplerMultiplier = SimplifyExpression(expr.GetParameter(1), path);
            Expression result = new Expression(OperatorStore.MultOp,
                simplerMultiplicand, simplerMultiplier, expr.BitSize);

            /* Simplify the Expression even further. */
            if (IsConstantExpression(simplerMultiplicand) &&
                IsConstantExpression(simplerMultiplier))
            {
                int newConstant =
                    ConvertToConstant(simplerMultiplicand) * ConvertToConstant(simplerMultiplier);
                result = new Constant(newConstant, expr.BitSize);
            }
            else if (IsConstantExpression(simplerMultiplier) &&
                (simplerMultiplicand.Op.Type == OperatorType.Multiplication))
            {
                /* Distribute the multiplication over multiplication. */
                Expression multiplicandMultiplicand = simplerMultiplicand.GetParameter(0);
                Expression multiplicandMultiplier = simplerMultiplicand.GetParameter(1);
                if (IsConstantExpression(multiplicandMultiplicand))
                {
                    Expression newMultiplicand = new Expression(OperatorStore.MultOp,
                        multiplicandMultiplicand, simplerMultiplier, expr.BitSize);
                    newMultiplicand = SimplifyExpression(newMultiplicand, path);
                    result = new Expression(OperatorStore.MultOp,
                        newMultiplicand, multiplicandMultiplier, expr.BitSize);
                }
                else if (IsConstantExpression(multiplicandMultiplier))
                {
                    Expression newMultiplier = new Expression(OperatorStore.MultOp,
                        multiplicandMultiplier, simplerMultiplier, expr.BitSize);
                    newMultiplier = SimplifyExpression(newMultiplier, path);
                    result = new Expression(OperatorStore.MultOp,
                        multiplicandMultiplicand, newMultiplier, expr.BitSize);
                }
            }
            else if (IsConstantExpression(simplerMultiplicand))
            {
                int multiplicandConstant = ConvertToConstant(simplerMultiplicand);
                if (multiplicandConstant == 0) { result = simplerMultiplicand; }
                else if (multiplicandConstant == 1) { result = simplerMultiplier; }
            }
            else if (IsConstantExpression(simplerMultiplier))
            {
                int multiplierConstant = ConvertToConstant(simplerMultiplier);
                if (multiplierConstant == 0) { result = simplerMultiplier; }
                else if (multiplierConstant == 1) { result = simplerMultiplicand; }
            }

            result.Type = expr.Type;
            return result;
        }

        /// <summary>
        /// Simplifies the input division Expression as much as possible. This method
        /// does not perform exhaustive simplification: it is meant to simplify commonly
        /// occurring types of division Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Division Expression to simplify.</param>
        /// <param name="path">Path that contains the input division Expression.</param>
        /// <returns>Simplified (as much as possible) version of the input division
        /// Expression. The original Expression is not modified.</returns>
        private static Expression SimplifyDivisionExpression(Expression expr, Path path)
        {
            Expression simplerDividend = SimplifyExpression(expr.GetParameter(0), path);
            Expression simplerDivisor = SimplifyExpression(expr.GetParameter(1), path);
            Expression result = new Expression(expr.Op,
                simplerDividend, simplerDivisor, expr.BitSize);

            /* Simplify the Expression even further. */
            if (IsConstantExpression(simplerDividend) && IsConstantExpression(simplerDivisor))
            {
                int dividendConstant = ConvertToConstant(simplerDividend);
                int divisorConstant = ConvertToConstant(simplerDivisor);
                int newConstant = dividendConstant / divisorConstant;
                result = new Constant(newConstant, expr.BitSize);
            }
            else if (IsConstantExpression(simplerDivisor) &&
                simplerDividend.Op.Type == OperatorType.Multiplication)
            {
                /* Check if the second term of the multiplication in the dividend is
                 * divisible by the divisor. This happens, for example, when an Expression is
                 * multiplied by the byte-size of array elements for the purposes of
                 * pointer arithmetic. */
                Expression dividendMultiplier = simplerDividend.GetParameter(1);
                if (IsConstantExpression(dividendMultiplier))
                {
                    int divisorConstant = ConvertToConstant(simplerDivisor);
                    int multiplierConstant = ConvertToConstant(dividendMultiplier);
                    if (multiplierConstant % divisorConstant == 0)
                    {
                        int quotient = multiplierConstant / divisorConstant;
                        Expression quotientExpr = new Constant(quotient, expr.BitSize);
                        result = new Expression(OperatorStore.MultOp,
                            simplerDividend.GetParameter(0), quotientExpr,
                            expr.BitSize);
                        result = SimplifyExpression(result, path);
                    }
                }
            }
            else if (simplerDividend.Op.Type == OperatorType.Addition ||
                simplerDividend.Op.Type == OperatorType.Subtraction)
            {
                /* Distribute the division operation across the terms of the dividend. */
                Expression dividendTerm1 = simplerDividend.GetParameter(0);
                Expression dividendTerm2 = simplerDividend.GetParameter(1);

                Expression newDividendTerm1 = new Expression(expr.Op,
                    dividendTerm1, simplerDivisor, expr.BitSize);
                newDividendTerm1 = SimplifyExpression(newDividendTerm1, path);

                Expression newDividendTerm2 = new Expression(expr.Op,
                    dividendTerm2, simplerDivisor, expr.BitSize);
                newDividendTerm2 = SimplifyExpression(newDividendTerm2, path);

                result = new Expression(simplerDividend.Op,
                    newDividendTerm1, newDividendTerm2, simplerDividend.BitSize);
                result = SimplifyExpression(result, path);
            }
            else if (IsConstantExpression(simplerDividend))
            {
                int dividendConstant = ConvertToConstant(simplerDividend);
                if (dividendConstant == 0) { result = simplerDividend; }
            }
            else if (IsConstantExpression(simplerDivisor))
            {
                int divisorConstant = ConvertToConstant(simplerDivisor);
                if (divisorConstant == 1) { result = simplerDividend; }
            }

            result.Type = expr.Type;
            return result;
        }

        /// <summary>
        /// Simplifies the input remainder Expression as much as possible. This method
        /// does not perform exhaustive simplification: it is meant to simplify commonly
        /// occurring types of remainder Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Remainder Expression to simplify.</param>
        /// <param name="path">Path that contains the input remainder Expression.</param>
        /// <returns>Simplified (as much as possible) version of the input remainder
        /// Expression. The original Expression is not modified.</returns>
        private static Expression SimplifyRemainderExpression(Expression expr, Path path)
        {
            Expression simplerDividend = SimplifyExpression(expr.GetParameter(0), path);
            Expression simplerDivisor = SimplifyExpression(expr.GetParameter(1), path);
            Expression result = new Expression(OperatorStore.RemOp,
                simplerDividend, simplerDivisor, expr.BitSize);

            /* Simplify the Expression even further. */
            if (IsConstantExpression(simplerDividend) && IsConstantExpression(simplerDivisor))
            {
                int dividendConstant = ConvertToConstant(simplerDividend);
                int divisorConstant = ConvertToConstant(simplerDivisor);
                int remainder = dividendConstant % divisorConstant;
                result = new Constant(remainder, expr.BitSize);
            }
            else if (IsConstantExpression(simplerDivisor) &&
                simplerDividend.Op.Type == OperatorType.Multiplication)
            {
                /* Check if the second term of the multiplication in the dividend is
                 * divisible by the divisor. This happens, for example, when a variable is
                 * multiplied by the byte-size of array elements for the purposes of
                 * pointer arithmetic. */
                Expression dividendMultiplier = simplerDividend.GetParameter(1);
                if (IsConstantExpression(dividendMultiplier))
                {
                    int divisorConstant = ConvertToConstant(simplerDivisor);
                    int multiplierConstant = ConvertToConstant(dividendMultiplier);
                    if (multiplierConstant % divisorConstant == 0)
                    {
                        result = new Constant(0, expr.BitSize);
                    }
                }
            }
            else if (IsConstantExpression(simplerDividend))
            {
                int dividendConstant = ConvertToConstant(simplerDividend);
                if (dividendConstant == 0)  { result = simplerDividend; }
            }
            else if (IsConstantExpression(simplerDivisor))
            {
                int divisorConstant = ConvertToConstant(simplerDivisor);
                if (divisorConstant == 1) { result = new Constant(0, expr.BitSize); }
            }

            result.Type = expr.Type;
            return result;
        }

        /// <summary>
        /// Simplifies the input equality Expression as much as possible. This method
        /// does not perform exhaustive simplification: it is meant to simplify commonly
        /// occurring types of equality Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Equality Expression to simplify.</param>
        /// <param name="path">Path that contains the input equality Expression.</param>
        /// <returns>Simplified (as much as possible) version of the input equality
        /// Expression. The original Expression is not modified.</returns>
        private static Expression SimplifyEqualityExpression(Expression expr, Path path)
        {
            Expression simplerLhs = SimplifyExpression(expr.GetParameter(0), path);
            Expression simplerRhs = SimplifyExpression(expr.GetParameter(1), path);
            Expression result = new Expression(OperatorStore.EqualOp,
                simplerLhs, simplerRhs, expr.BitSize);

            /* Simplify the Expression even further. */
            if (IsConstantExpression(simplerLhs) && IsConstantExpression(simplerRhs))
            {
                int lhsConstant = ConvertToConstant(simplerLhs);
                int rhsConstant = ConvertToConstant(simplerRhs);
                result = (lhsConstant == rhsConstant) ?
                    new Expression(OperatorStore.TrueOp, expr.BitSize) :
                    new Expression(OperatorStore.FalseOp, expr.BitSize);
            }

            result.Type = expr.Type;
            return result;
        }

        /// <summary>
        /// Simplifies the input if-then-else Expression as much as possible. This method
        /// does not perform exhaustive simplification: it is meant to simplify commonly
        /// occurring types of if-then-else Expressions.
        /// </summary>
        /// 
        /// <param name="expr">If-then-else Expression to simplify.</param>
        /// <param name="path">Path that contains the input if-then-else Expression.</param>
        /// <returns>Simplified (as much as possible) version of the input if-then-else
        /// Expression. The original Expression is not modified.</returns>
        private static Expression SimplifyIteExpression(Expression expr, Path path)
        {
            Expression simplerCondition = SimplifyExpression(expr.GetParameter(0), path);
            Expression simplerConsequent = SimplifyExpression(expr.GetParameter(1), path);
            Expression simplerAlternative = SimplifyExpression(expr.GetParameter(2), path);

            List<Expression> iteParamList = new List<Expression>();
            iteParamList.Add(simplerCondition);
            iteParamList.Add(simplerConsequent);
            iteParamList.Add(simplerAlternative);
            Expression result = new Expression(OperatorStore.IteOp, iteParamList, expr.BitSize);

            /* Simplify the Expression even further. */
            result = (simplerCondition.Op.Type == OperatorType.True) ? simplerConsequent :
                (simplerCondition.Op.Type == OperatorType.False) ? simplerAlternative : result;
            result.Type = expr.Type;
            return result;
        }

        /// <summary>
        /// Updates the sub-Expressions in the input Expression to account for the assignments
        /// to variables made in the corresponding basic block. The input Expression is present
        /// along the input path.
        /// </summary>
        /// 
        /// <param name="expr">Expression to update.</param>
        /// <param name="basicBlockAddendum">BasicBlockAddendum that contains
        /// the Expression.</param>
        /// <param name="updateIndices">True if only the indices in an array access Expression
        /// need to be updated; false otherwise.</param>
        /// <param name="path">Path along which the input Expression is present.</param>
        /// <returns>New Expression that accounts for assignments made to variables in
        /// the original Expression. The original Expression is not modified.</returns>
        public static Expression UpdateExpression(Expression expr,
            BasicBlockAddendum basicBlockAddendum, bool updateIndices, Path path)
        {
            OperatorType exprOpType = expr.Op.Type;

            /* Base case: Zero-arity operators. */
            if (expr.Op.Arity == OperatorArity.Nil &&
                exprOpType != OperatorType.Variable &&
                exprOpType != OperatorType.ArrayVariable) { return expr; }

            Utilities.Configuration config = path.Config;
            Expression result = null;
            switch (exprOpType)
            {
                /* Base case: Expression is a variable Expression. */
                case OperatorType.Variable:
                    result = basicBlockAddendum.GetUpdatedVersion(expr, path);
                    break;

                /* Base case: Expression is an array variable Expression. The Expression
                 * is not updated if only indices need to be updated. */
                case OperatorType.ArrayVariable:
                    result = updateIndices ? expr :
                        basicBlockAddendum.GetUpdatedVersion(expr, path);
                    break;

                /* Recursive case: Convert the indices of an array access Expression,
                 * and the array variable Expression itself, if needed. */
                case OperatorType.Array:
                    Expression baseExpr = expr.GetParameter(0);
                    Expression updatedBaseExpr = UpdateExpression(baseExpr,
                        basicBlockAddendum, updateIndices, path);

                    Expression indexExpr = expr.GetParameter(1);
                    Expression updatedIndexExpr = UpdateExpression(indexExpr,
                        basicBlockAddendum, false, path);

                    result = new Expression(OperatorStore.ArrayOp,
                        updatedBaseExpr, updatedIndexExpr, expr.BitSize);
                    result.Type = expr.Type;
                    break;

                /* Recursive case: Convert the parameters of the Expression. */
                default:
                    List<Expression> newParamList = new List<Expression>();
                    foreach (Expression paramExpr in expr.ParameterList)
                    {
                        Expression convertedParamExpr = UpdateExpression(paramExpr,
                            basicBlockAddendum, updateIndices, path);
                        newParamList.Add(convertedParamExpr);
                    }

                    result = new Expression(expr.Op, newParamList, expr.BitSize);
                    result.Type = expr.Type;
                    break;
            }

            Trace.Assert(result != null, "PHOENIX: Result should not be null.");
            return result;
        }

        #endregion

        #region Miscellaneous Expression Methods

        /// <summary>
        /// Returns a pair whose elements are (in order) the augend Expression and the addend
        /// Expression of an addition Expression that evaluates to the input Expression.
        /// </summary>
        /// 
        /// <param name="expr">Expression that is the (possibly simplified) sum of the augend
        /// and the addend Expressions that this method will return.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Pair of Expressions as described.</returns>
        public static Pair<Expression, Expression> GetAugendAndAddend(Expression expr, Path path)
        {
            Expression augendExpr = null, addendExpr = null;
            if (expr.Op.Type == OperatorType.Addition)
            {
                augendExpr = expr.GetParameter(0);
                addendExpr = expr.GetParameter(1);
            }
            else if (expr.Op.Type == OperatorType.Subtraction)
            {
                augendExpr = expr.GetParameter(0);
                addendExpr = new Expression(OperatorStore.NegateOp,
                    expr.GetParameter(1), expr.GetParameter(1).BitSize);
            }
            else
            {
                augendExpr = expr;
                addendExpr = new Constant(0, path.Config.WORD_BITSIZE);
            }
            return new Pair<Expression, Expression>(augendExpr, addendExpr);
        }

        #endregion

        #region Condition Augmentation Methods

        /// <summary>
        /// Returns a list of all the sub-Expressions in the input Expression that must not
        /// be zero. This includes divisors of division Expressions and moduli of remainder
        /// Expressions.
        /// </summary>
        /// 
        /// <param name="expr">Expression to search.</param>
        /// <returns>List of Expressions that must not be zero.</returns>
        private static List<Expression> FindNotEqualToZero(Expression expr)
        {
            List<Expression> result = new List<Expression>();

            switch (expr.Op.Type)
            {
                /* Base case: Expression is an arithmetic division or remainder. */
                case OperatorType.SDivision:
                case OperatorType.UDivision:
                case OperatorType.Remainder:
                    result.Add(expr.GetParameter(1));
                    break;

                /* Recursive case: Recurse into the parameters of the Expression. */
                default:
                    foreach (Expression param in expr.ParameterList)
                    {
                        result.AddRange(FindNotEqualToZero(param));
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// Generates a list of condition Expressions using the input Expression to ensure
        /// that the divisor of a division Expression, or that the modulus of a remainder
        /// Expression, is not set to zero by the SMT solver.
        /// </summary>
        /// 
        /// <param name="expr">Expression to augment.</param>
        /// <returns>List of conditions that state that certain Expressions should not be
        /// equal to zero.</returns>
        public static List<Expression> GenerateNotEqualToZeroConditions(Expression expr)
        {
            List<Expression> notEqualToZeroList = FindNotEqualToZero(expr);
            List<Expression> result = new List<Expression>();

            foreach (Expression exprToProtect in notEqualToZeroList)
            {
                Expression zeroExpr = new Constant(0, exprToProtect.BitSize);
                Expression newCondition =
                    new Expression(OperatorStore.NotEqualOp, exprToProtect, zeroExpr);
                result.Add(newCondition);
            }

            return result;
        }

        #endregion
    }
}
