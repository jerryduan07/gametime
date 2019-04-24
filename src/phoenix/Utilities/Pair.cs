/*************************************************************************************************
 * Pair                                                                                          *
 * ============================================================================================= *
 * This file contains the class that describes a pair of elements.                               *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/


namespace Utilities
{
    /// <summary>
    /// This class describes a pair of elements.
    /// </summary>
    /// 
    /// <typeparam name="T">Type of the first element of the pair.</typeparam>
    /// <typeparam name="U">Type of the second element of the pair.</typeparam>
    public class Pair<T, U>
    {
        #region Fields and Properties

        /// <summary>
        /// First element of the pair.
        /// </summary>
        private T first;

        /// <summary>
        /// Gets or sets the first element of the pair.
        /// </summary>
        public T First
        {
            get { return this.first; }
            set { this.first = value; }
        }

        /// <summary>
        /// Second element of the pair.
        /// </summary>
        private U second;

        /// <summary>
        /// Gets or sets the second element of the pair.
        /// </summary>
        public U Second
        {
            get { return this.second; }
            set { this.second = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor that creates a pair with default values.
        /// </summary>
        public Pair()
        {
            this.first = default(T);
            this.second = default(U);
        }

        /// <summary>
        /// Constructor that creates a pair with the elements provided.
        /// </summary>
        /// 
        /// <param name="first">First element of the pair.</param>
        /// <param name="second">Second element of the pair.</param>
        public Pair(T first, U second)
        {
            this.first = first;
            this.second = second;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the string representation of this Pair.
        /// </summary>
        /// 
        /// <returns>String representation of this Pair.</returns>
        public override string ToString()
        {
            return "(" + this.first.ToString() + ", " +
                this.second.ToString() + ")";
        }

        /// <summary>
        /// Checks if this Pair is equal to the input Pair.
        /// </summary>
        /// 
        /// <param name="other">Pair to compare this Pair with.</param>
        /// <returns>True, if this Pair is equal to <paramref name="other" />;
        /// false otherwise.</returns>
        public override bool Equals(object other)
        {
            /* If parameter is null, return false. */
            if (other == null) { return false; }

            /* If parameter cannot be cast as an Expression, return false. */
            Pair<T, U> otherPair = other as Pair<T, U>;
            if ((object)otherPair == null) { return false; }

            return this.first.Equals(otherPair.first) &&
                this.second.Equals(otherPair.second);
        }

        /// <summary>
        /// Returns the hash code of this Pair.
        /// </summary>
        /// 
        /// <returns>Hash code of this Pair.</returns>
        public override int GetHashCode()
        {
            return this.first.GetHashCode() + this.second.GetHashCode();
        }

        #endregion
    }
}
