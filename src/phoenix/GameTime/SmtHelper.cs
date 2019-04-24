/*************************************************************************************************
 * Operator                                                                                      *
 * ============================================================================================= *
 * This file contains the classes that defines various helper functions                          *
 * to interact with SMT solvers.                                                                 *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Utilities;


namespace GameTime
{
    /// <summary>
    /// This class defines various helper functions to interact with SMT solvers.
    /// </summary>
    public class SmtHelper
    {
        /// <summary>
        /// Possible types of an Expression. This is useful when we need to
        /// "cast" an expression to the proper type.
        /// </summary>
        private enum ExpressionType
        {
            /// <summary>
            /// For non-Boolean Expressions.
            /// </summary>
            DEFAULT,
            
            /// <summary>
            /// For Boolean Expressions.
            /// </summary>
            BOOLEAN
        };

        /// <summary>
        /// Array of operator types that correspond to comparison operators.
        /// </summary>
        private static OperatorType[] comparisonOpTypes = new OperatorType[]
        {
            OperatorType.Equal,
            OperatorType.NotEqual,

            OperatorType.LessThan,
            OperatorType.FLessThan,
            OperatorType.ULessThan,

            OperatorType.LessThanEqual,
            OperatorType.FLessThanEqual,
            OperatorType.ULessThanEqual,

            OperatorType.GreaterThan,
            OperatorType.FGreaterThan,
            OperatorType.UGreaterThan,

            OperatorType.GreaterThanEqual,
            OperatorType.FGreaterThanEqual,
            OperatorType.UGreaterThanEqual
        };

        /// <summary>
        /// Returns a SMT-LIB v2.0-compliant string that corresponds to the input constant.
        /// </summary>
        /// 
        /// <param name="value">String that represents a constant integer value.</param>
        /// <param name="bitSize">Bit-size of the resulting bitvector.</param>
        /// <returns>SMT-LIB v2.0-compliant string that corresponds to
        /// the input constant.</returns>
        private static string MakeConstantBv(string value, uint bitSize)
        {
            return value.StartsWith("-") ?
                "(bvneg " + "(_ bv" + value.Substring(1) + " " + bitSize.ToString() + "))" :
                "(_ bv" + value + " " + bitSize.ToString() + ")";
        }

        /// <summary>
        /// Returns a SMT-LIB v2.0-compliant string that declares
        /// the input variable name as a bitvector.
        /// </summary>
        /// 
        /// <param name="name">Name of the input variable.</param>
        /// <param name="bitSize">Bit-size of the resulting bitvector.</param>
        /// <returns>SMT-LIB v2.0-compliant string that declares
        /// the input variable as a bitvector.</returns>
        public static string MakeVariableBv(string name, uint bitSize)
        {
            return "(declare-fun " + name + " () " + MakeBvSort(bitSize) + ")";
        }

        /// <summary>
        /// Returns a SMT-LIB v2.0-compliant string that corresponds
        /// to the sort of a bitvector with the input bit-size.
        /// </summary>
        /// 
        /// <param name="bitSize">Bit-size of a bitvector.</param>
        /// <returns>SMT-LIB v2.0-compliant string that corresponds
        /// to the sort of a bitvector with the input bit-size.</returns>
        private static string MakeBvSort(uint bitSize)
        {
            return "(_ BitVec " + bitSize.ToString() + ")";
        }

        /// <summary>
        /// Returns a SMT-LIB v2.0-compliant string that corresponds
        /// to the sort of a bitvector with the input bit-size.
        /// </summary>
        /// 
        /// <param name="bitSize">Bit-size of a bitvector.</param>
        /// <returns>SMT-LIB v2.0-compliant string that corresponds
        /// to the sort of a bitvector with the input bit-size.</returns>
        private static string MakeBvSort(long bitSize)
        {
            return "(_ BitVec " + bitSize.ToString() + ")";
        }

        /// <summary>
        /// Returns a SMT-LIB v2.0-compliant string that declares the input array variable name
        /// as an array of bitvectors with bitvector indices.
        /// </summary>
        /// 
        /// <param name="name">Array variable name.</param>
        /// <param name="dimensions">Dimensions of the array with the input name.</param>
        /// <param name="path">Path that contains the array with the input name.</param>
        /// <returns>SMT-LIB v2.0-compliant string that declares the input array variable name
        /// as an array of bitvectors with bitvector indices.</returns>
        private static string MakeArrayVarBv(string name, List<uint> dimensions, Path path)
        {
            
            return "(declare-fun " + name + " () " +
                SmtHelper.MakeArraySort(dimensions, path) + ")";
        }

        /// <summary>
        /// Returns a SMT-LIB v2.0-compliant string that corresponds to the sort of an array of
        /// bitvectors with bitvector indices, having the input dimensions.
        /// </summary>
        /// 
        /// <param name="dimensions">Dimensions of an array of bitvectors with
        /// bitvector indices.</param>
        /// <param name="path">Path that contains the input array
        /// whose dimensions are provided.</param>
        /// <returns>SMT-LIB v2.0-compliant string that corresponds to the sort of an array of
        /// bitvectors with bitvector indices, having the input dimensions.</returns>
        private static string MakeArraySort(List<uint> dimensions, Path path)
        {
            string result = "(Array";

            if (!path.ProjectConfig.MODEL_AS_NESTED_ARRAYS)
            {
                long sumDomainDims = dimensions.GetRange(0, dimensions.Count - 1).Sum(dim => dim);
                result += " " + SmtHelper.MakeBvSort(sumDomainDims) + " ";
                result += SmtHelper.MakeBvSort(dimensions[dimensions.Count - 1]) + ")";
                return result;
            }

            if (dimensions.Count == 1) { return SmtHelper.MakeBvSort(dimensions[0]); }

            result += " " + SmtHelper.MakeBvSort(dimensions[0]) + " ";
            result += SmtHelper.MakeArraySort(dimensions.GetRange(1, dimensions.Count - 1), path);
            result = result.TrimEnd() + ")";
            return result;
        }

        /// <summary>
        /// Returns a SMT-LIB v2.0-compliant string that corresponds to
        /// the input Boolean Expression.
        /// </summary>
        /// 
        /// <param name="expr">Boolean Expression.</param>
        /// <returns>SMT-LIB v2.0-compliant string that corresponds to
        /// the input Boolean Expression.</returns>
        private static string MakeBooleanExpr(Expression expr)
        {
            string result = null;
            OperatorType exprOpType = expr.Op.Type;
            switch (exprOpType)
            {
                case OperatorType.And:
                case OperatorType.Or:
                    Expression firstExpr = expr.GetParameter(0);
                    Expression secondExpr = expr.GetParameter(1);

                    Pair<string, ExpressionType> convertedFirstExpr =
                        SmtHelper.ConvertToSmtLib2(firstExpr);
                    Pair<string, ExpressionType> convertedSecondExpr =
                        SmtHelper.ConvertToSmtLib2(secondExpr);
                    result = "(" + ((exprOpType == OperatorType.And) ? "and" : "or") +
                        " " + convertedFirstExpr.First + " " + convertedSecondExpr.First + ")";
                    break;

                case OperatorType.Not:
                    Expression toNotExpr = expr.GetParameter(0);
                    Pair<string, ExpressionType> convertToNotExpr =
                        SmtHelper.ConvertToSmtLib2(toNotExpr);
                    result = "(not " + convertToNotExpr.First + ")";
                    break;
            }

            Trace.Assert(result != null,
                "PHOENIX: Could not convert Boolean Expression: " + expr.ToString());
            return result;
        }

        /// <summary>
        /// Converts a Boolean Expression into a logically equivalent ITE statement that returns
        /// a bitvector instead. If the Expression is not Boolean, it is returned unchanged.
        /// </summary>
        /// 
        /// <param name="exprString">SMT-LIB v2.0-compliant string that represents
        /// the Expression to convert.</param>
        /// <param name="exprType">Type of the Expression.</param>
        /// <param name="bitSize">Bit-size of the resulting bitvector.</param>
        /// <returns>Logically equivalent ITE statement if the Expression is Boolean;
        /// unchanged input Expression otherwise.</returns>
        private static string BooleanToIte(string exprString,
            ExpressionType exprType, uint bitSize)
        {
            string result = null;
            switch (exprType)
            {
                case ExpressionType.DEFAULT:
                    result = exprString;
                    break;

                case ExpressionType.BOOLEAN:
                    string zeroBv = SmtHelper.MakeConstantBv("0", bitSize);
                    string oneBv = SmtHelper.MakeConstantBv("1", bitSize);
                    result = "(ite " + exprString + " " + oneBv + " " + zeroBv + ")";
                    break;

                default:
                    Trace.Fail("PHOENIX: Unrecognized Expression type: " + exprType.ToString());
                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns the string representation of the function appropriate for the input
        /// operator type. The corresponding operator takes only one argument.
        /// </summary>
        /// 
        /// <param name="opType">Operator type.</param>
        /// <returns>String representation of the function appropriate for the input operator type.
        /// The corresponding operator takes only one argument.</returns>
        private static string GetFunctionForUnary(OperatorType opType)
        {
            switch (opType)
            {
                case OperatorType.Negate:
                    return "bvneg";
                case OperatorType.BitComplement:
                    return "bvnot";
                default:
                    throw new NotImplementedException("PHOENIX: " +
                        "Operator type not implemented: " + opType.ToString());
            }
        }

        /// <summary>
        /// Returns the string representation of the function appropriate for the input
        /// operator type. The corresponding operator takes two arguments.
        /// </summary>
        /// 
        /// <param name="opType">Operator type.</param>
        /// <returns>String representation of the function appropriate for the input operator type.
        /// The corresponding operator takes two arguments.</returns>
        private static string GetFunctionForBinary(OperatorType opType)
        {
            switch (opType)
            {
                case OperatorType.Addition:
                    return "bvadd";
                case OperatorType.Subtraction:
                    return "bvsub";
                case OperatorType.Multiplication:
                    return "bvmul";
                case OperatorType.SDivision:
                    return "bvsdiv";
                case OperatorType.UDivision:
                    return "bvudiv";
                case OperatorType.Remainder:
                    return "bvsmod";

                case OperatorType.Equal:
                    return "=";

                case OperatorType.LessThan:
                case OperatorType.FLessThan:
                    return "bvslt";
                case OperatorType.ULessThan:
                    return "bvult";

                case OperatorType.LessThanEqual:
                case OperatorType.FLessThanEqual:
                    return "bvsle";
                case OperatorType.ULessThanEqual:
                    return "bvule";

                case OperatorType.GreaterThan:
                case OperatorType.FGreaterThan:
                    return "bvsgt";
                case OperatorType.UGreaterThan:
                    return "bvugt";

                case OperatorType.GreaterThanEqual:
                case OperatorType.FGreaterThanEqual:
                    return "bvsge";
                case OperatorType.UGreaterThanEqual:
                    return "bvuge";

                case OperatorType.BitAnd:
                    return "bvand";
                case OperatorType.BitOr:
                    return "bvor";
                case OperatorType.BitXor:
                    return "bvxor";

                case OperatorType.ShiftLeft:
                    return "bvshl";
                case OperatorType.AShiftRight:
                    return "bvashr";
                case OperatorType.LShiftRight:
                    return "bvlshr";

                case OperatorType.Concatenate:
                    return "concat";

                case OperatorType.Implies:
                    return "=>";
                case OperatorType.Iff:
                    return "=";

                case OperatorType.Select:
                    return "select";

                default:
                    throw new NotImplementedException("PHOENIX: " +
                        "Operator type not implemented: " + opType.ToString());
            }
        }

        /// <summary>
        /// Returns true if the input operator type corresponds to a comparison operator.
        /// </summary>
        /// 
        /// <param name="opType">Operator type.</param>
        /// <returns>True if the input operator type corresponds to a comparison operator;
        /// false otherwise.</returns>
        private static bool IsComparisonOp(OperatorType opType)
        {
            return
                SmtHelper.comparisonOpTypes.Any(comparisonOpType => (opType == comparisonOpType));
        }

        /// <summary>
        /// Returns the string representation of the function appropriate for the input
        /// operator type. The corresponding operator takes three arguments.
        /// </summary>
        /// 
        /// <param name="opType">Operator type.</param>
        /// <returns>String representation of the function appropriate for the input operator type.
        /// The corresponding operator takes three arguments.</returns>
        private static string GetFunctionForTernary(OperatorType opType)
        {
            switch (opType)
            {
                case OperatorType.Store:
                    return "store";
                default:
                    throw new NotImplementedException("PHOENIX: " +
                        "Operator type not implemented: " + opType.ToString());
            }
        }

        /// <summary>
        /// Returns a pair whose first element is a string that complies with the SMT-LIB v2.0
        /// standard and that corresponds to the input Expression, and whose second element
        /// is the type of the Expression.
        /// </summary>
        /// 
        /// <param name="expr">Expression to convert.</param>
        /// <returns>Pair whose first element is a string that complies with the SMT-LIB v2.0
        /// standard and that corresponds to the input Expression, and whose second element
        /// is the type of the Expression.</returns>
        private static Pair<string, ExpressionType> ConvertToSmtLib2(Expression expr)
        {
            Pair<string, ExpressionType> result = null;
            string resultString = "";

            OperatorType exprOpType = expr.Op.Type;
            switch (exprOpType)
            {
                case OperatorType.True:
                    result = new Pair<string, ExpressionType>("true", ExpressionType.BOOLEAN);
                    break;
                case OperatorType.False:
                    result = new Pair<string, ExpressionType>("false", ExpressionType.BOOLEAN);
                    break;
                case OperatorType.Constant:
                    resultString = SmtHelper.MakeConstantBv(expr.Value, expr.BitSize);
                    result = new Pair<string, ExpressionType>(resultString, ExpressionType.DEFAULT);
                    break;
                case OperatorType.Variable:
                case OperatorType.ArrayVariable:
                    result = new Pair<string, ExpressionType>(expr.Value, ExpressionType.DEFAULT);
                    break;

                case OperatorType.ZeroExtend:
                case OperatorType.SignExtend:
                    Expression toExtendExpr = expr.GetParameter(0);
                    Expression extendByExpr = expr.GetParameter(1);

                    Pair<string, ExpressionType> convertedToExtendExpr =
                        SmtHelper.ConvertToSmtLib2(toExtendExpr);
                    resultString = "((_ " +
                        (exprOpType == OperatorType.ZeroExtend ? "zero_extend" : "sign_extend") +
                        " " + extendByExpr.Value + ") ";
                    resultString += SmtHelper.BooleanToIte(convertedToExtendExpr.First,
                        convertedToExtendExpr.Second, toExtendExpr.BitSize) + ")";
                    result =
                        new Pair<string, ExpressionType>(resultString, ExpressionType.DEFAULT);
                    break;

                case OperatorType.BitExtract:
                    Expression toExtractExpr = expr.GetParameter(0);
                    Expression lowIndexExpr = expr.GetParameter(1);
                    Expression highIndexExpr = expr.GetParameter(2);

                    Pair<string, ExpressionType> convertedToExtractExpr =
                        SmtHelper.ConvertToSmtLib2(toExtractExpr);
                    resultString = "((_ extract " + highIndexExpr.Value + " " +
                        lowIndexExpr.Value + ") ";
                    resultString += SmtHelper.BooleanToIte(convertedToExtractExpr.First,
                        convertedToExtractExpr.Second, toExtractExpr.BitSize) + ")";
                    result =
                        new Pair<string, ExpressionType>(resultString, ExpressionType.DEFAULT);
                    break;

                case OperatorType.Ite:
                    Expression conditionalExpr = expr.GetParameter(0);
                    Expression consequentExpr = expr.GetParameter(1);
                    Expression alternateExpr = expr.GetParameter(2);

                    Pair<string, ExpressionType> convertedConditionalExpr =
                        SmtHelper.ConvertToSmtLib2(conditionalExpr);
                    Pair<string, ExpressionType> convertedConsequentExpr =
                        SmtHelper.ConvertToSmtLib2(consequentExpr);
                    Pair<string, ExpressionType> convertedAlternateExpr =
                        SmtHelper.ConvertToSmtLib2(alternateExpr);

                    resultString = "(ite " + convertedConditionalExpr.First + " ";
                    resultString += SmtHelper.BooleanToIte(convertedConsequentExpr.First,
                        convertedConsequentExpr.Second, consequentExpr.BitSize) + " ";
                    resultString += SmtHelper.BooleanToIte(convertedAlternateExpr.First,
                        convertedAlternateExpr.Second, alternateExpr.BitSize) + ")";
                    result =
                        new Pair<string, ExpressionType>(resultString, ExpressionType.DEFAULT);
                    break;

                case OperatorType.And:
                case OperatorType.Or:
                case OperatorType.Not:
                    resultString = SmtHelper.MakeBooleanExpr(expr);
                    result =
                        new Pair<string, ExpressionType>(resultString, ExpressionType.BOOLEAN);
                    break;

                case OperatorType.NotEqual:
                    Expression firstParam = expr.GetParameter(0);
                    Expression secondParam = expr.GetParameter(1);

                    Pair<string, ExpressionType> convertedFirstParam =
                        SmtHelper.ConvertToSmtLib2(firstParam);
                    Pair<string, ExpressionType> convertedSecondParam =
                        SmtHelper.ConvertToSmtLib2(secondParam);

                    resultString = "(not (= ";
                    resultString += SmtHelper.BooleanToIte(convertedFirstParam.First,
                        convertedFirstParam.Second, firstParam.BitSize) + " ";
                    resultString += SmtHelper.BooleanToIte(convertedSecondParam.First,
                        convertedSecondParam.Second, secondParam.BitSize) + "))";
                    result =
                        new Pair<string, ExpressionType>(resultString, ExpressionType.BOOLEAN);
                    break;

                default:
                    string convertedParams = "";
                    foreach (Expression paramExpr in expr.ParameterList)
                    {
                        Pair<string, ExpressionType> convertedParam =
                            SmtHelper.ConvertToSmtLib2(paramExpr);
                        convertedParams += " " + SmtHelper.BooleanToIte(convertedParam.First,
                            convertedParam.Second, paramExpr.BitSize);
                    }

                    OperatorType opType = expr.Op.Type;
                    ExpressionType resultType = SmtHelper.IsComparisonOp(opType) ?
                        ExpressionType.BOOLEAN : ExpressionType.DEFAULT;
                    string functionForOp = "";

                    int numParams = expr.ParameterList.Count;
                    switch (numParams)
                    {
                        case 1:
                            functionForOp = SmtHelper.GetFunctionForUnary(opType);
                            break;
                        case 2:
                            functionForOp = SmtHelper.GetFunctionForBinary(opType);
                            break;
                        case 3:
                            functionForOp = SmtHelper.GetFunctionForTernary(opType);
                            break;
                        default:
                            throw new NotImplementedException("PHOENIX: " +
                                "No support for operators with arity " +
                                numParams.ToString() + ".");
                    }

                    resultString = "(" + functionForOp + convertedParams + ")";
                    result = new Pair<string, ExpressionType>(resultString, resultType);
                    break;
            }

            Trace.Assert(result != null, "PHOENIX: Result should not be null.");
            return result;
        }

        /// <summary>
        /// Returns a string that complies with the SMT-LIB v2.0 standard and that corresponds
        /// to a query that checks the feasibility of the conditions along the input Path.
        /// </summary>
        /// 
        /// <param name="path">Path to convert.</param>
        /// <returns>String that complies with the SMT-LIB v2.0 standard and that corresponds to
        /// a query that checks the feasibility of the conditions along the input Path.</returns>
        public static string ConvertToSmtLib2Query(Path path)
        {
            string result = "";

            /* Add the command to initialize the solver with the QF_AUFBV logic. */
            result += "(set-logic QF_AUFBV)" + "\n";

            /* Add declarations for the Boolean constraint variables. */
            string identConstraint = path.Config.IDENT_CONSTRAINT;
            uint constraintNum;
            for (constraintNum = 0; constraintNum < path.Conditions.Count; constraintNum++)
            {
                result += "(declare-fun ";
                result += identConstraint + constraintNum.ToString() + " ";
                result += "() Bool)" + "\n";
            }
            
            /* Add declarations for the variables. */
            foreach (Expression varExpr in path.Variables)
            {
                result += MakeVariableBv(varExpr.Value, varExpr.BitSize) + "\n";
            }

            /* Add declarations for the array variables. */
            foreach (Expression arrayVarExpr in path.ArrayVariables)
            {
                result += MakeArrayVarBv(arrayVarExpr.Value,
                    path.ArrayDimensions[arrayVarExpr], path) + "\n";
            }

            /* Assert the conditions along the input Path. */
            constraintNum = 0;
            foreach (Pair<Expression, uint> conditionPair in path.Conditions)
            {
                Pair<string, ExpressionType> convertedCondition =
                    SmtHelper.ConvertToSmtLib2(conditionPair.First);
                result += "(assert (= ";
                result += identConstraint + constraintNum.ToString() + " ";
                result += convertedCondition.First + "))" + "\n";
                constraintNum++;
            }

            /* Assert that the Boolean constraint variables are all true. */
            result += "(assert (and";
            for (constraintNum = 0; constraintNum < path.Conditions.Count; constraintNum++)
            {
                result += " " + identConstraint + constraintNum.ToString();
            }
            result += "))" + "\n";

            /* Finish the query. */
            result += "(check-sat)" + "\n";
            result += "(exit)" + "\n";
            return result;
        }
    }
}
