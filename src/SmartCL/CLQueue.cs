using System;
using System.Runtime.InteropServices;

namespace SmartCL
{
    /// <summary>
    /// Commends queue
    /// </summary>
    internal sealed class CLQueue : CLObject
    {
        #region Properties
        /// <summary>
        /// Target device
        /// </summary>
        public CLDeviceContext? Device { get; private set; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="device">Target device</param>
        /// <param name="id"></param>
        private CLQueue(CLDeviceContext device, nint id) : base(id)
        {
            Device = device;
        }
        /// <summary>
        /// Create a queue
        /// </summary>
        /// <param name="device">Target device</param>
        /// <returns>The queue</returns>
        /// <exception cref="ArgumentNullException">Program and device cannot be null</exception>
        /// <exception cref="ArgumentException">The device is not part of the program's context</exception>
        public static CLQueue Create(CLDeviceContext device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            var queue = CreateCommandQueue(device.Context.ID, device.ID, CLQueueProperties.None, out var result);
            CL.Assert(result, $"Cannot create the commands queue for the device {device.DeviceType}");
            return new(device, queue);
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clCreateCommandQueue")]
        private static extern nint CreateCommandQueue(
            [In] nint context,
            [In] nint device,
            [In] CLQueueProperties properties,
            [Out] out CLError errcode_ret);
        /// <summary>
        /// Destroy the queue
        /// </summary>
        internal void Destroy()
        {
            InvalidateObject();
        }
        /// <summary>
        /// Invalidate the object
        /// </summary>
        protected override void InvalidateObject()
        {
            try {
                if (ID != 0)
                    ReleaseCommandQueue(ID);
            }
            catch (Exception) { }
            Device = null;
            try {
                base.InvalidateObject();
            }
            catch (Exception) {
            }
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clReleaseCommandQueue")]
        private static extern CLError ReleaseCommandQueue([In] nint command_queue);
        #endregion
    }
}
