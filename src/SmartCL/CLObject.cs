using System.Runtime.CompilerServices;

namespace SmartCL
{
    /// <summary>
    /// Represent the base for OpenCL objects
    /// </summary>
    public abstract class CLObject
    {
        #region Fields
        /// <summary>
        /// The ID of the object
        /// </summary>
        internal nint ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }
        #endregion
        #region Properties
        /// <summary>
        /// Valid object flag
        /// </summary>
        public bool Valid => ID != 0;
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">The ID of the object</param>
        protected CLObject(nint id)
        {
            ID = id;
        }
        /// <summary>
        /// Invalidate the object
        /// </summary>
        protected virtual void InvalidateObject()
        {
            ID = 0;
        }
        #endregion
    }
}
