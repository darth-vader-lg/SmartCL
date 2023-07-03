using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SmartCL
{
    /// <summary>
    /// An OpenCL program without return value
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class CLProgram : CLObject
    {
        #region Properties
        /// <summary>
        /// The OpenCL context
        /// </summary>
        public CLContext Context { get; private set; }
        /// <summary>
        /// The source code
        /// </summary>
        public string[] SourceCode { get; private set; }
        #endregion
        #region Delegates
        /// <summary>
        /// A callback function that can be registered by the application to report the <see cref="ComputeProgram"/> build status.
        /// </summary>
        /// <param name="program">The program identifier</param>
        /// <param name="userData">User data specified in the call to the function BuildProgram</param>
        public delegate void ComputeProgramBuildNotifier(
            [In] nint program,
            [In] IntPtr userData);
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
        /// <param name="context">The context</param>
        /// <param name="id">The program identifier</param>
        /// <param name="sourceCode">The source code of the program</param>
        private CLProgram(CLContext context, nint id, string[] sourceCode) : base(id)
        {
            Context = context;
            SourceCode = sourceCode;
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clBuildProgram")]
        private static extern CLError BuildProgram(
            [In] nint program,
            [In] uint num_devices,
            [In] nint[] device_list,
            [In, MarshalAs(UnmanagedType.LPStr)] string options,
            [In] ComputeProgramBuildNotifier pfn_notify,
            [In] IntPtr user_data);
        /// <summary>
        /// Create program
        /// </summary>
        /// <param name="context">Program context</param>
        /// <param name="sourceCode">Source code. null for invalid program</param>
        /// <returns>The program</returns>
        internal static CLProgram Create(CLContext context, string[] sourceCode)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (sourceCode == null)
                return new(context, 0, sourceCode ?? Array.Empty<string>());
            var program = CreateProgramWithSource(context.ID, (uint)sourceCode.Length, sourceCode, null!, out var result);
            CL.Assert(result, "Cannot create the program");
            try {
                CL.Assert(
                    BuildProgram(
                        program,
                        (uint)context.Devices.Count,
                        context.Devices.Select(d => d.ID).ToArray(),
                        null!,
                        null!,
                        IntPtr.Zero));
            }
            catch (CLException exc) {
                var excMessage = new StringBuilder();
                foreach (var device in context.Devices) {
                    var log = IntPtr.Zero;
                    try {
                        GetProgramBuildInfo(program, device.ID, CLProgramBuildInfo.BuildLog, IntPtr.Zero, IntPtr.Zero, out var logsize);
                        log = Marshal.AllocHGlobal(logsize);
                        GetProgramBuildInfo(program, device.ID, CLProgramBuildInfo.BuildLog, logsize, log, out var _);
                        var message = Marshal.PtrToStringAnsi(log);
                        excMessage.AppendLine(message);
                    }
                    finally {
                        if (log != IntPtr.Zero)
                            Marshal.FreeHGlobal(log);
                    }
                }
                throw new CLException(exc.Error, excMessage.ToString());
            }
            return new(context, program, sourceCode);
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clCreateProgramWithSource")]
        private static extern nint CreateProgramWithSource(
            [In] nint context,
            [In] uint count,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] strings,
            [In] IntPtr[] lengths,
            [Out] out CLError errcode_ret);
        /// <summary>
        /// Destroy the program
        /// </summary>
        internal void Destroy()
        {
            try {
                InvalidateObject();
            }
            catch (Exception) {
            }
        }
        /// <summary>
        /// Get build information
        /// </summary>
        /// <param name="program">Specifies the program object being queried.</param>
        /// <param name="device">Specifies the device for which build information is being queried. device must be a valid device associated with program</param>
        /// <param name="param_name">Specifies the information to query</param>
        /// <param name="param_value_size">Specifies the size in bytes of memory pointed to by param_value</param>
        /// <param name="param_value">A pointer to memory where the appropriate result being queried is returned. If param_value is IntPtr.Zero, it is ignored</param>
        /// <param name="param_value_size_ret">Returns the actual size in bytes of data copied to param_value. If param_value_size_ret is IntPtr.Zero, it is ignored</param>
        /// <returns></returns>
        [DllImport("OpenCL", EntryPoint = "clGetProgramBuildInfo")]
        private static extern CLError GetProgramBuildInfo(
            [In] nint program,
            [In] nint device,
            [In] CLProgramBuildInfo param_name,
            [In] IntPtr param_value_size,
            [In] IntPtr param_value,
            [Out] out IntPtr param_value_size_ret);
        /// <summary>
        /// Debugger visualization
        /// </summary>
        /// <returns>The text</returns>
        private string GetDebuggerDisplay()
        {
            return string.Join(Environment.NewLine, SourceCode);
        }
        /// <summary>
        /// Invalidate the object
        /// </summary>
        protected override void InvalidateObject()
        {
            try {
                if (ID != 0)
                    ReleaseProgram(ID);
            }
            catch (Exception) {
            }
            try {
                base.InvalidateObject();
            }
            catch (Exception) {
            }
            Context = null!;
            SourceCode = null!;
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clReleaseProgram")]
        private static extern CLError ReleaseProgram([In] nint program);
        #endregion
    }
}
