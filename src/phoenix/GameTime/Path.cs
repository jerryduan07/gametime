/*************************************************************************************************
 * Path                                                                                          *
 * ============================================================================================= *
 * This file contains the class that describes a path in                                         *
 * the flow graph of a Phoenix function unit.                                                    *
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

using Utilities;


namespace GameTime
{
    /// <summary>
    /// This class describes a path in the flow graph of
    /// a Phoenix function unit.
    /// </summary>
    public class Path
    {
        #region Fields and Properties

        /// <summary>
        /// Source basic block (the first basic block) of the path.
        /// </summary>
        private BasicBlock sourceBlock;

        /// <summary>
        /// Gets the source basic block (the first basic block) of the path.
        /// </summary>
        public BasicBlock SourceBlock
        {
            get { return this.sourceBlock; }
        }

        /// <summary>
        /// Sink basic block (the last basic block) of the path.
        /// </summary>
        private BasicBlock sinkBlock;

        /// <summary>
        /// Gets the sink basic block (the last basic block) of the path.
        /// </summary>
        public BasicBlock SinkBlock
        {
            get { return this.sinkBlock; }
        }

        /// <summary>
        /// Function unit that contains the basic blocks of the path.
        /// </summary>
        private FunctionUnit functionUnit;

        /// <summary>
        /// Gets the function unit that contains the basic blocks of this path.
        /// </summary>
        public FunctionUnit FunctionUnit
        {
            get { return this.functionUnit; }
        }

        /// <summary>
        /// Object that maintains configuration information about GameTime.
        /// </summary>
        private Utilities.Configuration config;

        /// <summary>
        /// Gets the object that maintains configuration information about GameTime.
        /// </summary>
        public Utilities.Configuration Config
        {
            get { return this.config; }
        }

        /// <summary>
        /// Object that maintains information about the current GameTime project.
        /// </summary>
        private ProjectConfiguration projectConfig;

        /// <summary>
        /// Gets the object that maintains information about the current GameTime project.
        /// </summary>
        public Utilities.ProjectConfiguration ProjectConfig
        {
            get { return this.projectConfig; }
        }

        /// <summary>
        /// List of basic blocks (of the flow graph of the function unit) that lie along the path.
        /// </summary>
        private List<BasicBlock> blocks;

        /// <summary>
        /// Gets or sets the list of basic blocks (of the flow graph of the function unit)
        /// that lie along the path.
        /// </summary>
        public List<BasicBlock> Blocks
        {
            get { return this.blocks; }
            set { this.blocks = value; }
        }

        /// <summary>
        /// Dictionary that maps the ID of a basic block to the ID of its successor basic block
        /// along the path. This memoization is used to find edges along the path, given the ID
        /// of the source basic block.
        /// </summary>
        private Dictionary<uint, uint> blockSuccessors;

        /// <summary>
        /// Gets the dictionary that maps the ID of a basic block to the ID of its successor
        /// basic block along the path.
        /// </summary>
        public Dictionary<uint, uint> BlockSuccessors
        {
            get { return this.blockSuccessors; }
        }

        /// <summary>
        /// List of pairs, where the first element is an Expression and the second element
        /// is the ID of the basic block that contains the conditional Expression. The Expressions
        /// represent the conditions that must be true to traverse this path.
        /// </summary>
        private List<Pair<Expression, uint>> conditions;

        /// <summary>
        /// Gets the list of pairs, where the first element is an Expression and the second
        /// element is the ID of the basic block that contains the conditional Expression.
        /// The Expressions represent the conditions that must be true to traverse this path.
        /// </summary>
        public List<Pair<Expression, uint>> Conditions
        {
            get { return this.conditions; }
        }

        /// <summary>
        /// Dictionary that maps an operand to its translated Expression. This dictionary
        /// memoizes translations, improving code efficiency.
        /// </summary>
        private Dictionary<Operand, Expression> operandExpressions;

        /// <summary>
        /// Gets the dictionary that maps an operand to its translated Expression.
        /// </summary>
        public Dictionary<Operand, Expression> OperandExpressions
        {
            get { return this.operandExpressions; }
        }

        /// <summary>
        /// Set of variable Expressions that occur in conditions and assignments along the path.
        /// </summary>
        private HashSet<Expression> variables;

        /// <summary>
        /// Gets the set of variable Expressions that occur in conditions and assignments
        /// along the path.
        /// </summary>
        public HashSet<Expression> Variables
        {
            get { return this.variables; }
        }

        /// <summary>
        /// Dictionary that maps between a variable Expression whose address has been taken
        /// (an "address-taken" variable) and the temporary array variable Expression that
        /// represents a pointer to the variable Expression.
        /// </summary>
        private Dictionary<Expression, Expression> addressTaken;

        /// <summary>
        /// Gets the dictionary that maps between a variable Expression whose address has
        /// been taken (an "address-taken" variable) and the temporary array variable Expression
        /// that represents a pointer to the variable Expression.
        /// </summary>
        public Dictionary<Expression, Expression> AddressTaken
        {
            get { return this.addressTaken; }
        }

        /// <summary>
        /// Set of array variable Expressions that occur in conditions and assignments
        /// along the path.
        /// </summary>
        private HashSet<Expression> arrayVariables;

        /// <summary>
        /// Gets the set of array variable Expressions that occur in conditions and assignments
        /// along the path.
        /// </summary>
        public HashSet<Expression> ArrayVariables
        {
            get { return this.arrayVariables; }
        }

        /// <summary>
        /// Dictionary that maps between array variable Expressions and the dimensions of
        /// the arrays that they refer to. The "dimension" of an array is a list of
        /// bit-sizes that appear in the type of the operand that refers to the array.
        /// For example, an array that maps an integer to a character, such as a character array,
        /// has the dimensions (32, 8).
        /// </summary>
        private Dictionary<Expression, List<uint>> arrayDimensions;

        /// <summary>
        /// Gets the dictionary that maps between array variable Expressions and the dimensions
        /// of the arrays that they refer to.
        /// </summary>
        public Dictionary<Expression, List<uint>> ArrayDimensions
        {
            get { return this.arrayDimensions; }
        }

        /// <summary>
        /// Table that maps an Expression to the Expression that it is an alias for. This means
        /// that the Expression can be replaced with the Expression that it is an alias for,
        /// without changing the result. This is especially helpful when a memory location
        /// can be accessed through at least two different names: for example, if "a" and "b"
        /// point to the same memory location, then this table will contain the anonymous
        /// "dereference function" Expression "(_ (__gtINDEX0) a[__gtINDEX0])" and its alias
        /// "(_ (__gtINDEX1) b[__gtINDEX1])", so that any call to the latter can, and will,
        /// be replaced by a call to the former.
        /// </summary>
        private Dictionary<Expression, Expression> aliasTable;

        /// <summary>
        /// Gets the table that maps an Expression to the Expression that it aliases.
        /// </summary>
        public Dictionary<Expression, Expression> AliasTable
        {
            get { return this.aliasTable; }
        }

        /// <summary>
        /// Offset table that maps an aggregate (struct or union) Expression to a pair whose
        /// first element is a "base aggregate" Expression and whose second element is
        /// the Expression for the offset of the start of the aggregate from
        /// the "base aggregate" (in bits).
        /// </summary>
        private Dictionary<Expression, Pair<Expression, Expression>> aggregateOffsetTable;

        /// <summary>
        /// Gets the offset table that maps an aggregate (struct or union) Expression to
        /// a pair whose first element is a "base aggregate" Expression and whose second
        /// element is the Expression for the offset of the start of the aggregate from
        /// the "base aggregate" (in bits).
        /// </summary>
        public Dictionary<Expression, Pair<Expression, Expression>> AggregateOffsetTable
        {
            get { return this.aggregateOffsetTable; }
        }

        /// <summary>
        /// List of array accesses that occur in conditions and assignments along the path.
        /// An "array access" is a pair that maps an array variable Expression to a list of
        /// the numbers of the temporary indices in an array access. For example, the array
        /// access Expression "a[__gtINDEX0][__gtINDEX2]" is stored as a pair that maps
        /// the array variable Expression for "a" to a list that contains the numbers 0 and 2.
        /// </summary>
        private List<Pair<Expression, List<uint>>> arrayAccesses;

        /// <summary>  
        /// Gets the list of array accesses that occur in conditions and assignments along
        /// the path.
        /// </summary>
        public List<Pair<Expression, List<uint>>> ArrayAccesses
        {
            get { return this.arrayAccesses; }
        }

        /// <summary>
        /// Dictionary that maps temporary indices in array access Expressions to
        /// the Expressions that these indices replace.
        /// </summary>
        private Dictionary<uint, Expression> temporaryIndexExpressions;

        /// <summary>
        /// Gets the dictionary that maps temporary indices in array access Expressions
        /// to the Expressions that these indices replace.
        /// </summary>
        public Dictionary<uint, Expression> TemporaryIndexExpressions
        {
            get { return this.temporaryIndexExpressions; }
        }

        /// <summary>
        /// Counter that keeps track of the number of temporary (non-index) variable Expressions
        /// that have already been generated.
        /// </summary>
        private uint temporaryVariableCounter;

        /// <summary>
        ///ounter that keeps track of the number of temporary index variable Expressions
        /// that have already been generated.
        /// </summary>
        private uint temporaryIndexCounter;

        /// <summary>
        /// Counter that keeps track of the number of temporary pointer variable Expressions
        /// that have already been generated.
        /// </summary>
        private uint temporaryPointerCounter;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for the Path class.
        /// </summary>
        /// 
        /// <param name="blocks">List of basic blocks (of the flow graph of the function unit)
        /// that lie along the path.</param>
        /// <param name="config">Configuration object that contains GameTime configuration
        /// information.</param>
        /// <param name="projectConfig">ProjectConfiguration object that contains project
        /// configuration information.</param>
        public Path(List<BasicBlock> blocks, Utilities.Configuration config,
            ProjectConfiguration projectConfig)
        {
            this.sourceBlock = blocks[0];
            this.sinkBlock = blocks[blocks.Count - 1];

            Trace.Assert(sourceBlock.FunctionUnit == sinkBlock.FunctionUnit,
                "PHOENIX: The path is not contained within one function unit.");
            this.functionUnit = sourceBlock.FunctionUnit;

            this.config = config;
            this.projectConfig = projectConfig;

            this.blocks = blocks;
            this.blockSuccessors = new Dictionary<uint, uint>();
            for (int blockNum = 0; blockNum < blocks.Count - 1; blockNum++)
            {
                this.blockSuccessors[blocks[blockNum].Id] = blocks[blockNum + 1].Id;
            }

            this.conditions = new List<Pair<Expression, uint>>();
            this.operandExpressions = new Dictionary<Operand, Expression>();

            this.variables = new HashSet<Expression>();
            this.addressTaken = new Dictionary<Expression, Expression>();

            this.arrayVariables = new HashSet<Expression>();
            this.arrayDimensions = new Dictionary<Expression, List<uint>>();

            this.aliasTable = new Dictionary<Expression, Expression>();
            this.aggregateOffsetTable = new Dictionary<Expression, Pair<Expression, Expression>>();

            this.arrayAccesses = new List<Pair<Expression, List<uint>>>();
            this.temporaryIndexExpressions = new Dictionary<uint, Expression>();

            this.temporaryVariableCounter = 0;
            this.temporaryIndexCounter = 0;
            this.temporaryPointerCounter = 0;
        }

        #endregion

        #region Basic Block Methods

        /// <summary>
        /// Determines if <paramref name="block"/> is present after
        /// <paramref name="otherBlock"/> on this Path.
        /// </summary>
        /// 
        /// <param name="block">Block to check for.</param>
        /// <param name="otherBlock">Block to check against.</param>
        /// <returns>True if <paramref name="block"/> is located before
        /// <paramref name="otherBlock"/> on this Path, false otherwise.</returns>
        public bool IsLocatedAfter(BasicBlock block, BasicBlock otherBlock)
        {
            return this.blocks.IndexOf(block) >= this.blocks.IndexOf(otherBlock);
        }

        /// <summary>
        /// Identifies the edge that corresponds to the input conditional Expression
        /// along this path.
        /// </summary>
        /// 
        /// <param name="conditionIndex">Index of the conditional Expression in the list of
        /// conditional Expressions maintained by this path.</param>
        /// <returns>Pair of the IDs of the basic blocks incident to the edge that corresponds
        /// to the input conditional Expression.</returns>
        public Pair<uint, uint> IdentifyEdge(int conditionIndex)
        {
            uint predBlockId, succBlockId = 0;

            /* Get the ID of the predecessor basic block. */
            predBlockId = this.conditions[conditionIndex].Second;

            /* Get the ID of the successor basic block. If the ID of
             * the predecessor basic block is 0, then no basic blocks are
             * associated with the condition. */
            succBlockId = (predBlockId == 0) ? 0 : this.blockSuccessors[predBlockId];

            return new Pair<uint, uint>(predBlockId, succBlockId);
        }

        #endregion

        #region Condition Generation Methods

        /// <summary>
        /// Initializes the BasicBlockAddendum for each of the basic blocks along this path.
        /// </summary>
        private void InitializeBasicBlockAddenda()
        {
            foreach (BasicBlock basicBlock in this.Blocks)
            {
                basicBlock.AddExtensionObject(new BasicBlockAddendum());
            }
        }

        /// <summary>
        /// Generates the conditions and assignments that must be true to traverse this path.
        /// </summary>
        public void GenerateConditionsAndAssignments()
        {
            Instruction lastInstruction;

            Console.Out.WriteLine("PHOENIX: Initializing the addenda for the basic blocks...");
            this.InitializeBasicBlockAddenda();
            Console.Out.WriteLine("PHOENIX: Addenda initialized.");

            Console.Out.WriteLine("PHOENIX: Walking through the basic blocks in the graph...");

            uint numBasicBlocks = (uint) blocks.Count;
            Console.Out.WriteLine("PHOENIX: Processing " + numBasicBlocks + " basic blocks...");
            Console.Out.WriteLine("PHOENIX: Every dot represents 5 basic blocks processed.");

            // Debugger.Break();

            uint basicBlockCount = 1;
            foreach (BasicBlock basicBlock in this.blocks)
            {
                BasicBlockAddendum basicBlockAddendum =
                    basicBlock.FindExtensionObject(typeof(BasicBlockAddendum))
                    as BasicBlockAddendum;
                int id = (int) basicBlock.Id;

                /* Find the instructions in the basic block that correspond to
                 * source-level assignments. */
                Instruction instruction = basicBlock.FirstInstruction;

                while (instruction != null && instruction.BasicBlock == basicBlock)
                {
                    if (!instruction.IsLabelInstruction &&
                        instruction.InstructionKind == InstructionKind.ValueInstruction)
                    {
                        Operand destOperand = instruction.DestinationOperand;
                        ValueInstruction valueInstruction = instruction.AsValueInstruction;

                        /* Assignments to temporary variables are not actually
                         * assignments in the original source code. */
                        if (!destOperand.IsTemporary)
                        {
                            Expression destExpr =
                                ExecutionHelper.TraceOperandBackward(destOperand,
                                    destOperand, this);
                            Expression sourceExpr =
                                ExecutionHelper.ExecuteValueInstructionBackward(valueInstruction,
                                    destOperand, true, this);
                            List<Expression> assignExprs =
                                this.GenerateAndLogAssignment(destExpr, sourceExpr, basicBlock);
                            foreach (Expression assignExpr in assignExprs)
                            {
                                this.AddCondition(assignExpr, basicBlock.Id);
                            }

                            if (this.projectConfig.debugConfig.DUMP_INSTRUCTION_TRACE)
                            {
                                string messageToPrint = "\n";
                                messageToPrint += "*** ASSIGNMENT TO CONVERT ***" + "\n";
                                messageToPrint += instruction.ToString() + "\n";
                                messageToPrint += "*** ON LINE " + instruction.GetLineNumber();

                                if (assignExprs.Count > 0)
                                {
                                    messageToPrint += " CONVERTED TO ***" + "\n";
                                    foreach (Expression assignExpr in assignExprs)
                                    {
                                        messageToPrint += "   " + assignExpr.Value + "\n";
                                    }
                                }
                                else
                                {
                                    messageToPrint += " STORED IN TABLE ***" + "\n";
                                }

                                Console.Out.WriteLine(messageToPrint);
                            }
                        }
                    }

                    if (!instruction.IsLabelInstruction &&
                        instruction.InstructionKind == InstructionKind.CallInstruction)
                    {
                        string functionName =
                            PhoenixHelper.GetOperandName(instruction.SourceOperand1);
                        if (functionName == this.config.ANNOTATE_ASSUME)
                        {
                            Operand assumeOperand = instruction.SourceOperand2;
                            Expression assumeExpr =
                                ExecutionHelper.TraceOperandBackward(assumeOperand,
                                    assumeOperand, this);
                            assumeExpr = ExpressionHelper.UpdateExpression(assumeExpr,
                                basicBlockAddendum, false, this);
                            /* Check that the assumption is true. */
                            assumeExpr = new Expression(OperatorStore.NotEqualOp,
                                assumeExpr, new Constant(0, assumeExpr.BitSize),
                                assumeExpr.BitSize);

                            if (this.projectConfig.debugConfig.DUMP_INSTRUCTION_TRACE)
                            {
                                string messageToPrint = "\n";
                                messageToPrint += "*** ASSUMPTION TO CONVERT ***" + "\n";
                                messageToPrint += instruction.ToString() + "\n";
                                messageToPrint += "*** ON LINE " + instruction.GetLineNumber() +
                                    " CONVERTED TO ***" + "\n";
                                messageToPrint += assumeExpr.ToString() + "\n";
                                Console.Out.WriteLine(messageToPrint);
                            }

                            this.AddCondition(assumeExpr, basicBlock.Id);
                        }
                    }

                    instruction = instruction.Next;
                }

                if (basicBlock.SuccessorCount > 1)
                {
                    lastInstruction = basicBlock.LastInstruction;
                    Trace.Assert(!lastInstruction.IsSwitchInstruction,
                        "PHOENIX: Support for switch-case statements have not been added. " +
                        "The CIL preprocessing phase should have converted these " +
                        "statements into the corresponding if-statements.");

                    Expression newConditionExpr =
                        ExecutionHelper.TraceOperandBackward(lastInstruction.SourceOperand,
                            lastInstruction.SourceOperand, this);
                    newConditionExpr = ExpressionHelper.UpdateExpression(newConditionExpr,
                        basicBlockAddendum, false, this);

                    if (this.projectConfig.debugConfig.DUMP_INSTRUCTION_TRACE)
                    {
                        string messageToPrint = "\n";
                        messageToPrint += "*** INSTRUCTION TO CONVERT ***" + "\n";
                        messageToPrint += lastInstruction.ToString() + "\n";
                        messageToPrint += "*** ON LINE " + lastInstruction.GetLineNumber() +
                            " CONVERTED TO ***" + "\n";
                        messageToPrint += newConditionExpr.ToString() + "\n";
                        Console.Out.WriteLine(messageToPrint);
                    }

                    this.AddCondition(newConditionExpr, basicBlock.Id);
                }

                if (basicBlockCount % 5 == 0)
                {
                    Console.Write(".");
                    if (basicBlockCount % 50 == 0)
                    {
                        /* Add a newline after 10 dots to make them easier to count. */
                        Console.Out.WriteLine();
                    }
                }
                
                basicBlockCount += 1;
            }

            Console.Out.WriteLine();
            Console.Out.WriteLine("PHOENIX: Walk completed.");

            if (this.conditions.Count == 0)
            {
                Expression trueExpr = new Expression(OperatorStore.TrueOp,
                    this.config.WORD_BITSIZE);
                this.AddCondition(trueExpr, this.blocks[0].Id);
            }

            /* Use the list of conditions and assignments to find array variables
             * and their depths. */
            Console.Out.WriteLine("PHOENIX: Finding array variables and dimensions...");
            foreach (Expression arrayVarExpr in this.arrayVariables)
            {
                ExpressionHelper.GetArrayDimensions(arrayVarExpr, this);
            }
            Console.Out.WriteLine("PHOENIX: Array variables and dimensions found.");

            /* Use the list of conditions and assignments to modify array accesses so that
             * their indices are temporary variables. */
            Console.Out.WriteLine("PHOENIX: Replacing the indices of array accesses " +
                "with temporary indices...");
            this.ReplaceIndices();
            Console.Out.WriteLine("PHOENIX: Array accesses modified.");

            /* Use the list of conditions and assignments to find and store
             * array access information. */
            Console.Out.WriteLine("PHOENIX: Storing array access information...");
            foreach (Pair<Expression, uint> conditionPair in this.conditions)
            {
                Expression condition = conditionPair.First;
                this.AddArrayAccesses(ExpressionHelper.FindArrayAccesses(condition, this));
            }
            Console.Out.WriteLine("PHOENIX: Array access information stored.");

            /* Convert the array accesses to array selection Expressions. */
            Console.Out.WriteLine("PHOENIX: Converting array accesses to array selection " +
                "expressions...");
            this.ConvertArrayAccesses();
            Console.Out.WriteLine("PHOENIX: Array accesses converted.");

            /* Now that we have the conditions, we have to post-process them to account for
             * a few special cases. */
            Console.Out.WriteLine("PHOENIX: Adding conditions that prevent division by zero " +
                "and modulo by zero...");
            this.AddNotEqualToZeroConditions();
            Console.Out.WriteLine("PHOENIX: Conditions added.");
        }

        /// <summary>
        /// Logs the assignment in the addendum of the basic block where it was made. If
        /// the expression that is assigned to refers to a pointer variable, then
        /// the assignment is also logged in the offset table. This function also generates
        /// the conditional Expressions that correspond to the assignment.
        /// </summary>
        /// 
        /// <param name="destExpr">Expression that represents the destination of
        /// the assignment.</param>
        /// <param name="sourceExpr">Expression that represents the source of
        /// the assignment.</param>
        /// <param name="basicBlock">Basic block that contains the assignment.</param>
        /// <returns>List of conditional Expressions that correspond to the assignment.</returns>
        public List<Expression> GenerateAndLogAssignment(Expression destExpr,
            Expression sourceExpr, BasicBlock basicBlock)
        {
            List<Expression> result = new List<Expression>();

            BasicBlockAddendum basicBlockAddendum =
                basicBlock.FindExtensionObject(typeof(BasicBlockAddendum)) as BasicBlockAddendum;

            Expression newDestExpr = destExpr;
            Expression newSourceExpr = sourceExpr;

            if (destExpr.Op.Type == OperatorType.Concatenate)
            {
                /* If the destination Expression is a concatenation Expression, separate
                 * the Expressions that are being concatenated into different assignment
                 * statements. To obtain the source Expression for each assignment statement,
                 * extract the appropriate bits of the original source Expression. */
                Expression firstExpr = destExpr.GetParameter(0);
                Expression secondExpr = destExpr.GetParameter(1);

                List<Expression> secondExprAssignments = GenerateAndLogAssignment(secondExpr,
                    ExpressionHelper.ExtractBits(sourceExpr, 0, secondExpr.BitSize - 1, this),
                    basicBlock);
                result.AddRange(secondExprAssignments);

                List<Expression> firstExprAssignments = GenerateAndLogAssignment(firstExpr,
                    ExpressionHelper.ExtractBits(sourceExpr, secondExpr.BitSize,
                        sourceExpr.BitSize - 1, this),
                    basicBlock);
                result.AddRange(firstExprAssignments);

                return result;
            }
            else if (destExpr.Op.Type == OperatorType.ZeroExtend ||
                destExpr.Op.Type == OperatorType.SignExtend)
            {
                /* If the destination Expression is a zero-extended or sign-extended Expression,
                 * then an Expression of some bit-size is being assigned to an Expression that
                 * represents a memory location of smaller bit-size. Recurse on the Expression
                 * that is being extended. To obtain the source Expression for the recursive call,
                 * extract the appropriate bits of the original source Expression. The other bits
                 * can be ignored, since they will not fit into the memory location anyway. */
                Expression toExtendExpr = destExpr.GetParameter(0);
                Expression extensionAmountExpr = destExpr.GetParameter(1);
                uint extensionAmount =
                    (uint)ExpressionHelper.ConvertToConstant(extensionAmountExpr);
                return GenerateAndLogAssignment(toExtendExpr,
                    ExpressionHelper.ExtractBits(sourceExpr, 0, extensionAmount - 1, this),
                    basicBlock);
            }
            else if (destExpr.Op.Type == OperatorType.BitExtract)
            {
                /* If the destination Expression is a bit-extraction Expression, then
                 * a range of bits in the destination Expression will be replaced by
                 * the bits from the source Expression, while the other bits will remain
                 * the same. Recurse on the Expression that is being extracted from.
                 * To obtain the source Expression for the recursive call, extract
                 * the unchanging bits of the destination Expression and concatenate them
                 * with the original source Expression. */
                Expression toExtractFromExpr = destExpr.GetParameter(0);
                Expression startIndexExpr = destExpr.GetParameter(1);
                uint startIndex = (uint)ExpressionHelper.ConvertToConstant(startIndexExpr);
                Expression endIndexExpr = destExpr.GetParameter(2);
                uint endIndex = (uint)ExpressionHelper.ConvertToConstant(endIndexExpr);

                if (startIndex > 0)
                {
                    sourceExpr = new Expression(OperatorStore.ConcatenateOp,
                        sourceExpr,
                        ExpressionHelper.ExtractBits(toExtractFromExpr, 0, startIndex - 1, this),
                        sourceExpr.BitSize + startIndex);
                }
                if (endIndex < toExtractFromExpr.BitSize - 1)
                {
                    sourceExpr = new Expression(OperatorStore.ConcatenateOp,
                        ExpressionHelper.ExtractBits(toExtractFromExpr, endIndex + 1,
                            toExtractFromExpr.BitSize - 1, this),
                        sourceExpr,
                        toExtractFromExpr.BitSize);
                }
                return GenerateAndLogAssignment(toExtractFromExpr, sourceExpr, basicBlock);
            }
            else if (destExpr.Op.Type == OperatorType.Ite)
            {
                /* If the destination Expression is an if-then-else Expression, then
                 * the assignment should only occur if the conditional Expression is satisfied.
                 * Recurse on the Expression that would be the destination Expression if
                 * the conditional Expression were true. For the recursive call, the source
                 * Expression would be another if-then-else Expression that returns
                 * the original source Expression if the conditional Expression were true.
                 * Otherwise, the destination Expression (of the recursive call) should
                 * be assigned to itself. */
                Expression conditionExpr = destExpr.GetParameter(0);
                Expression consequentExpr = destExpr.GetParameter(1);
                Expression alternativeExpr = destExpr.GetParameter(2);

                Expression iteDestExpr = consequentExpr;

                List<Expression> iteParamList = new List<Expression>();
                iteParamList.Add(conditionExpr);
                iteParamList.Add(sourceExpr);
                iteParamList.Add(iteDestExpr);
                Expression iteSourceExpr = new Expression(OperatorStore.IteOp,
                    iteParamList, sourceExpr.BitSize);

                result.AddRange(GenerateAndLogAssignment(iteDestExpr, iteSourceExpr, basicBlock));

                /* Also recurse on the alternative Expression, which would be the destination
                 * Expression if the condition were false. */
                result.AddRange(GenerateAndLogAssignment(alternativeExpr, sourceExpr, basicBlock));
                return result;
            }
            else if (ExpressionHelper.IsPointerExpression(destExpr))
            {
                /* The variable that is assigned to is a pointer. Prepare the source and
                 * the destination Expressions for logging in the alias table. */

                /* Update the indices in the source and destination expressions. Only the indices
                 * are updated since we want the accesses to be made on the original arrays. */
                newDestExpr = ExpressionHelper.UpdateExpression(destExpr,
                    basicBlockAddendum, true, this);
                newSourceExpr = ExpressionHelper.UpdateExpression(sourceExpr,
                    basicBlockAddendum, true, this);

                /* Log the relationship between the destination Expression and the new source
                 * Expression in the alias table. */
                this.AddToAliasTable(newDestExpr, newSourceExpr);
                return result;
            }
            else if (ExpressionHelper.IsAggregateExpression(destExpr))
            {
                /* If the destination and source Expressions are aggregates, then at the level of
                 * the source code, an aggregate is being assigned to another, with the former
                 * potentially being cast to an aggregate of another type. Log this assignment in
                 * the aggregate offset table. */
                this.AddToAggregateOffsetTable(newDestExpr,
                    ((newSourceExpr.Op.Type == OperatorType.Offset) ?
                        newSourceExpr.GetParameter(0) : newSourceExpr),
                    ((newSourceExpr.Op.Type == OperatorType.Offset) ?
                        newSourceExpr.GetParameter(1) : new Constant(0, this.Config.WORD_BITSIZE)));
                return result;
            }

            /* Update the source expression to account for any assignments made to
             * the variables previously. */
            newSourceExpr = ExpressionHelper.UpdateExpression(newSourceExpr,
                basicBlockAddendum, false, this);

            /* Find the Expression for the variable that is being assigned. If the destination
             * Expression is an array access, then the Expression for the variable that is
             * being assigned is the Expression for the array variable. */
            Expression assignedExpr = (newDestExpr.Op.Type == OperatorType.Array) ?
                ExpressionHelper.GetArrayVariable(newDestExpr) : newDestExpr;

            /* Increment the number of assignments for the variable Expression that is assigned
             * in the addenda for the current basic block and for the later basic blocks
             * along this path. */
            uint currentBlockId = basicBlock.Id;
            BasicBlock currentBlock = null;
            BasicBlockAddendum currentBlockAddendum = null;
            while (this.blockSuccessors.ContainsKey(currentBlockId))
            {
                Node currentNode =
                    this.sourceBlock.FunctionUnit.FlowGraph.Node((uint) currentBlockId);
                currentBlock = currentNode.AsBasicBlock;
                currentBlockAddendum =
                    currentBlock.FindExtensionObject(typeof(BasicBlockAddendum))
                    as BasicBlockAddendum;
                currentBlockAddendum.IncrementNumAssignments(assignedExpr);

                currentBlockId = this.blockSuccessors[currentBlockId];
            }

            /* Update the destination Expression to account for the current assignment. */
            newDestExpr = ExpressionHelper.UpdateExpression(newDestExpr,
                basicBlockAddendum, false, this);

            Expression assignExpr = (newDestExpr.Op.Type == OperatorType.Array) ?
                GenerateArrayAssignment(newDestExpr, newSourceExpr, this) :
                new Expression(OperatorStore.EqualOp, newDestExpr, newSourceExpr);
            result.Add(assignExpr);
            return result;
        }

        /// <summary>
        /// Private method for <see cref="GenerateAndLogAssignment"/> that constructs
        /// an array storage Expression that assigns another Expression
        /// to an element of an array.
        /// </summary>
        /// 
        /// <param name="destExpr">Expression that represents the destination of
        /// the assignment.</param>
        /// <param name="sourceExpr">Expression that represents the source of
        /// the assignment.</param>
        /// <param name="path">Path that contains the two Expressions.</param>
        /// <returns>Array storage Expression that assigns another Expression
        /// to an element of an array.</returns>
        /// <remarks>Precondition: The destination of the assignment is
        /// an element of an array.</remarks>
        private Expression GenerateArrayAssignment(Expression destExpr,
            Expression sourceExpr, Path path)
        {
            Trace.Assert(destExpr.Op.Type == OperatorType.Array,
                "PHOENIX: Cannot generate an Expression that assigns to an array " +
                "if the destination provided is not an array.");
            Expression arrayVarExpr = ExpressionHelper.GetArrayVariable(destExpr);

            Expression arrayPrevVersionExpr =
                BasicBlockAddendum.GetPreviousVersion(arrayVarExpr, path);
            Expression destPrevVersionExpr = destExpr.Replace(arrayVarExpr, arrayPrevVersionExpr);

            return new Expression(OperatorStore.EqualOp,
                arrayVarExpr,
                ExpressionHelper.CreateArrayStore(destPrevVersionExpr, sourceExpr, path));
        }

        /// <summary>
        /// Adds a new conditional Expression to the list of conditional Expressions that
        /// must be true to traverse this path.
        /// </summary>
        /// 
        /// <param name="condition">New conditional Expression.</param>
        /// <param name="basicBlockId">ID of the basic block that contains the new conditional
        /// Expression.</param>
        public void AddCondition(Expression condition, uint basicBlockId)
        {
            if (condition != null)
            {
                Pair<Expression, uint> conditionPair =
                    new Pair<Expression, uint>(condition, basicBlockId);
                this.conditions.Add(conditionPair);
            }
        }

        #endregion

        #region Condition Augmentation Methods

        /// <summary>
        /// Finds the array accesses in the conditions and the assignments along this path,
        /// and replaces the index Expressions with temporary variable Expressions. This is
        /// useful when the model that results from a satisfiable query is parsed, so that
        /// the parser will not have to deal with evaluating arbitrarily complicated expressions.
        /// Extra conditions are also added to establish equality between the original indices
        /// and these temporary indices.
        /// 
        /// <remarks>This function has a side-effect: the array accesses are logged in
        /// the instance variable <see cref="ArrayAccesses"/>.
        /// </remarks>
        /// </summary>
        private void ReplaceIndices()
        {
            List<Pair<Expression, uint>> newConditions = new List<Pair<Expression, uint>>();

            for (int i = 0; i < this.conditions.Count; i++)
            {
                Pair<Expression, uint> conditionPair = this.conditions[i];
                Expression condition = conditionPair.First;
                uint conditionBlockId = conditionPair.Second;

                Pair<Expression, List<Expression>> conditionResult =
                    ExpressionHelper.ReplaceIndices(condition, this);
                Expression newCondition = conditionResult.First;
                this.conditions[i] = new Pair<Expression, uint>(newCondition, conditionBlockId);

                List<Expression> supplementaryConditions = conditionResult.Second;
                foreach (Expression supplementaryCondition in supplementaryConditions)
                {
                    Pair<Expression, uint> newConditionPair =
                        new Pair<Expression, uint>(supplementaryCondition, conditionBlockId);
                    newConditions.Add(newConditionPair);
                }
            }

            foreach (Pair<Expression, uint> newCondition in newConditions)
            {
                this.AddCondition(newCondition.First, newCondition.Second);
            }
        }

        /// <summary>
        /// Adds the conditional Expressions that use the conditional Expressions along this path
        /// to ensure that the divisor of a division Expression, or that the modulus of a remainder
        /// Expression, is not set to zero by the SMT solver.
        /// </summary>
        private void AddNotEqualToZeroConditions()
        {
            List<Pair<Expression, uint>> newConditions = new List<Pair<Expression, uint>>();

            for (int i = 0; i < this.conditions.Count; i++)
            {
                Pair<Expression, uint> conditionPair = this.conditions[i];
                Expression currentCondition = conditionPair.First;
                uint currentConditionBlockId = conditionPair.Second;

                List<Expression> notEqualToZeroConditions =
                    ExpressionHelper.GenerateNotEqualToZeroConditions(currentCondition);
                foreach (Expression newExpr in notEqualToZeroConditions)
                {
                    Pair<Expression, uint> newConditionPair =
                        new Pair<Expression, uint>(newExpr, currentConditionBlockId);
                    newConditions.Add(newConditionPair);
                }
            }

            foreach (Pair<Expression, uint> condition in newConditions)
            {
                this.AddCondition(condition.First, condition.Second);
            }
        }

        /// <summary>
        /// Returns a new temporary variable Expression with the input bit-size.
        /// </summary>
        /// 
        /// <param name="bitSize">Bit-size of the new temporary variable Expression.</param>
        /// <returns>New temporary variable Expression with the input bit-size.</returns>
        public Expression GetNewTemporaryVariable(uint bitSize)
        {
            Expression result = new Expression(OperatorStore.VariableOp,
                this.config.IDENT_TEMPVAR + this.temporaryVariableCounter, bitSize);
            this.temporaryVariableCounter++;
            return result;
        }

        #endregion

        #region Variable, Array and Pointer Methods

        /// <summary>
        /// Adds the input variable Expression to the appropriate list of variable Expressions
        /// of the path: if the input Expression is a variable Expression, it is added to
        /// the list of variable Expressions; if the input Expression is an array variable
        /// Expression, it is added to the list of array variable Expressions. The addition only
        /// happens if the Expression is not already in its destination list.
        /// </summary>
        /// <param name="variable">Variable Expression to add.</param>
        public void AddVariable(Expression variable)
        {
            if (variable.Op.Type == OperatorType.ArrayVariable)
            {
                if (!this.arrayVariables.Contains(variable)) { this.arrayVariables.Add(variable); }
            }
            else if (variable.Op.Type == OperatorType.Variable)
            {
                if (!this.variables.Contains(variable)) { this.variables.Add(variable); }
            }
        }

        /// <summary>
        /// Adds the dimensions of the array that the input array variable Expression refers to
        /// into a dictionary that maps array variable Expressions to their dimensions, but only
        /// if the mapping does not already exist.
        /// </summary>
        /// 
        /// <param name="arrayVarExpr">Array variable Expression.</param>
        /// <param name="dimensions">Dimensions of the array that
        /// <paramref name="arrayVarExpr"/> refers to.</param>
        public void AddArrayDimensions(Expression arrayVarExpr, List<uint> dimensions)
        {
            if (!this.arrayDimensions.ContainsKey(arrayVarExpr))
            {
                this.arrayDimensions.Add(arrayVarExpr, dimensions);
            }
        }

        /// <summary>
        /// Returns a new temporary index variable Expression that will replace the index
        /// in an existing array access.
        /// </summary>
        /// 
        /// <param name="bitSize">Bit-size of the new variable Expression.</param>
        /// <returns>New temporary index variable Expression with the input bit-size.</returns>
        public Expression GetNewTemporaryIndex(uint bitSize)
        {
            Expression result = new Expression(OperatorStore.VariableOp,
                this.config.IDENT_TEMPINDEX + this.temporaryIndexCounter, bitSize);
            this.AddVariable(result);
            this.temporaryIndexCounter++;
            return result;
        }

        /// <summary>
        /// Returns a new temporary pointer variable Expression.
        /// </summary>
        /// 
        /// <param name="pointerType">Type of the pointer.</param>
        /// <returns>New temporary pointer variable Expression.</returns>
        public Expression GetNewTemporaryPointer(Phx.Types.Type pointerType)
        {
            Trace.Assert(ExpressionHelper.IsPointerExpressionType(pointerType),
                "PHOENIX: Temporary pointers can only be constructed with pointer types.");

            Expression result = new Expression(OperatorStore.ArrayVariableOp,
                this.config.IDENT_TEMPPTR + this.temporaryPointerCounter,
                this.Config.WORD_BITSIZE);
            result.Type = pointerType;

            this.AddVariable(result);
            this.temporaryPointerCounter++;

            return ExpressionHelper.MakeDereferencingFunction(result, this);
        }

        /// <summary>
        /// Adds the input array accesses to the list of array accesses of the path, but only
        /// those array accesses that are not already in the list.
        /// </summary>
        /// 
        /// <param name="arrayAccesses">Array accesses to be added.</param>
        private void AddArrayAccesses(List<Pair<Expression, List<uint>>> arrayAccesses)
        {
            foreach (Pair<Expression, List<uint>> arrayAccess in arrayAccesses)
            {
                /* Ignore all array accesses that do not have as many indices as the array
                 * that is being accessed has dimensions. */
                Expression arrayVariableExpr = arrayAccess.First;
                List<uint> arrayAccessIndices = arrayAccess.Second;

                List<uint> dimensions = this.arrayDimensions[arrayVariableExpr];
                if (arrayAccessIndices.Count + 1 == dimensions.Count &&
                    !this.ArrayAccesses.Contains(arrayAccess))
                {
                    this.arrayAccesses.Add(arrayAccess);
                }
            }
        }

        /// <summary>
        /// Converts array access Expressions in the conditions along this path to
        /// the appropriate array selection Expressions.
        /// </summary>
        private void ConvertArrayAccesses()
        {
            for (int i = 0; i < this.conditions.Count; i++)
            {
                Pair<Expression, uint> conditionPair = this.conditions[i];
                Expression conditionExpr = conditionPair.First;
                uint conditionBlock = conditionPair.Second;

                Expression newConditionExpr =
                    ExpressionHelper.ConvertArrayAccesses(conditionExpr, this);
                this.conditions[i] = new Pair<Expression, uint>(newConditionExpr, conditionBlock);
            }
        }

        #endregion

        #region Table Methods

        /// <summary>
        /// Adds a new entry to the alias table that maps an Expression to the Expression that it
        /// is an alias for. If there is already an entry for the Expression, the entry is updated.
        /// </summary>
        /// 
        /// <param name="aliasExpr">Expression to add to the alias table.</param>
        /// <param name="expr">Expression that it is an alias for.</param>
        public void AddToAliasTable(Expression aliasExpr, Expression expr)
        {
            if (this.projectConfig.debugConfig.DUMP_INSTRUCTION_TRACE)
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine("  Adding a new entry to the alias table: ");

                string aliasTableEntry = "  " + aliasExpr.ToString() + "\n";
                aliasTableEntry += "  = ";
                aliasTableEntry += expr.ToString();
                Console.Out.WriteLine(aliasTableEntry);
                Console.Out.WriteLine();
            }

            this.aliasTable[aliasExpr] = expr;
        }

        /// <summary>
        /// Returns the Expression that the input Expression is an alias for. If the input
        /// Expression is not an alias for another Expression, the function returns the input
        /// Expression itself.
        /// </summary>
        /// 
        /// <param name="aliasExpr">Expression that may be an alias for another Expression.</param>
        /// <returns>Expression that the input Expression is an alias for, if any; the input
        /// Expression itself, otherwise.</returns>
        public Expression FindAliasedExpression(Expression aliasExpr)
        {
            return this.aliasTable.ContainsKey(aliasExpr) ? this.aliasTable[aliasExpr] : aliasExpr;
        }

        /// <summary>
        /// Adds a new entry to the offset table that maps an aggregate (struct or union)
        /// to a pair whose first element is a "base aggregate" and whose second element
        /// is the bit offset of the start of the aggregate from the "base aggregate".
        /// If there is already an entry for the pointer, the entry is updated.
        /// </summary>
        /// 
        /// <param name="newAggregate">Aggregate to add to the offset table.</param>
        /// <param name="baseAggregate">"Base aggregate" that the start of
        /// <paramref name="newAggregate"/> is offset from.</param>
        /// <param name="bitOffsetExpr">Offset of the start of <paramref name="newAggregate"/>
        /// from <paramref name="baseAggregate"/> (in bits).</param>
        public void AddToAggregateOffsetTable(Expression newAggregate,
            Expression baseAggregate, Expression bitOffsetExpr)
        {
            /* Add the bit offset to any bit offset that may already exist in the table. */
            Pair<Expression, Expression> prevBaseAndOffset =
                this.FindAggregateBaseAndOffset(baseAggregate);
            Expression origBaseAggregate = prevBaseAndOffset.First;
            Expression prevOffsetExpr = prevBaseAndOffset.Second;
            bitOffsetExpr = new Expression(OperatorStore.AddOp,
                prevOffsetExpr, bitOffsetExpr, prevOffsetExpr.BitSize);
            bitOffsetExpr.Type = prevOffsetExpr.Type;
            bitOffsetExpr = ExpressionHelper.SimplifyExpression(bitOffsetExpr, this);

            if (this.projectConfig.debugConfig.DUMP_INSTRUCTION_TRACE)
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine("  Adding a new entry to the aggregate offset table: ");
                
                string aggregateOffsetEntry = "  " + newAggregate.ToString().PadRight(18);
                aggregateOffsetEntry += " = ";
                aggregateOffsetEntry += origBaseAggregate.ToString() + ", ";
                aggregateOffsetEntry += bitOffsetExpr.ToString();
                Console.Out.WriteLine(aggregateOffsetEntry);
                Console.Out.WriteLine();
            }

            aggregateOffsetTable[newAggregate] =
                new Pair<Expression, Expression>(origBaseAggregate, bitOffsetExpr);
        }

        /// <summary>
        /// Returns a Pair whose first element is the "base aggregate" that the start of
        /// the input aggregate is offset from, and whose second element is the bit offset.
        /// </summary>
        /// 
        /// <param name="aggregate">Aggregate whose "base aggregate" and offset
        /// is to be returned.</param>
        /// <returns>Pair as described.</returns>
        public Pair<Expression, Expression> FindAggregateBaseAndOffset(Expression aggregate)
        {
            return aggregateOffsetTable.ContainsKey(aggregate) ?
                aggregateOffsetTable[aggregate] :
                new Pair<Expression, Expression>(aggregate,
                    new Constant(0, this.config.WORD_BITSIZE));
        }

        #endregion

        #region Path Information Methods

        /// <summary>
        /// Dump the line numbers of the source-level statements along this path to a file
        /// in the temporary GameTime directory.
        /// </summary>
        public void DumpLineNumbers()
        {
            /* Use a hash set initially to ignore duplicate line numbers. */
            HashSet<uint> lineNumbers = new HashSet<uint>();

            foreach (BasicBlock basicBlock in this.blocks)
            {
                Instruction instruction = basicBlock.FirstInstruction;

                while (instruction != null && instruction.BasicBlock == basicBlock)
                {
                    lineNumbers.Add(instruction.GetLineNumber());
                    instruction = instruction.Next;
                }
            }

            /* Now, add the unique line numbers to a list and sort. */
            List<uint> lineNumberList = new List<uint>();
            lineNumberList.AddRange(lineNumbers);
            lineNumberList.Sort();

            string lineNumbersFileName =
                System.IO.Path.Combine(projectConfig.locationTempDir,
                config.TEMP_PATH_LINE_NUMBERS);
            StreamWriter lineNumbersWriter = new StreamWriter(lineNumbersFileName, false);
            lineNumbersWriter.AutoFlush = true;

            foreach (uint lineNumber in lineNumberList)
            {
                lineNumbersWriter.Write(lineNumber.ToString() + " ");
            }

            lineNumbersWriter.Close();
        }

        /// <summary>
        /// Dump the edges that correspond to the conditions and assignments along this path
        /// to a file in the temporary GameTime directory.
        /// 
        /// <param name="sourceAdjuster">Function that maps the ID of a basic block to
        /// the "adjusted" ID of a node in the directed acyclic graph of the function unit.
        /// This function is used for basic blocks that are sources of edges.</param>
        /// <param name="sinkAdjuster">Function that maps the ID of a basic block to
        /// the "adjusted" ID of a node in the directed acyclic graph of the function unit.
        /// This function is used for basic blocks that are sinks of edges.</param>
        /// </summary>
        public void DumpConditionEdges(Func<uint, uint> sourceAdjuster,
            Func<uint, uint> sinkAdjuster)
        {
            string conditionEdgesFileName =
                System.IO.Path.Combine(projectConfig.locationTempDir,
                config.TEMP_PATH_CONDITION_EDGES);
            StreamWriter conditionEdgesWriter = new StreamWriter(conditionEdgesFileName, false);
            conditionEdgesWriter.AutoFlush = true;

            for (int conditionNum = 0; conditionNum < this.conditions.Count; conditionNum++)
            {
                string conditionEdgesLine = conditionNum + ": ";

                Pair<uint, uint> conditionEdge = IdentifyEdge(conditionNum);
                uint sourceNodeId = conditionEdge.First;
                uint sinkNodeId = conditionEdge.Second;
                if (sourceNodeId != 0)
                {
                    sourceNodeId = sourceAdjuster(sourceNodeId);
                    sinkNodeId = sinkAdjuster(sinkNodeId);
                }
                conditionEdgesLine += sourceNodeId + " " + sinkNodeId;
                
                conditionEdgesWriter.WriteLine(conditionEdgesLine);
            }

            conditionEdgesWriter.Close();
        }

        /// <summary>
        /// Dump the line numbers and truth values of the first lines for each basic block
        /// along this path to a file in the temporary directory created by GameTime.
        /// </summary>
        public void DumpConditionTruths()
        {
            string conditionTruthsFileName =
                System.IO.Path.Combine(projectConfig.locationTempDir,
                config.TEMP_PATH_CONDITION_TRUTHS);
            StreamWriter conditionTruthsWriter = new StreamWriter(conditionTruthsFileName, false);
            conditionTruthsWriter.AutoFlush = true;

            foreach (BasicBlock block in this.blocks)
            {
                BranchInstruction branchInstruction = block.LastInstruction as BranchInstruction;
                if (branchInstruction != null && branchInstruction.IsConditional)
                {
                    /* The last instruction of this basic block is a branch instruction. */
                    uint firstLineNumber = branchInstruction.GetLineNumber();

                    int blockIndex = this.blocks.IndexOf(block);
                    if (blockIndex != -1)
                    {
                        BasicBlock nextBlock = this.blocks[blockIndex + 1];
                        conditionTruthsWriter.WriteLine(firstLineNumber + ": " +
                            ((nextBlock == branchInstruction.TrueLabelInstruction.BasicBlock) ?
                                "True" : "False"));
                    }
                    else
                    {
                        conditionTruthsWriter.WriteLine(firstLineNumber + " False");
                    }
                }
            }

            conditionTruthsWriter.Close();
        }

        /// <summary>
        /// Dump the information about all the array and aggregate accesses made in the conditions
        /// and the assignments along this path to files in the temporary directory
        /// created by GameTime.
        /// </summary>
        public void DumpAccesses()
        {
            string arrayAccessesFileName =
                System.IO.Path.Combine(this.projectConfig.locationTempDir,
                this.config.TEMP_PATH_ARRAY_ACCESSES);
            StreamWriter arrayAccessesWriter = new StreamWriter(arrayAccessesFileName, false);
            arrayAccessesWriter.AutoFlush = true;

            string aggIndexExprsFileName =
                System.IO.Path.Combine(this.projectConfig.locationTempDir,
                this.config.TEMP_PATH_AGG_INDEX_EXPRS);
            StreamWriter aggIndexExprsWriter = new StreamWriter(aggIndexExprsFileName, false);
            aggIndexExprsWriter.AutoFlush = true;

            foreach (Pair<Expression, List<uint>> arrayAccess in this.arrayAccesses)
            {
                /* Dump information about array accesses. */
                Expression arrayVariable = arrayAccess.First;
                string arrayName = arrayVariable.Value;
                List<uint> tempIndices = arrayAccess.Second;

                string arrayAccessToWrite = arrayName + ": [(";
                foreach (uint tempIndex in tempIndices)
                {
                    arrayAccessToWrite += tempIndex + ", ";
                }
                arrayAccessToWrite = arrayAccessToWrite.TrimEnd() + ")]";
                arrayAccessesWriter.WriteLine(arrayAccessToWrite);

                /* Dump information about expressions associated with the temporary indices of
                 * aggregate accesses along a path. */
                string identField = this.config.IDENT_FIELD;
                if (arrayName.StartsWith(identField))
                {
                    uint tempIndex = tempIndices[0];
                    Expression aggIndexExpr = this.temporaryIndexExpressions[tempIndex];

                    OperatorType aggIndexExprOpType = aggIndexExpr.Op.Type;
                    Trace.Assert(aggIndexExprOpType == OperatorType.Variable ||
                        aggIndexExprOpType == OperatorType.Array,
                        "PHOENIX: Only either a variable Expression or an array " +
                        "Expression can be the first index of an aggregate access.");

                    string aggIndexExprToWrite = tempIndex.ToString();
                    aggIndexExprToWrite += ": ";

                    string aggIndexExprVal = aggIndexExpr.Value;
                    aggIndexExprVal = aggIndexExprVal.Replace('[', ' ');
                    aggIndexExprVal = aggIndexExprVal.Replace(']', ' ');
                    aggIndexExprVal = aggIndexExprVal.Replace(this.config.IDENT_TEMPINDEX, "");
                    aggIndexExprToWrite += aggIndexExprVal;

                    aggIndexExprsWriter.WriteLine(aggIndexExprToWrite);
                }
            }

            aggIndexExprsWriter.Close();
            arrayAccessesWriter.Close();
        }

        /// <summary>
        /// Returns the string representation of this path, which contains information such
        /// as the conditions and the array variables along this path.
        /// </summary>
        /// 
        /// <returns>String representation of this path, which contains information such as
        /// the conditions and the array variables along this path.</returns>
        public override string ToString()
        {
            string result = "";
            result += "\n";

            result += "*** Conditions along this path ***\n";
            foreach (Pair<Expression, uint> conditionPair in this.conditions)
            {
                result += conditionPair.First.ToString() + "\n";
            }

            result += "\n";
            result += "*** Variables, sizes, types ***\n";
            foreach (Expression varExpr in this.variables)
            {
                string varInfo = varExpr.Value.PadRight(20);
                varInfo += " ";
                varInfo += varExpr.BitSize.ToString().PadRight(20);
                varInfo += " ";
                varInfo += (varExpr.Type != null ? varExpr.Type.ToString() : "");
                result += varInfo + "\n";
            }

            result += "\n";
            result += "*** Array variables, dimensions, types ***\n";
            foreach (Expression arrayVarExpr in this.arrayVariables)
            {
                string arrayInfo = arrayVarExpr.Value.PadRight(20);
                arrayInfo += " ";

                List<uint> dimensions = this.arrayDimensions[arrayVarExpr];
                string dimensionsString = "";
                foreach (uint dimension in dimensions)
                {
                    dimensionsString += dimension + " ";
                }
                arrayInfo += ("(" + dimensionsString.TrimEnd() + ")").PadRight(20);

                arrayInfo += " " + (arrayVarExpr.Type != null ? arrayVarExpr.Type.ToString() : "");
                result += arrayInfo + "\n";
            }

            result += "\n";
            result += "*** Address-taken variables, pointer replacements, bit-sizes ***\n";
            foreach (Expression addressTakenExpr in this.addressTaken.Keys)
            {
                string addressTakenInfo = "    " + addressTakenExpr.Value + "\n";
                addressTakenInfo += " -> ";

                /* Get the name of the temporary pointer that replaces
                 * the current address-taken variable. */
                Expression tempPtr = addressTaken[addressTakenExpr];
                while (tempPtr.Op.Type == OperatorType.Function)
                {
                    tempPtr = tempPtr.GetParameter(tempPtr.ParameterList.Count - 1);
                }
                /* Get the first parameter of the offset Expression. */
                tempPtr = tempPtr.GetParameter(0);
                /* Get the name of the temporary pointer. */
                string tempPtrName = tempPtr.GetParameter(0).Value;

                addressTakenInfo += tempPtrName.PadRight(70);
                addressTakenInfo += " ";
                addressTakenInfo += addressTakenExpr.BitSize.ToString();

                result += addressTakenInfo + "\n";
            }

            result += "\n";
            result += "*** Operands and bit-sizes; Expressions and bit-sizes ***\n";
            foreach (Operand operand in this.operandExpressions.Keys)
            {
                string operandAndExpr = "    " + operand.ToString().PadRight(70);
                operandAndExpr += " " + operand.BitSize.ToString() + "\n";
                operandAndExpr += " ->";

                Expression operandExpr = this.operandExpressions[operand];
                operandAndExpr += " " + operandExpr.ToString().PadRight(70);
                operandAndExpr += " " + operandExpr.BitSize.ToString() + "\n";

                result += operandAndExpr + "\n";
            }

            result += "\n";
            result += "*** Alias table ***\n";
            foreach (Expression expr in this.AliasTable.Keys)
            {
                string aliasTableEntry = "   " + expr.ToString() + "\n";
                aliasTableEntry += " = ";
                aliasTableEntry += this.AliasTable[expr] + "\n";
                result += aliasTableEntry + "\n";
            }

            result += "\n";
            result += "*** Aggregate offset table ***\n";
            foreach (Expression aggregateExpr in this.AggregateOffsetTable.Keys)
            {
                string aggregateOffsetEntry = aggregateExpr.ToString().PadRight(18);
                aggregateOffsetEntry += " = ";

                Pair<Expression, Expression> baseAggregateAndOffset =
                    this.AggregateOffsetTable[aggregateExpr];
                aggregateOffsetEntry += baseAggregateAndOffset.First.ToString().PadRight(20) + ",";
                aggregateOffsetEntry += " " + baseAggregateAndOffset.Second.ToString();

                result += aggregateOffsetEntry + "\n";
            }

            return result;
        }

        /// <summary>
        /// Dumps the path information to standard output.
        /// </summary>
        public void Dump()
        {
            Console.Out.Write(this.ToString());
        }

        #endregion
    }
}
