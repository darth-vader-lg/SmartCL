using System.Linq;
using Silk.NET.OpenCL;

namespace SmartCL
{
    /// <summary>
    /// Smart OpenCL class
    /// </summary>
    public class CL
    {
        #region Properties
        /// <summary>
        /// The OpenCL Api
        /// </summary>
        internal Silk.NET.OpenCL.CL Api { get; }
        /// <summary>
        /// The available platforms
        /// </summary>
        public CLPlatform[] Platforms { get; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cl">CL Api instance</param>
        private unsafe CL(Silk.NET.OpenCL.CL cl)
        {
            // Store the cl api context
            Api = cl;
            // Get the number of available platforms
            CheckResult(cl.GetPlatformIDs(0, null, out var num_platforms), "Cannot get the number of platforms");
            // Get the platforms IDs
            var ids = stackalloc nint[(int)num_platforms];
            CheckResult(cl.GetPlatformIDs(num_platforms, ids, out num_platforms), "Cannot get the platforms IDs");
            // Build the platforms
            Platforms = new CLPlatform[num_platforms];
            for (var i = 0; i < num_platforms; i++)
                Platforms[i] = new(this, ids[i]);
        }
        /// <summary>
        /// Array definition
        /// </summary>
        /// <typeparam name="T">Type of array element</typeparam>
        /// <param name="access">Access type</param>
        /// <returns></returns>
        public static CLArg<T[]> Array<T>(CLAccess access = CLAccess.ReadWrite) where T : struct
        {
            return new CLArg<T[]>(access);
        }
        /// <summary>
        /// Check the result of a call to cl api and throw an exception if something wrong
        /// </summary>
        /// <param name="result">The result of the call</param>
        internal static void CheckResult(int result, string? message = null)
        {
            if (result == 0)
                return;
            if (message != null)
                throw new CLException($"{message}. Error: {(ErrorCodes)result}");
            throw new CLException($"Error: {(ErrorCodes)result}");
        }
        /// <summary>
        /// Create an instance of SmartOpenCL
        /// </summary>
        public static CL Create()
        {
            return new CL(Silk.NET.OpenCL.CL.GetApi());
        }
        /// <summary>
        /// Return the first available GPU device or the default if no one GPU is present
        /// </summary>
        /// <returns>The device</returns>
        public static CLDevice GetFirstGpuOrDefault()
        {
            var cl = Create();
            var platform = cl.Platforms.Where(p => p.Devices.Any(d => d.DeviceType == CLDeviceType.Gpu)).DefaultIfEmpty(cl.Platforms.First()).First();
            var device = platform.Devices
                .Where(d => d.DeviceType == CLDeviceType.Gpu)
                .DefaultIfEmpty(platform.Devices.Where(d => d.DeviceType == CLDeviceType.Default).First())
                .First();
            return device;
        }
        /// <summary>
        /// Variable definition
        /// </summary>
        /// <typeparam name="T">Type of variable</typeparam>
        /// <returns>The CL variable</returns>
        public static CLArg<T> Var<T>() where T : struct
        {
            return new CLArg<T>(CLAccess.Const);
        }
        #endregion
    }
}
