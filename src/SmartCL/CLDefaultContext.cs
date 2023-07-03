using System;

namespace SmartCL
{
    /// <summary>
    /// OpenCL default context
    /// </summary>
    internal sealed class CLDefaultContext : CLContext
    {
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="devices">Devices set</param>
        /// <param name="id">Context identifier</param>
        private CLDefaultContext(CLDevicesGroup devices, nint id) : base(devices, id)
        {
        }
        /// <summary>
        /// Create a context
        /// </summary>
        /// <param name="devices">Devices set</param>
        /// <returns>The context</returns>
        public static CLDefaultContext Create(CLDevicesGroup devices)
        {
            if (devices == null)
                throw new ArgumentNullException(nameof(devices));
            return new(devices, CreateContext(devices));
        }
        #endregion
    }
}
