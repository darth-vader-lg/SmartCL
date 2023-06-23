using System;
using System.Runtime.CompilerServices;

namespace SmartCL
{
    /// <summary>
    /// Represent the base for OpenCL objects
    /// </summary>
    public abstract class CLObject : IDisposable
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
        /// Finalizer
        /// </summary>
        ~CLObject()
        {
            Dispose(disposing: false);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (ID == 0)
                return;
            ID = 0;
        }
        #endregion
    }
}
