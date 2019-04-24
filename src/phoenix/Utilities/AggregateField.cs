/*************************************************************************************************
 * AggregateField                                                                                *
 * ============================================================================================= *
 * This file contains the class that describes a field                                           *
 * of an aggregate (either a struct or a union).                                                 *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using System.Collections.Generic;

using Phx.Types;


namespace Utilities
{
    /// <summary>
    /// This class describes a field of an aggregate (either a struct or a union).
    /// </summary>
    public class AggregateField
    {
        #region Fields and Properties

        /// <summary>
        /// Type of the aggregate that contains this field.
        /// </summary>
        private AggregateType aggregateType;

        /// <summary>
        /// Gets the type of the aggregate that contains this field.
        /// </summary>
        public AggregateType AggregateType
        {
            get { return this.aggregateType; }
        }

        /// <summary>
        /// Expression that represents an access of this field.
        /// </summary>
        private Expression accessExpr;

        /// <summary>
        /// Gets the Expression that represents an access of this field.
        /// </summary>
        public Expression AccessExpr
        {
            get { return this.accessExpr; }
        }

        /// <summary>
        /// Offset of the start of the field, in bits, from the start of the aggregate that
        /// contains this field.
        /// </summary>
        private int startOffset;

        /// <summary>
        /// Gets the offset of the start of the field, in bits, from the start of the aggregate
        /// that contains this field.
        /// </summary>
        public int StartOffset
        {
            get { return this.startOffset; }
            set { this.startOffset = value; }
        }

        /// <summary>
        /// Size of the field, in bits.
        /// </summary>
        private uint bitSize;

        /// <summary>
        /// Gets the size of the field, in bits.
        /// </summary>
        public uint BitSize
        {
            get { return this.bitSize; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for the AggregateField class, which creates an object that describes
        /// a field of an aggregate (either a struct or a union).
        /// </summary>
        /// 
        /// <param name="aggregateType">Type of the aggregate that contains this field.</param>
        /// <param name="accessExpr">Expression that represents an access of this field.</param>
        /// <param name="startOffset">Offset of the start of the field, in bits, from the start
        /// of the aggregate that contains this field.</param>
        /// <param name="bitSize">Bit-size of the field.</param>
        public AggregateField(AggregateType aggregateType, Expression accessExpr,
            int startOffset, uint bitSize)
        {
            this.aggregateType = aggregateType;
            this.accessExpr = accessExpr;
            this.startOffset = startOffset;
            this.bitSize = bitSize;
        }

        #endregion
    }
}
