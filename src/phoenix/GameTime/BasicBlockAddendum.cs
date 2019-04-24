/*************************************************************************************************
 * BasicBlockAddendum                                                                            *
 * ============================================================================================= *
 * This file contains the class that maintains additional information                            *
 * about a Phoenix basic block.                                                                  *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Utilities;


namespace GameTime
{
    /// <summary>
    /// This class maintains additional information about a Phoenix basic block.
    /// </summary>
    public class BasicBlockAddendum : Phx.Graphs.NodeExtensionObject
    {
        #region Fields and Properties

        /// <summary>
        /// Dictionary that maps a variable name to the number of assignments made to
        /// that variable, if any assignments have been made.
        /// </summary>
        private Dictionary<string, int> varNumAssigns;

        /// <summary>
        /// Gets and sets the dictionary that maps a variable to the number of assignments
        /// made to that variable, if any assignments have been made.
        /// </summary>
        public Dictionary<string, int> VarNumAssigns
        {
            get { return this.varNumAssigns; }
            set { this.varNumAssigns = value; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for the class.
        /// </summary>
        public BasicBlockAddendum()
        {
            this.varNumAssigns = new Dictionary<string, int>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the original variable name that corresponds to the input variable name.
        /// The original name is the name without the annotation that denotes how many
        /// assignments have been made to the variable.
        /// </summary>
        /// 
        /// <param name="variableName">Variable name.</param>
        /// <returns>Original variable name that corresponds to the input variable name.</returns>
        public static string GetOriginalVarName(string variableName)
        {
            string[] varNameSplit = variableName.Split('<');
            return varNameSplit[0];
        }

        /// <summary>
        /// Returns the version of the input variable name, which is the annotation that
        /// denotes how many assignments have been made to the variable. If there is
        /// no such annotation, the method returns 0.
        /// </summary>
        /// 
        /// <param name="variableName">Variable name.</param>
        /// <returns>Version of the input variable name.</returns>
        public static int GetVersion(string variableName)
        {
            string[] varNameSplit = variableName.Split('<');
            return (varNameSplit.Length > 1) ?
                System.Convert.ToInt32(varNameSplit[1].Split('>')[0]) : 0;
        }

        /// <summary>
        /// Returns the number of assignments to a variable, as currently logged.
        /// </summary>
        /// 
        /// <param name="variableExpr">Expression that corresponds
        /// to the variable to check.</param>
        /// <returns>Number of assignments to the variable whose Expression is provided.</returns>
        public int GetNumAssignments(Expression variableExpr)
        {
            string originalVarName = GetOriginalVarName(variableExpr.Value);
            return this.varNumAssigns.ContainsKey(originalVarName) ?
                this.varNumAssigns[originalVarName] : 0;
        }

        /// <summary>
        /// Logs a new assignment to a variable.
        /// </summary>
        /// 
        /// <param name="variableExpr">Expression that corresponds
        /// to the variable to update.</param>
        public void IncrementNumAssignments(Expression variableExpr)
        {
            string originalVarName = GetOriginalVarName(variableExpr.Value);
            this.varNumAssigns[originalVarName] = GetNumAssignments(variableExpr) + 1;
        }

        /// <summary>
        /// Returns the original variable Expression for the input variable Expression.
        /// The original variable Expression has the same name as the input variable, without
        /// the annotation that denotes how many assignments have been made to the variable.
        /// </summary>
        /// 
        /// <param name="variableExpr">Variable Expression.</param>
        /// <returns>Original variable Expression for the input variable Expression.</returns>
        public static Expression GetOriginalVariable(Expression variableExpr)
        {
            string originalVarName = GetOriginalVarName(variableExpr.Value);
            Expression result = new Expression(variableExpr.Op,
                originalVarName, variableExpr.BitSize);
            result.Type = variableExpr.Type;
            return result;
        }

        /// <summary>
        /// Returns an updated version of the input variable Expression, based on
        /// the number of assignments to the variable that have been logged.
        /// </summary>
        /// 
        /// <param name="variableExpr">Expression that corresponds
        /// to the variable to update.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Updated Expression.</returns>
        public Expression GetUpdatedVersion(Expression variableExpr, Path path)
        {
            int numAssignments = this.GetNumAssignments(variableExpr);

            string originalVarName = GetOriginalVarName(variableExpr.Value);
            string updatedVarName =  String.Format("{0}{1}",
                originalVarName,
                ((numAssignments == 0) ? "" : (String.Format("<{0}>", numAssignments))));

            Expression result = new Expression(variableExpr.Op,
                updatedVarName, variableExpr.BitSize);
            result.Type = variableExpr.Type;
            path.AddVariable(result);
            return result;
        }

        /// <summary>
        /// Returns the previous version of the input (updated) variable Expression.
        /// </summary>
        /// 
        /// <param name="variableExpr">Updated variable Expression.</param>
        /// <param name="path">Path that contains the input Expression.</param>
        /// <returns>Previous version of the updated variable Expression.</returns>
        public static Expression GetPreviousVersion(Expression variableExpr, Path path)
        {
            string originalVarName = GetOriginalVarName(variableExpr.Value);
            int currentVersion = GetVersion(variableExpr.Value);

            Trace.Assert(currentVersion > 0,
                "PHOENIX: Cannot find the previous version of a variable that " +
                "has not yet been assigned to.");

            string previousVersionName = String.Format("{0}{1}",
                originalVarName,
                ((currentVersion == 1) ? "" : (String.Format("<{0}>", currentVersion - 1))));

            Expression result = new Expression(variableExpr.Op,
                previousVersionName, variableExpr.BitSize);
            result.Type = variableExpr.Type;
            path.AddVariable(result);
            return result;
        }

        #endregion
    }
}
