using System;
using System.Runtime.InteropServices;

namespace SmartCL
{
    /// <summary>
    /// Pinned handle of object
    /// </summary>
    internal class CLHandle : IDisposable
    {
        #region Fields
        /// <summary>
        /// Associated handle
        /// </summary>
        private GCHandle handle;
        #endregion
        #region Methods
        /// <summary>
        /// Private constructor
        /// </summary>
        /// <remarks>Prevent from creating empty handles</remarks>
        private CLHandle()
        {
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="obj">Object to pin</param>
        public CLHandle(object obj)
        {
            handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
        }
        /// <summary>
        /// Finalizer
        /// </summary>
        ~CLHandle()
        {
            Dispose(disposing: false);
        }
        /// <summary>
        /// IDisposable implementation
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Programmatically disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (handle.IsAllocated)
                handle.Free();
        }
        /// <summary>
        /// Get the pointer of the pinned object
        /// </summary>
        /// <returns></returns>
        unsafe public void* ToPointer()
        {
            return handle.AddrOfPinnedObject().ToPointer();
        }
        /// <summary>
        /// Get the pointer of the pinned object
        /// </summary>
        /// <returns></returns>
        unsafe public T* ToPointer<T>() where T : unmanaged
        {
            return (T*)handle.AddrOfPinnedObject().ToPointer();
        }
        #endregion
    }
}
