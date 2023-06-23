using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SmartCL
{
    /// <summary>
    /// The class that represents a platform
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class CLPlatform : CLObject
    {
        #region Properties
        /// <summary>
        /// Platform's devices
        /// </summary>
        public CLDevice[] Devices { get; private set; }
        /// <summary>
        /// Platform's extensions
        /// </summary>
        public string[] Extensions { get; } = Array.Empty<string>();
        /// <summary>
        /// Platform's Khronos suffix
        /// </summary>
        public string IcdSuffixKhr { get; } = "";
        /// <summary>
        /// Platform's name
        /// </summary>
        public string Name { get; } = "";
        /// <summary>
        /// Platform's profile
        /// </summary>
        public string Profile { get; } = "";
        /// <summary>
        /// Platform's vendor
        /// </summary>
        public string Vendor { get; } = "";
        /// <summary>
        /// Platform's version
        /// </summary>
        public string Version { get; } = "";
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">ID of the platform</param>
        internal CLPlatform(nint id) : base(id)
        {
            string GetStringPlatformInfo(CLPlatformInfo infoType)
            {
                var result = GetPlatformInfo(id, infoType, 0, IntPtr.Zero, out var valueSizeRet);
                CL.CheckResult(result, "Cannot read the size of the info");
                var array = new byte[valueSizeRet];
                GCHandle gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
                try {
                    result = GetPlatformInfo(id, infoType, valueSizeRet, gcHandle.AddrOfPinnedObject(), out valueSizeRet);
                    CL.CheckResult(result, "Cannot get the info");
                }
                finally {
                    gcHandle.Free();
                }
                char[] chars = Encoding.ASCII.GetChars(array, 0, array.Length);
                string text = new string(chars);
                char[] trimChars = new char[1];
                return text.TrimEnd(trimChars);
            }
            try {
                Extensions = GetStringPlatformInfo(CLPlatformInfo.Extensions).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception) {
            }
            try {
                IcdSuffixKhr = GetStringPlatformInfo(CLPlatformInfo.IcdSuffixKhr);
            }
            catch (Exception) {
            }
            try {
                Name = GetStringPlatformInfo(CLPlatformInfo.Name);
            }
            catch (Exception) {
            }
            try {
                Profile = GetStringPlatformInfo(CLPlatformInfo.Profile);
            }
            catch (Exception) {
            }
            try {
                Vendor = GetStringPlatformInfo(CLPlatformInfo.Vendor);
            }
            catch (Exception) {
            }
            try {
                Version = GetStringPlatformInfo(CLPlatformInfo.Version);
            }
            catch (Exception) {
            }
            var devices = new List<CLDevice>();
            foreach (var type in new[] { CLDeviceType.Default, CLDeviceType.Cpu, CLDeviceType.Gpu, CLDeviceType.Accelerator, CLDeviceType.Custom }) {
                GetDeviceIDs(id, type, 0, null!, out var num_devices);
                if (num_devices > 0) {
                    var ids = new nint[num_devices];
                    GetDeviceIDs(id, type, num_devices, ids, out num_devices);
                    for (var i = 0; i < num_devices; i++)
                        devices.Add(new CLDevice(this, ids[i], type));
                }
            }
            Devices = devices.ToArray();
        }
        /// <summary>
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (ID == 0)
                return;
            if (disposing) {
                foreach (var device in Devices)
                    device.Dispose();
            }
            Devices = null!;
            base.Dispose(disposing);
        }
        /// <summary>
        /// Debugger view
        /// </summary>
        /// <returns>The debugger view</returns>
        private string GetDebuggerDisplay()
        {
            return Name;
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clGetDeviceIDs")]
        public static extern CLError GetDeviceIDs(
            [In] nint platform,
            [In] CLDeviceType device_type,
            [In] uint num_entries,
            [In, Out] nint[] devices,
            [Out] out uint num_devices);
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clGetPlatformInfo")]
        private static extern CLError GetPlatformInfo(
            [In] nint platform,
            [In] CLPlatformInfo param_name,
            [In] nuint param_value_size,
            [In] IntPtr param_value,
            [Out] out nuint param_value_size_ret);
        #endregion
    }
}
