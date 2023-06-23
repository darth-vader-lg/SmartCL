using System.Linq;
using System.Runtime.InteropServices;

namespace SmartCL
{
    /// <summary>
    /// Smart OpenCL class
    /// </summary>
    public static class CL
    {
        #region Properties
        /// <summary>
        /// The available platforms
        /// </summary>
        public static CLPlatform[] Platforms { get; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cl">CL Api instance</param>
        static CL()
        {
            // Get the number of available platforms
            CheckResult(GetPlatformIDs(0, null!, out var num_platforms), "Cannot get the number of platforms");
            // Get the platforms IDs
            var ids = new nint[num_platforms];
            CheckResult(GetPlatformIDs(num_platforms, ids, out var _), "Cannot get the platforms IDs");
            // Build the platforms
            Platforms = Enumerable.Range(0, num_platforms).Select(i => new CLPlatform(ids[i])).ToArray();
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
        internal static void CheckResult(CLError result, string? message = null)
        {
            if (result == 0)
                return;
            if (message != null)
                throw new CLException($"{message}. Error: {result}");
            throw new CLException($"Error: {result}");
        }
        /// <summary>
        /// Return the first available GPU device or the default if no one GPU is present
        /// </summary>
        /// <returns>The device</returns>
        public static CLDevice GetFirstGpuOrDefault()
        {
            var platform = Platforms.Where(p => p.Devices.Any(d => d.DeviceType == CLDeviceType.Gpu)).DefaultIfEmpty(Platforms.First()).First();
            var device = platform.Devices
                .Where(d => d.DeviceType == CLDeviceType.Gpu)
                .DefaultIfEmpty(platform.Devices.Where(d => d.DeviceType == CLDeviceType.Default).First())
                .First();
            return device;
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clGetPlatformIDs")]
        private static extern CLError GetPlatformIDs(
            [In] int num_entries,
            [Out] nint[] platforms,
            [Out] out int num_platforms);
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
