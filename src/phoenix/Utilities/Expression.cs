/*************************************************************************************************
 * Expression                                                                                    *
 * ============================================================================================= *
 * This file contains the class that describes the expressions that can either be                *
 * the conditions to satisfy along a path, or the assignments along a path.                      *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Utilities
{
    /// <summary>
    /// This class describes the expressions that can either be
    /// the conditions to satisfy along a path, or the assignments along a path.
    /// </summary>
    public class Expression
    {
        #region Fields and Properties

        /// <summary>
        /// Operator of this Expression.
        /// </summary>
        private Operator op;

        /// <summary>
        /// Get the operator of this Expression.
        /// </summary>
        public Operator Op
        {
            get { return this.op; }
            set { this.op = value; }
        }

        /// <summary>
        /// Bit-size of this Expression.
        /// </summary>
        private uint bitSize;

        /// <summary>
        /// Get or set the bit-size of this Expression.
        /// </summary>
        public uint BitSize
        {
            get { return this.bitSize; }
            set { this.bitSize = value; }
        }

        /// <summary>
        /// List of Expressions for all the parameters of this Expression.
        /// </summary>
        private List<Expression> parameterList;

        /// <summary>
        /// Get the list of Expressions for all the parameters of this Expression.
        /// </summary>
        public List<Expression> ParameterList
        {
            get { return this.parameterList; }
        }

        /// <summary>
        /// Value of this Expression.
        /// </summary>
        private string value;

        /// <summary>
        /// Get and set the value of this Expression.
        /// </summary>
        public string Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        /// <summary>
        /// Phoenix type associated with this Expression.
        /// </summary>
        private Phx.Types.Type type;

        /// <summary>
        /// Get or set the Phoenix type associated with this Expression.
        /// </summary>
        public Phx.Types.Type Type
        {
            get { return this.type; }
            set { this.type = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for an empty expression. This type of expression is used either
        /// for expressions that should be ignored, or for no-operation expressions,
        /// or for expressions that have no value.
        /// </summary>
        /// 
        /// <param name="op">Operator of the expression.</param>
        /// <param name="bitSize">Bit-size of the expression.</param>
        public Expression(Operator op, uint bitSize)
        {
            this.op = op;
            this.bitSize = bitSize;
            this.parameterList = new List<Expression>();
            this.value = this.ComputeValue();
        }

        /// <summary>
        /// Constructor for an empty expression. This constructor assumes a bit-size of zero.
        /// <seealso cref="Expression(op, bitSize)"/>
        /// </summary>
        /// 
        /// <param name="op">Operator of the expression.</param>
        public Expression(Operator op) : this(op, 0) { }

        /// <summary>
        /// Constructor for expression with zero arity operators.
        /// </summary>
        /// 
        /// <param name="op">Operator of the expression.</param>
        /// <param name="value">Value of the expression.</param>
        /// <param name="bitSize">Bit-size of the expression.</param>
        public Expression(Operator op, string value, uint bitSize)
        {
            this.op = op;
            this.bitSize = bitSize;
            this.parameterList = new List<Expression>();
            this.value = value;
        }

        /// <summary>
        /// Constructor for expression with zero arity operators.
        /// This constructor assumes a bit-size of zero.
        /// <seealso cref="Expression(op, value, bitSize)"/>
        /// </summary>
        /// 
        /// <param name="op">Operator of the expression.</param>
        /// <param name="value">Value of the expression.</param>
        public Expression(Operator op, string value) : this(op, value, 0) { }

        /// <summary>
        /// Constructor for expressions with unary operators.
        /// </summary>
        /// 
        /// <param name="op">Unary operator of the expression.</param>
        /// <param name="parameter">Expression for the operand of the unary operator.</param>
        /// <param name="bitSize">Bit-size of the expression.</param>
        public Expression(Operator op, Expression parameter, uint bitSize)
        {
            this.op = op;
            this.bitSize = bitSize;

            this.parameterList = new List<Expression>(1);
            this.parameterList.Add(parameter);

            this.value = this.ComputeValue();
        }

        /// <summary>
        /// Constructor for expressions with unary operators.
        /// This constructor assumes a bit-size of zero.
        /// <seealso cref="Expression(op, parameter, bitSize)"/>
        /// </summary>
        /// 
        /// <param name="op">Unary operator of the expression.</param>
        /// <param name="parameter">Expression for the operand of the unary operator.</param>
        public Expression(Operator op, Expression parameter) : this(op, parameter, 0) { }

        /// <summary>
        /// Constructor for expressions with binary operators.
        /// </summary>
        /// 
        /// <param name="op">Binary operator of the expression.</param>
        /// <param name="param1">Expression for the first operand of the binary operator.</param>
        /// <param name="param2">Expression for the second operand of the binary operator.</param>
        /// <param name="bitSize">Bit-size of the expression.</param>
        public Expression(Operator op, Expression param1, Expression param2, uint bitSize)
        {
            this.op = op;
            this.bitSize = bitSize;

            this.parameterList = new List<Expression>(2);
            this.parameterList.Add(param1);
            this.parameterList.Add(param2);

            this.value = this.ComputeValue();
        }

        /// <summary>
        /// Constructor for expressions with binary operators.
        /// This constructor assumes a bit-size of zero.
        /// <seealso cref="Expression(op, param1, param2, bitSize)"/>
        /// </summary>
        /// 
        /// <param name="op">Binary operator of the expression.</param>
        /// <param name="param1">Expression for the first operand of the binary operator.</param>
        /// <param name="param2">Expression for the second operand of the binary operator.</param>
        public Expression(Operator op, Expression param1, Expression param2) :
            this(op, param1, param2, 0) { }

        /// <summary>
        /// General constructor for expressions with operators that have any arity.
        /// </summary>
        /// 
        /// <param name="op">Operator of the expression.</param>
        /// <param name="parameterList">List of expressions for the operands
        /// of the operator.</param>
        /// <param name="bitSize">Bit-size of the expression.</param>
        public Expression(Operator op, List<Expression> parameterList, uint bitSize)
        {
            switch (op.Arity)
            {
                case OperatorArity.Nil:
                    throw new ArgumentException("PHOENIX: " +
                        "The constructor for general expressions should not be " +
                        "used for expressions with zero arity.");

                case OperatorArity.Unary:
                case OperatorArity.Binary:
                case OperatorArity.Ternary:
                case OperatorArity.Polynary:
                    this.op = op;
                    this.bitSize = bitSize;
                    this.parameterList = parameterList;
                    this.value = this.ComputeValue();
                    break;

                default:
                    throw new NotImplementedException("PHOENIX: " +
                        "Expressions with arity " +
                        op.Arity.ToString() + " not implemented.");
            }
        }

        /// <summary>
        /// General constructor for expressions with operators that have any arity.
        /// This constructor assumes a bit-size of zero.
        /// </summary>
        /// 
        /// <param name="op">Operator of the expression.</param>
        /// <param name="parameterList">List of expressions for the operands
        /// of the operator.</param>
        public Expression(Operator op, List<Expression> parameterList) :
            this(op, parameterList, 0) { }

        #endregion

        #region Methods

        /// <summary>
        /// Computes the value of this Expression.
        /// </summary>
        /// 
        /// <returns>Value of this Expression.</returns>
        private string ComputeValue()
        {
            string result = null;

            switch (op.Type)
            {
                case OperatorType.True:
                    return "true";
                case OperatorType.False:
                    return "false";

                case OperatorType.Constant:
                case OperatorType.Variable:
                case OperatorType.ArrayVariable:
                    return value;

                case OperatorType.Negate:
                case OperatorType.Not:
                case OperatorType.BitComplement:
                    return op.Symbol + parameterList[0].value;

                case OperatorType.Addition:
                case OperatorType.Subtraction:
                case OperatorType.Multiplication:
                case OperatorType.SDivision:
                case OperatorType.UDivision:
                case OperatorType.Remainder:

                case OperatorType.Equal:
                case OperatorType.NotEqual:

                case OperatorType.LessThan:
                case OperatorType.ULessThan:
                case OperatorType.FLessThan:

                case OperatorType.GreaterThan:
                case OperatorType.UGreaterThan:
                case OperatorType.FGreaterThan:

                case OperatorType.LessThanEqual:
                case OperatorType.ULessThanEqual:
                case OperatorType.FLessThanEqual:

                case OperatorType.GreaterThanEqual:
                case OperatorType.UGreaterThanEqual:
                case OperatorType.FGreaterThanEqual:

                case OperatorType.And:
                case OperatorType.Or:

                case OperatorType.BitAnd:
                case OperatorType.BitOr:
                case OperatorType.BitXor:

                case OperatorType.ShiftLeft:
                case OperatorType.AShiftRight:
                case OperatorType.LShiftRight:

                case OperatorType.Implies:
                case OperatorType.Iff:
                case OperatorType.Let:
                    return "(" + parameterList[0].value + " " + op.Symbol + " " +
                        parameterList[1].value + ")";

                case OperatorType.Concatenate:
                case OperatorType.ZeroExtend:
                case OperatorType.SignExtend:
                    return op.Symbol + "(" + parameterList[0].value + ", " +
                        parameterList[1].value + ")";

                case OperatorType.Array:
                    return parameterList[0].value + "[" + parameterList[1].value + "]";

                case OperatorType.Offset:
                    return "(" + parameterList[0].value + "." + parameterList[1].value + ")";

                case OperatorType.Select:
                case OperatorType.Store:
                case OperatorType.Ite:
                case OperatorType.Address:
                case OperatorType.BitExtract:
                    result = op.Symbol + "(";
                    for (int i = 0; i < this.parameterList.Count - 1; i++)
                    {
                        result = result + this.parameterList[i].value + ", ";
                    }
                    return result + this.parameterList[this.parameterList.Count - 1].value + ")";

                case OperatorType.Function:
                    result = "(" + op.Symbol + " " + "(";
                    for (int i = 0; i < this.parameterList.Count - 2; i++)
                    {
                        result = result + this.parameterList[i].value + ", ";
                    }
                    result += this.parameterList[this.parameterList.Count - 2].value + ")" + " ";
                    return result + this.parameterList[this.parameterList.Count - 1].value + ")";

                case OperatorType.FunctionCall:
                    return "(" + parameterList[0].value + " " + parameterList[1].value + ")";

               default:
                    throw new NotImplementedException("PHOENIX: " + 
                        "Operator type not implemented: " + op.Type.ToString());
            }
        }

        /// <summary>
        /// Checks if this Expression is equal to the Expression provided.
        /// </summary>
        /// 
        /// <param name="other">Expression to compare this Expression with.</param>
        /// <returns>True if this Expression is equal to the input Expression;
        /// false otherwise.</returns>
        public override bool Equals(object other)
        {
            /* If the parameter is null, return false. */
            if (other == null) { return false; }

            /* If the parameter cannot be cast as an Expression, return false. */
            Expression otherExpr = other as Expression;
            if ((object)otherExpr == null) { return false; }

            return EqualsHelper(this, otherExpr);
        }

        /// <summary>
        /// Helper function that checks if the first input Expression is equal to
        /// the second input Expression.
        /// </summary>
        /// 
        /// <param name="oneExpr">One Expression to compare.</param>
        /// <param name="otherExpr">Other Expression to compare.</param>
        /// <returns>True if the first input Expression is equal to the second input Expression;
        /// false otherwise.</returns>
        private static bool EqualsHelper(Expression oneExpr, Expression otherExpr)
        {
            if (oneExpr.bitSize != otherExpr.bitSize) { return false; }
            if (oneExpr.Op != otherExpr.Op) { return false; }
            if (oneExpr.Op.Arity == OperatorArity.Nil) { return oneExpr.value.Equals(otherExpr.value); }
            if (oneExpr.ParameterList.Count != otherExpr.ParameterList.Count) { return false; }

            int numParams = oneExpr.ParameterList.Count;
            /* Special case: If both the Expressions are functions, replace the argument (and all
             * of its occurrences) of one function with the argument of the other function. */
            if (oneExpr.Op.Type == OperatorType.Function &&
                otherExpr.Op.Type == OperatorType.Function)
            {
                for (int paramNum = 0; paramNum < numParams - 1; paramNum++)
                {
                    oneExpr = oneExpr.Replace(oneExpr.parameterList[paramNum],
                        otherExpr.parameterList[paramNum]);
                }
                return EqualsHelper(oneExpr.parameterList[numParams - 1],
                    otherExpr.parameterList[numParams - 1]);
            }

            for (int paramNum = 0; paramNum < numParams; paramNum++)
            {
                bool paramsEqual = EqualsHelper(oneExpr.parameterList[paramNum],
                    otherExpr.parameterList[paramNum]);
                if (!paramsEqual) { return false; }
            }

            return true;
        }

        /// <summary>
        /// Returns the string representation of this Expression.
        /// </summary>
        /// 
        /// <returns>String representation of this Expression.</returns>
        public override string ToString()
        {
            return this.value;
        }

        /// <summary>
        /// Returns the hash code of this Expression.
        /// </summary>
        /// 
        /// <returns>Hash code of this Expression.</returns>
        public override int GetHashCode()
        {
            if (this.Op.Arity == OperatorArity.Nil) { return this.value.GetHashCode(); }
            if (this.Op.Type != OperatorType.Function) { return this.value.GetHashCode(); }

            /* Special case for functions: Replace the arguments before computing the hash code. */
            int numParams = this.parameterList.Count;
            Expression replacedExpr = this;
            for (int i = 0; i < numParams - 1; i++)
            {
                replacedExpr = replacedExpr.Replace(this.parameterList[i],
                    new Expression(OperatorStore.VariableOp, "_", this.parameterList[i].bitSize));
            }
            return replacedExpr.parameterList[numParams - 1].GetHashCode();
        }

        /// <summary>
        /// Creates a deep clone of this expression.
        /// </summary>
        /// 
        /// <returns>Deep clone of this expression.</returns>
        public Expression Clone()
        {
            Expression result = null;

            int numParams = this.parameterList.Count;
            if (numParams == 0)
            {
                result = new Expression(this.op, this.value, this.bitSize);
                result.type = this.type;
                return result;
            }

            List<Expression> newParameterList = new List<Expression>(numParams);
            foreach (Expression param in this.parameterList)
            {
                newParameterList.Add(param.Clone());
            }
            
            result = new Expression(this.op, newParameterList, this.bitSize);
            result.type = this.type;
            return result;
        }

        /// <summary>
        /// Returns the parameter at the input position in the parameter list of this Expression.
        /// </summary>
        /// 
        /// <param name="paramNumber">Position of the requested parameter.</param>
        /// <returns>Parameter at the input position.</returns>
        /// <remarks>Precondition: <paramref name="paramNumber"/> is a valid position.</remarks>
        public Expression GetParameter(int paramNumber)
        {
            Trace.Assert(0 <= paramNumber && paramNumber < this.parameterList.Count,
                "PHOENIX: Invalid parameter position.");
            return this.parameterList[paramNumber];
        }

        /// <summary>
        /// Updates the parameter at the input position in the parameter
        /// list of this Expression with the input new parameter.
        /// </summary>
        /// 
        /// <param name="paramNumber">Position of the requested parameter.</param>
        /// <param name="newParam">Input new parameter at the input position in
        /// the parameter list of this Expression.</param>
        /// <remarks>Precondition: <paramref name="paramNumber"/> is a valid position.</remarks>
        public void UpdateParameter(int paramNumber, Expression newParam)
        {
            Trace.Assert(0 <= paramNumber && paramNumber < this.parameterList.Count,
                "PHOENIX: Invalid parameter position.");
            this.parameterList[paramNumber] = newParam;
            this.value = this.ComputeValue();
        }

        /// <summary>
        /// Replaces all occurrences of one Expression in this Expression with another Expression.
        /// </summary>
        /// 
        /// <param name="toReplaceExpr">Expression to replace.</param>
        /// <param name="replacementExpr">Replacement Expression.</param>
        /// <returns>New Expression where all the occurrences of one Expression in this Expression
        /// are replaced with another Expression. This Expression is not modified.</returns>
        public Expression Replace(Expression toReplaceExpr, Expression replacementExpr)
        {
            if (this.Op.Arity == OperatorArity.Nil)
            {
                /* Base case: Zero-arity operators. */
                return this.Equals(toReplaceExpr) ? replacementExpr : this.Clone();
            }
            else
            {
                /* Recursive case: Replace occurrences within the parameters of this Expression. */
                Expression result = null;

                List<Expression> newParamList = new List<Expression>();
                foreach (Expression paramExpr in this.parameterList)
                {
                    Expression newParamExpr = paramExpr.Replace(toReplaceExpr, replacementExpr);
                    newParamList.Add(newParamExpr);
                }

                result = new Expression(this.Op, newParamList, this.BitSize);
                result.Type = this.Type;
                return result;
            }
        }

        #endregion
    }

    /// <summary>
    /// This class describes a constant (and is derived from the <see cref="Expression"/> class).
    /// </summary>
    public class Constant : Expression
    {
        #region Constructors

        /// <summary>
        /// Creates an instance of the Constant class.
        /// </summary>
        /// 
        /// <param name="value">Value of the constant.</param>
        /// <param name="bitSize">Bit-size of the expression.</param>
        public Constant(uint value, uint bitSize) :
            base(OperatorStore.ConstantOp, value.ToString(), bitSize) { }

        /// <summary>
        /// Creates an instance of the Constant class.
        /// </summary>
        /// 
        /// <param name="value">Value of the constant.</param>
        /// <param name="bitSize">Bit-size of the expression.</param>
        public Constant(int value, uint bitSize) :
            base(OperatorStore.ConstantOp, value.ToString(), bitSize) { }

        /// <summary>
        /// Creates an instance of the Constant class.
        /// </summary>
        /// 
        /// <param name="value">Value of the constant.</param>
        /// <param name="bitSize">Bit-size of the expression.</param>
        public Constant(string value, uint bitSize) :
            base(OperatorStore.ConstantOp, value, bitSize) { }

        #endregion
    }
}
