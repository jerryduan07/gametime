/*************************************************************************************************
 * Operator                                                                                      *
 * ============================================================================================= *
 * This file contains the classes that describe and provide                                      *
 * the operators that can be used in Expressions.                                                *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/


namespace Utilities
{
    /// <summary>
    /// Enumeration that lists the possible arities of any operator.
    /// </summary>
    public enum OperatorArity { Nil, Unary, Binary, Ternary, Polynary };

    /// <summary>
    /// Enumeration that lists the operators that can be used in Expressions.
    /// </summary>
    public enum OperatorType
    {
        Addition, Subtraction, Multiplication,
        SDivision, UDivision, Remainder,

        Equal, NotEqual,
        LessThan, ULessThan, FLessThan,
        LessThanEqual, ULessThanEqual, FLessThanEqual,
        GreaterThan, UGreaterThan, FGreaterThan,
        GreaterThanEqual, UGreaterThanEqual, FGreaterThanEqual,
        Negate,
        
        And, Or, Not,

        BitAnd, BitOr, BitXor, BitComplement,
        ShiftLeft, AShiftRight, LShiftRight,
        Concatenate, ZeroExtend, SignExtend, BitExtract,

        Implies, Iff, Let, Ite,

        Variable, Constant, True, False,

        ArrayVariable, Array, Offset, Select, Store,
        Address,

        Function, FunctionCall,

        Acquire, Release
    };

    /// <summary>
    /// This class describes the operators that can be used in Expressions.
    /// </summary>
    public class Operator
    {
        #region Fields and Properties

        /// <summary>
        /// Arity of the operator.
        /// </summary>
        private OperatorArity arity;

        /// <summary>
        /// Gets the arity of the operator.
        /// </summary>
        public OperatorArity Arity
        {
            get { return this.arity; }
        }

        /// <summary>
        /// Symbol for the operator.
        /// </summary>
        private string symbol;

        /// <summary>
        /// Gets the symbol for the operator.
        /// </summary>
        public string Symbol
        {
            get { return this.symbol; }
        }

        /// <summary>
        /// Type of the operator.
        /// </summary>
        private OperatorType type;

        /// <summary>
        /// Gets the type of the operator.
        /// </summary>
        public OperatorType Type
        {
            get { return this.type; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the Operator class.
        /// </summary>
        /// 
        /// <param name="opArity">Arity of the operator.</param>
        /// <param name="symbol">Operator symbol.</param>
        /// <param name="opType">Operator type.</param>
        public Operator(OperatorArity opArity, string symbol, OperatorType opType)
        {
            this.arity = opArity;
            this.symbol = symbol;
            this.type = opType; 
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the string representation of the operator.
        /// </summary>
        /// 
        /// <returns>String representation of the operator.</returns>
        public override string ToString()
        {
            return this.symbol;
        }

        #endregion
    }

    /// <summary>
    /// This class provides the operators that can be used in Expressions.
    /// </summary>
    public static class OperatorStore
    {
        #region Fields
        
        #region Zero Arity Operators

        private static Operator constantOp =
            new Operator(OperatorArity.Nil, "const", OperatorType.Constant);
        private static Operator trueOp =
            new Operator(OperatorArity.Nil, "true", OperatorType.True);
        private static Operator falseOp =
            new Operator(OperatorArity.Nil, "false", OperatorType.False);
        private static Operator variableOp =
            new Operator(OperatorArity.Nil, "var", OperatorType.Variable);
        private static Operator arrayVariableOp =
            new Operator(OperatorArity.Nil, "arrayvar", OperatorType.ArrayVariable);

        private static Operator acquireOp =
            new Operator(OperatorArity.Nil, "acquire", OperatorType.Acquire);
        
        #endregion

        #region Unary Operators

        private static Operator notOp =
            new Operator(OperatorArity.Unary, "!", OperatorType.Not);
        private static Operator negateOp =
            new Operator(OperatorArity.Unary, "-", OperatorType.Negate);
        private static Operator addressOp =
            new Operator(OperatorArity.Unary, "&", OperatorType.Address);

        private static Operator bitComplementOp =
            new Operator(OperatorArity.Binary, "~", OperatorType.BitComplement);

        private static Operator releaseOp =
            new Operator(OperatorArity.Unary, "release", OperatorType.Release);

        #endregion

        #region Binary Operators

        private static Operator addOp =
            new Operator(OperatorArity.Binary, "+", OperatorType.Addition);
        private static Operator subOp =
            new Operator(OperatorArity.Binary, "-", OperatorType.Subtraction);
        private static Operator multOp =
            new Operator(OperatorArity.Binary, "*", OperatorType.Multiplication);
        private static Operator sDivOp =
            new Operator(OperatorArity.Binary, "/s", OperatorType.SDivision);
        private static Operator uDivOp =
            new Operator(OperatorArity.Binary, "/s", OperatorType.UDivision);
        private static Operator remOp =
            new Operator(OperatorArity.Binary, "%", OperatorType.Remainder);

        private static Operator equalOp =
            new Operator(OperatorArity.Binary, "==", OperatorType.Equal);
        private static Operator notEqualOp =
            new Operator(OperatorArity.Binary, "!=", OperatorType.NotEqual);
        private static Operator lessThanOp =
            new Operator(OperatorArity.Binary, "<", OperatorType.LessThan);
        private static Operator uLessThanOp =
            new Operator(OperatorArity.Binary, "<u", OperatorType.ULessThan);
        private static Operator fLessThanOp =
            new Operator(OperatorArity.Binary, "<f", OperatorType.FLessThan);
        private static Operator lessThanEqualOp =
            new Operator(OperatorArity.Binary, "<=", OperatorType.LessThanEqual);
        private static Operator uLessThanEqualOp =
            new Operator(OperatorArity.Binary, "<=u", OperatorType.ULessThanEqual);
        private static Operator fLessThanEqualOp =
            new Operator(OperatorArity.Binary, "<=f", OperatorType.FLessThanEqual);
        private static Operator greaterThanOp =
            new Operator(OperatorArity.Binary, ">", OperatorType.GreaterThan);
        private static Operator uGreaterThanOp =
            new Operator(OperatorArity.Binary, ">u", OperatorType.UGreaterThan);
        private static Operator fGreaterThanOp =
            new Operator(OperatorArity.Binary, ">f", OperatorType.FGreaterThan);
        private static Operator greaterThanEqualOp =
            new Operator(OperatorArity.Binary, ">=", OperatorType.GreaterThanEqual);
        private static Operator uGreaterThanEqualOp =
            new Operator(OperatorArity.Binary, ">=u", OperatorType.UGreaterThanEqual);
        private static Operator fGreaterThanEqualOp =
            new Operator(OperatorArity.Binary, ">=f", OperatorType.FGreaterThanEqual);

        private static Operator andOp =
            new Operator(OperatorArity.Binary, "and", OperatorType.And);
        private static Operator orOp =
            new Operator(OperatorArity.Binary, "or", OperatorType.Or);

        private static Operator bitAndOp =
            new Operator(OperatorArity.Binary, "&", OperatorType.BitAnd);
        private static Operator bitOrOp =
            new Operator(OperatorArity.Binary, "|", OperatorType.BitOr);
        private static Operator bitXorOp =
            new Operator(OperatorArity.Binary, "^", OperatorType.BitXor);
        private static Operator shiftLeftOp =
            new Operator(OperatorArity.Binary, "<<", OperatorType.ShiftLeft);
        private static Operator aShiftRightOp =
            new Operator(OperatorArity.Binary, ">>", OperatorType.AShiftRight);
        private static Operator lShiftRightOp =
            new Operator(OperatorArity.Binary, ">>>", OperatorType.LShiftRight);

        private static Operator concatenateOp =
            new Operator(OperatorArity.Binary, "concat", OperatorType.Concatenate);
        private static Operator zeroExtendOp =
            new Operator(OperatorArity.Binary, "zeroExtend", OperatorType.ZeroExtend);
        private static Operator signExtendOp =
            new Operator(OperatorArity.Binary, "signExtend", OperatorType.SignExtend);

        private static Operator impliesOp =
            new Operator(OperatorArity.Binary, "=>", OperatorType.Implies);
        private static Operator iffOp =
            new Operator(OperatorArity.Binary, "iff", OperatorType.Iff);
        private static Operator letOp =
            new Operator(OperatorArity.Binary, "let", OperatorType.Let);

        private static Operator arrayOp =
            new Operator(OperatorArity.Binary, "[]", OperatorType.Array);
        private static Operator offsetOp =
            new Operator(OperatorArity.Binary, ".", OperatorType.Offset);
        private static Operator selectOp =
            new Operator(OperatorArity.Binary, "select", OperatorType.Select);

        #endregion

        #region Ternary Operators

        private static Operator iteOp =
            new Operator(OperatorArity.Ternary, "ite", OperatorType.Ite);

        private static Operator storeOp =
            new Operator(OperatorArity.Ternary, "store", OperatorType.Store);

        private static Operator bitExtractOp =
            new Operator(OperatorArity.Ternary, "bitExtract", OperatorType.BitExtract);

        #endregion

        #region Polynary Operators

        private static Operator functionOp =
            new Operator(OperatorArity.Polynary, "_", OperatorType.Function);

        private static Operator functionCallOp =
            new Operator(OperatorArity.Polynary, "", OperatorType.FunctionCall);

        #endregion

        #endregion

        #region Properties

        #region Zero arity operators

        /// <summary>
        /// Gets the variable operator.
        /// </summary>
        public static Operator VariableOp
        {
            get { return variableOp; }
        }

        /// <summary>
        /// Gets the constant operator.
        /// </summary>
        public static Operator ConstantOp
        {
            get { return constantOp; }
        }

        /// <summary>
        /// Gets the operator that represents true Boolean values.
        /// </summary>
        public static Operator TrueOp
        {
            get { return trueOp; }
        }

        /// <summary>
        /// Gets the operator that represents false Boolean values.
        /// </summary>
        public static Operator FalseOp
        {
            get { return falseOp; }
        }

        #endregion

        #region Arithmetic operators

        /// <summary>
        /// Gets the addition operator.
        /// </summary>
        public static Operator AddOp
        {
            get { return addOp; }
        }
        
        /// <summary>
        /// Gets the subtraction operator.
        /// </summary>
        public static Operator SubOp
        {
            get { return subOp; }
        }

        /// <summary>
        /// Gets the multiplication operator.
        /// </summary>
        public static Operator MultOp
        {
            get { return multOp; }
        }

        /// <summary>
        /// Gets the signed division operator.
        /// </summary>
        public static Operator SDivOp
        {
            get { return sDivOp; }
        }

        /// <summary>
        /// Gets the unsigned division operator.
        /// </summary>
        public static Operator UDivOp
        {
            get { return uDivOp; }
        }

        /// <summary>
        /// Gets the remainder operator.
        /// </summary>
        public static Operator RemOp
        {
            get { return remOp; }
        }

        /// <summary>
        /// Gets the negate operator.
        /// </summary>
        public static Operator NegateOp
        {
            get { return negateOp; }
        }

        #endregion

        #region Comparison operators

        /// <summary>
        /// Gets the equal operator.
        /// </summary>
        public static Operator EqualOp
        {
            get { return equalOp; }
        }

        /// <summary>
        /// Gets the not equal operator.
        /// </summary>
        public static Operator NotEqualOp
        {
            get { return notEqualOp; }
        }

        /// <summary>
        /// Gets the less-than operator.
        /// </summary>
        public static Operator LessThanOp
        {
            get { return lessThanOp; }
        }

        /// <summary>
        /// Gets the unsigned less-than operator.
        /// </summary>
        public static Operator ULessThanOp
        {
            get { return uLessThanOp; }
        }

        /// <summary>
        /// Gets the float less-than operator.
        /// </summary>
        public static Operator FLessThanOp
        {
            get { return fLessThanOp; }
        }

        /// <summary>
        /// Gets the less-than-or-equal operator.
        /// </summary>
        public static Operator LessThanEqualOp
        {
            get { return lessThanEqualOp; }
        }

        /// <summary>
        /// Gets the unsigned less-than-or-equal operator.
        /// </summary>
        public static Operator ULessThanEqualOp
        {
            get { return uLessThanEqualOp; }
        }

        /// <summary>
        /// Gets the float less-than-or-equal operator.
        /// </summary>
        public static Operator FLessThanEqualOp
        {
            get { return fLessThanEqualOp; }
        }

        /// <summary>
        /// Gets the greater-than operator.
        /// </summary>
        public static Operator GreaterThanOp
        {
            get { return greaterThanOp; }
        }

        /// <summary>
        /// Gets the unsigned greater-than operator.
        /// </summary>
        public static Operator UGreaterThanOp
        {
            get { return uGreaterThanOp; }
        }

        /// <summary>
        /// Gets the float greater-than operator.
        /// </summary>
        public static Operator FGreaterThanOp
        {
            get { return fGreaterThanOp; }
        }

        /// <summary>
        /// Gets the greater-than-or-equal operator.
        /// </summary>
        public static Operator GreaterThanEqualOp
        {
            get { return greaterThanEqualOp; }
        }

        /// <summary>
        /// Gets the unsigned greater-than-or-equal operator.
        /// </summary>
        public static Operator UGreaterThanEqualOp
        {
            get { return uGreaterThanEqualOp; }
        }

        /// <summary>
        /// Gets the float greater-than-or-equal operator.
        /// </summary>
        public static Operator FGreaterThanEqualOp
        {
            get { return fGreaterThanEqualOp; }
        }

        #endregion

        #region Boolean logic operators

        /// <summary>
        /// Gets the logical-AND operator.
        /// </summary>
        public static Operator AndOp
        {
            get { return andOp; }
        }

        /// <summary>
        /// Gets the logical-OR operator.
        /// </summary>
        public static Operator OrOp
        {
            get { return orOp; }
        }

        /// <summary>
        /// Gets the logical-NOT operator.
        /// </summary>
        public static Operator NotOp
        {
            get { return notOp; }
        }

        #endregion

        #region Bit operators

        /// <summary>
        /// Gets the shift-left operator.
        /// </summary>
        public static Operator ShiftLeftOp
        {
            get { return shiftLeftOp; }
        }

        /// <summary>
        /// Gets the arithmetic shift-right operator.
        /// </summary>
        public static Operator AShiftRightOp
        {
            get { return aShiftRightOp; }
        }

        /// <summary>
        /// Gets the logical shift-right operator.
        /// </summary>
        public static Operator LShiftRightOp
        {
            get { return lShiftRightOp; }
        }

        /// <summary>
        /// Gets the bitwise-AND operator.
        /// </summary>
        public static Operator BitAndOp
        {
            get { return bitAndOp; }
        }

        /// <summary>
        /// Gets the bitwise-OR operator.
        /// </summary>
        public static Operator BitOrOp
        {
            get { return bitOrOp; }
        }

        /// <summary>
        /// Gets the bitwise-XOR operator.
        /// </summary>
        public static Operator BitXorOp
        {
            get { return bitXorOp; }
        }

        /// <summary>
        /// Gets the bitwise-complement operator.
        /// </summary>
        public static Operator BitComplementOp
        {
            get { return bitComplementOp; }
        }

        /// <summary>
        /// Gets the concatenation operator.
        /// </summary>
        public static Operator ConcatenateOp
        {
            get { return concatenateOp; }
        }

        /// <summary>
        /// Gets the zero-extend operator. For example, "zeroExtend(x, 16)" represents an Expression
        /// with a value whose bit representation has 16 zeros, followed by the bits of x.
        /// </summary>
        public static Operator ZeroExtendOp
        {
            get { return zeroExtendOp; }
        }

        /// <summary>
        /// Gets the sign-extend operator. For example, "signExtend(x, 16)" represents an Expression
        /// with value x but with bit-size (w + 16), where w is the original bit-size of x.
        /// </summary>
        public static Operator SignExtendOp
        {
            get { return signExtendOp; }
        }

        /// <summary>
        /// Gets the bit-extract operator. For example, "bitExtract(high, low, x)" represents an
        /// Expression that returns x[low:high].
        /// </summary>
        public static Operator BitExtractOp
        {
            get { return bitExtractOp; }
        }

        #endregion

        #region Array operators

        /// <summary>
        /// Gets the array variable operator.
        /// </summary>
        public static Operator ArrayVariableOp
        {
            get { return arrayVariableOp; }
        }

        /// <summary>
        /// Gets the array operator.
        /// </summary>
        public static Operator ArrayOp
        {
            get { return arrayOp; }
        }

        /// <summary>
        /// Gets the offset operator, which represents a bit offset from the start
        /// of a memory location.
        /// </summary>
        public static Operator OffsetOp
        {
            get { return offsetOp; }
        }

        /// <summary>
        /// Gets the array selection operator.
        /// </summary>
        public static Operator SelectOp
        {
            get { return selectOp; }
        }

        /// <summary>
        /// Gets the array storage operator.
        /// </summary>
        public static Operator StoreOp
        {
            get { return storeOp; }
        }

        /// <summary>
        /// Gets the address operator.
        /// </summary>
        public static Operator AddressOp
        {
            get { return addressOp; }
        }

        #endregion

        #region Function operators

        /// <summary>
        /// Gets the anonymous function operator. This is used to represent anonymous functions.
        /// The last argument of this operator represents the body of the function, while
        /// the rest of the arguments represents the formal arguments of the function.
        /// </summary>
        public static Operator FunctionOp
        {
            get { return functionOp; }
        }

        /// <summary>
        /// Gets the function call operator. This is used to represent calls of anonymous functions.
        /// </summary>
        public static Operator FunctionCallOp
        {
            get { return functionCallOp; }
        }

        #endregion

        #region Thread operators

        /// <summary>
        /// Gets the acquire operator.
        /// </summary>
        public static Operator AcquireOp
        {
            get { return acquireOp; }
        }

        /// <summary>
        /// Gets the release operator.
        /// </summary>
        public static Operator ReleaseOp
        {
            get { return releaseOp; }
        }

        #endregion

        #region Miscellaneous operators

        /// <summary>
        /// Gets the implies operator.
        /// </summary>
        public static Operator ImpliesOp
        {
            get { return impliesOp; }
        }

        /// <summary>
        /// Gets the iff operator.
        /// </summary>
        public static Operator IffOp
        {
            get { return iffOp; }
        }

        /// <summary>
        /// Gets the let operator.
        /// </summary>
        public static Operator LetOp
        {
            get { return letOp; }
        }

        /// <summary>
        /// Gets the if-then-else operator.
        /// </summary>
        public static Operator IteOp
        {
            get { return iteOp; }
        }

        #endregion

        #endregion
    }
}
