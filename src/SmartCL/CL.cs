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
        /// The default context
        /// </summary>
        public static CLContext? DefaultContext { get; }
        /// <summary>
        /// The default cpu device
        /// </summary>
        public static CLDeviceContext? DefaultCPU { get; }
        /// <summary>
        /// The default device for this context
        /// </summary>
        public static CLDeviceContext? DefaultDevice { get; }
        /// <summary>
        /// The default gpu device
        /// </summary>
        public static CLDeviceContext? DefaultGPU { get; }
        /// <summary>
        /// First accelerated platform of the first available
        /// </summary>
        public static CLPlatform? DefaultPlatform { get; }
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
            Assert(GetPlatformIDs(0, null!, out var num_platforms), "Cannot get the number of platforms");
            // Get the platforms IDs
            var ids = new nint[num_platforms];
            Assert(GetPlatformIDs(num_platforms, ids, out var _), "Cannot get the platforms IDs");
            // Build the platforms
            Platforms = Enumerable.Range(0, num_platforms).Select(i => new CLPlatform(ids[i])).ToArray();
            // Select the default platform
            DefaultPlatform =
                Platforms
                .Where(p => p.Devices.Any(d => d.DeviceType == CLDeviceType.GPU))
                .DefaultIfEmpty(Platforms.FirstOrDefault())
                .First();
            var defaultGPU = new CLDevicesGroup(DefaultPlatform.GPUs.Take(1));
            var defaultCPU = new CLDevicesGroup(DefaultPlatform.CPUs.Take(1));
            DefaultGPU = defaultGPU.Count > 0 ? new(CLDefaultContext.Create(defaultGPU), defaultGPU[0]) : null;
            DefaultCPU = defaultCPU.Count > 0 ? new(CLDefaultContext.Create(defaultCPU), defaultCPU[0]) : null;
            DefaultDevice = DefaultGPU ?? DefaultCPU;
            DefaultContext = DefaultDevice?.Context;
        }
        /// <summary>
        /// Assert success otherwise throw CLException
        /// </summary>
        /// <param name="result">The result of the call</param>
        internal static void Assert(CLError result, string message = null!)
        {
            if (result == CLError.Success)
                return;
            throw new CLException(result, message);
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
