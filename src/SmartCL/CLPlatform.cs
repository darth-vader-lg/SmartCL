using System;
using System.Collections.Generic;
using System.Diagnostics;
using Silk.NET.OpenCL;

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
        /// <param name="cl">The api</param>
        /// <param name="id">ID of the platform</param>
        unsafe internal CLPlatform(CL cl, nint id) : base(cl, id)
        {
            try {
                Extensions = GetStringInfo(id, PlatformInfo.Extensions, cl.Api.GetPlatformInfo).Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception) {
            }
            try {
                IcdSuffixKhr = GetStringInfo(id, PlatformInfo.IcdSuffixKhr, cl.Api.GetPlatformInfo);
            }
            catch (Exception) {
            }
            try {
                Name = GetStringInfo(id, PlatformInfo.Name, cl.Api.GetPlatformInfo);
            }
            catch (Exception) {
            }
            try {
                Profile = GetStringInfo(id, PlatformInfo.Profile, cl.Api.GetPlatformInfo);
            }
            catch (Exception) {
            }
            try {
                Vendor = GetStringInfo(id, PlatformInfo.Vendor, cl.Api.GetPlatformInfo);
            }
            catch (Exception) {
            }
            try {
                Version = GetStringInfo(id, PlatformInfo.Version, cl.Api.GetPlatformInfo);
            }
            catch (Exception) {
            }
            var devices = new List<CLDevice>();
            foreach (var type in new[] { DeviceType.Default, DeviceType.Cpu, DeviceType.Gpu, DeviceType.Accelerator, DeviceType.Custom }) {
                cl.Api.GetDeviceIDs(id, type, 0, null, out var num_devices);
                if (num_devices > 0) {
                    var ids = stackalloc nint[(int)num_devices];
                    cl.Api.GetDeviceIDs(id, type, num_devices, ids, &num_devices);
                    for (var i = 0; i < num_devices; i++)
                        devices.Add(new CLDevice(this, ids[i], (CLDeviceType)type));
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
        #endregion
    }
}
