using System.Linq;

namespace SmartCL
{
    /// <summary>
    /// Coumputing tensor's dimensions
    /// </summary>
    public class CLDims
    {
        #region Properties
        /// <summary>
        /// Local ranks
        /// </summary>
        public int[] Locals { get; }
        /// <summary>
        /// Global ranks
        /// </summary>
        public int[] Globals { get; }
        /// <summary>
        /// Global offsets
        /// </summary>
        public int[] Offsets { get; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        public CLDims() : this(1)
        {
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ranks">The ranks for each dimensions of the computing tensor</param>
        public CLDims(params int[] ranks)
        {
            Locals = ranks.ToArray();
            Globals = ranks.ToArray();
            Offsets = new int[ranks.Length];
        }
        #endregion
    }
}
