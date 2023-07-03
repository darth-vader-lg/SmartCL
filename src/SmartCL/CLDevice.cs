using System;
using System.Diagnostics;

namespace SmartCL
{
    /// <summary>
    /// An OpenCL device
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class CLDevice : CLObject
    {
        #region Properties
        /// <summary>
        /// The type of the device
        /// </summary>
        public CLDeviceType DeviceType { get; private set; }
        /// <summary>
        /// The platform
        /// </summary>
        public CLPlatform Platform { get; private set; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="platform">The owner platform</param>
        /// <param name="id">ID of the device</param>
        /// <param name="deviceType">The type of the device</param>
        internal CLDevice(CLPlatform platform, nint id, CLDeviceType deviceType) : base(id)
        {
            Platform = platform ?? throw new ArgumentNullException(nameof(platform));
            DeviceType = deviceType;
        }
        /// <summary>
        /// Create a context
        /// </summary>
        /// <returns>The created context</returns>
        public CLDisposableContext CreateContext() => CLDisposableContext.Create(new(new[] { this }));
        /// <summary>
        /// Debugger visualization
        /// </summary>
        /// <returns>The string</returns>
        private string GetDebuggerDisplay()
        {
            return DeviceType.ToString();
        }
        /// <summary>
        /// Invalidate the object
        /// </summary>
        protected override void InvalidateObject()
        {
            try {
                base.InvalidateObject();
            }
            catch (Exception) {
            }
            DeviceType = CLDeviceType.None;
            Platform = null!;
        }
        #endregion
    }
}
