using System;

namespace SmartCL
{
    /// <summary>
    /// OpenCL disposable context
    /// </summary>
    public sealed class CLDisposableContext : CLContext, IDisposable
    {
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="devices">Devices set</param>
        /// <param name="id">Context identifier</param>
        private CLDisposableContext(CLDevicesGroup devices, nint id) : base(devices, id)
        {
        }
        /// <summary>
        /// Finalizer
        /// </summary>
        ~CLDisposableContext()
        {
            try {
                InvalidateObject();
            }
            catch (Exception) {
            }
        }
        /// <summary>
        /// Create a context
        /// </summary>
        /// <param name="devices">Devices set</param>
        /// <returns>The context</returns>
        public static CLDisposableContext Create(CLDevicesGroup devices)
        {
            if (devices == null)
                throw new ArgumentNullException(nameof(devices));
            return new(devices, CreateContext(devices));
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            try {
                InvalidateObject();
            }
            catch (Exception) {
            }
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
