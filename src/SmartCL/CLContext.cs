using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SmartCL
{
    /// <summary>
    /// OpenCL context
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public abstract class CLContext : CLObject
    {
        #region Properties
        /// <summary>
        /// The default device for this context
        /// </summary>
        public CLDeviceContext DefaultDevice { get; private set; }
        /// <summary>
        /// Set of devices for this context
        /// </summary>
        public CLDevicesGroup Devices { get; private set; }
        /// <summary>
        /// The program loaded in the context
        /// </summary>
        public CLProgram? Program { get; private set; }
        #endregion
        #region Delegates
        /// <summary>
        /// Callback function for context creation
        /// </summary>
        /// <param name="errorInfo">Error info</param>
        /// <param name="data">Binary data pointer</param>
        /// <param name="dataSize">Size of binary data</param>
        /// <param name="userData">User data specified in the call to the function CreateContext</param>
        public delegate void CreateContextNotifier(
            [In, MarshalAs(UnmanagedType.LPStr)] string errorInfo,
            [In] IntPtr data,
            [In] IntPtr dataSize,
            [In] IntPtr userData);
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="devices">Devices set</param>
        /// <param name="id">Context identifier</param>
        private protected CLContext(CLDevicesGroup devices, nint id) : base(id)
        {
            try {
                Devices = devices ?? throw new ArgumentNullException(nameof(devices));
                if (!Valid)
                    throw new CLException(CLError.InvalidContext);
                if (devices.Count < 1)
                    throw new ArgumentException("There must be at least one device in the group", nameof(devices));
                DefaultDevice = new(this, devices.First());
            }
            catch (Exception) {
                InvalidateObject();
                Devices = devices;
                DefaultDevice = null!;
            }
        }
        /// <summary>
        /// Create a buffer on the device
        /// </summary>
        /// <typeparam name="T">Type of data</typeparam>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        /// <returns>The buffer</returns>
        public CLBuffer<T> CreateBuffer<T>(int length, CLAccess access = CLAccess.ReadWrite) where T : struct
        {
            return CLBuffer<T>.Create(this, length, access);
        }
        /// <summary>
        /// Create a buffer from a host array
        /// </summary>
        /// <typeparam name="T">Type of data</typeparam>
        /// <param name="array">User supplied array</param>
        /// <param name="access">Access type</param>
        /// <returns>The buffer</returns>
        public CLBuffer<T> CreateBuffer<T>(T[] array, CLAccess access = CLAccess.ReadWrite) where T : struct
        {
            return CLBuffer<T>.Create(this, array?.Length ?? 0, access, array!);
        }
        /// <summary>
        /// Create the context
        /// </summary>
        /// <param name="devices"></param>
        /// <returns>The context identifier</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private protected static nint CreateContext(IEnumerable<CLDevice> devices)
        {
            if (devices == null)
                throw new ArgumentNullException(nameof(devices));
            var ids = devices.Select(d => d?.ID ?? 0).ToArray();
            if (ids.Length < 1)
                throw new InvalidOperationException("There must be at least one device to create a context");
            for (var i = 0; i < ids.Length; i++) {
                if (ids[i] == 0)
                    throw new CLException(CLError.InvalidDevice, $"Invalid device item {i}");
            }
            var platform = devices.First().Platform;
            if (!devices.All(d => d.Platform == platform))
                throw new ArgumentException("All devices must be in the same platform", nameof(devices));
            var context = CreateContext(
                new[] { (nint)CLContextProperties.Platform, platform.ID, 0 },
                (uint)ids.Length,
                ids,
                null!,
                IntPtr.Zero,
                out var result);
            CL.Assert(result, "Cannot create the context");
            return context;
        }
        /// <summary>
        /// Create a program
        /// </summary>
        /// <param name="sourceCode">The source code with kernels</param>
        /// <returns>The program</returns>
        public CLProgram CreateProgram(string[] sourceCode)
        {
            if (sourceCode == null)
                throw new ArgumentNullException(nameof(sourceCode));
            Program?.Destroy();
            Program = null!;
            return Program = CLProgram.Create(this, sourceCode);
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clCreateContext")]
        private static extern nint CreateContext(
            [In] nint[] properties,
            [In] uint num_devices,
            [In] nint[] devices,
            [In] CreateContextNotifier pfn_notify,
            [In] IntPtr user_data,
            [Out] out CLError errcode_ret);
        /// <summary>
        /// Debug display
        /// </summary>
        /// <returns>The human readable string</returns>
        private string GetDebuggerDisplay()
        {
            var sb = new StringBuilder();
            if (Devices != null) {
                foreach (var device in Devices) {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(device.DeviceType);
                }
            }
            return $"Program={(Program != null ? "Loaded" : "None")}, Devices=[{sb}]";
        }
        /// <summary>
        /// Invalidate the object
        /// </summary>
        protected override void InvalidateObject()
        {
            try {
                Program?.Destroy();
            }
            catch (Exception) {
            }
            finally {
                Program = null!;
            }
            try {
                if (ID != 0)
                    ReleaseContext(ID);
            }
            catch (Exception) {
            }
            finally {
                try {
                    base.InvalidateObject();
                }
                catch (Exception) {
                }
                DefaultDevice = null!;
                Devices = null!;
            }
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clReleaseContext")]
        private static extern CLError ReleaseContext([In] nint context);
        #endregion
    }
}
