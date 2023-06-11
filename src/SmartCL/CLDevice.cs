using System.Diagnostics;
using Silk.NET.OpenCL;

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
        public CLDeviceType DeviceType { get; }
        /// <summary>
        /// The platform
        /// </summary>
        public CLPlatform Platform { get; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="platform">The owner platform</param>
        /// <param name="id">ID of the device</param>
        /// <param name="deviceType">The type of the device</param>
        internal CLDevice(CLPlatform platform, nint id, CLDeviceType deviceType) : base(platform.CL, id)
        {
            Platform = platform;
            DeviceType = deviceType;
        }
        /// <summary>
        /// Create a program
        /// </summary>
        /// <typeparam name="T">Type of the delegate</typeparam>
        /// <param name="sourceCode">The source code with kernels</param>
        /// <returns>The program</returns>
        public CLProgram CreateProgram(string[] sourceCode)
        {
            return new CLProgram(this, sourceCode);
        }
        /// <summary>
        /// Debugger visualization
        /// </summary>
        /// <returns>The string</returns>
        private string GetDebuggerDisplay()
        {
            return DeviceType.ToString();
        }
        #endregion
    }

    /// <summary>
    /// Device types
    /// </summary>
    public enum CLDeviceType : ulong
    {
        None = DeviceType.None,
        Default = DeviceType.Default,
        Cpu = DeviceType.Cpu,
        Gpu = DeviceType.Gpu,
        Accelerator = DeviceType.Accelerator,
        All = DeviceType.All,
        Custom = DeviceType.Custom
    }
}
