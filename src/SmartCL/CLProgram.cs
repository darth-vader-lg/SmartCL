using System;
using System.Collections.Generic;
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
        /// The source codes
        /// </summary>
        public CLSource[] Sources { get; private set; }
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
        /// <param name="sources">The source codes of the program</param>
        private CLProgram(CLContext context, nint id, CLSource[] sources) : base(id)
        {
            Context = context;
            Sources = sources;
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
        /// Build a program
        /// </summary>
        /// <param name="context">Program context</param>
        /// <param name="sourceCode">Source code. null for invalid program</param>
        /// <returns>The program</returns>
        internal static CLProgram Build(CLContext context, string[] sourceCode)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (sourceCode == null)
                return new(context, 0, Array.Empty<CLSource>());
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
                try {
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
                }
                finally {
                    try {
                        ReleaseProgram(program);
                    }
                    catch (Exception) {
                    }
                }
                throw new CLException(exc.Error, excMessage.ToString());
            }
            return new(context, program, new[] { new CLSource(null, sourceCode) });
        }
        /// <summary>
        /// Compile a program
        /// </summary>
        /// <param name="context">Program context</param>
        /// <param name="sources">Set of sources codes</param>
        /// <returns>The program</returns>
        internal static CLProgram Build(CLContext context, IEnumerable<CLSource> sources)
        {
            // Check the context
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            // Check sources definition
            if (sources == null)
                return new(context, 0, Array.Empty<CLSource>());
            // Create a dictionary of programs
            var programs = new Dictionary<CLSource, nint>();
            var nProgram = 0;
            foreach (var s in sources) {
                var lines = s.Code != null ? s.Code.Select(line => line ?? string.Empty).ToArray() : Array.Empty<string>();
                var id = CreateProgramWithSource(context.ID, (uint)lines.Length, lines, null!, out var result);
                CL.Assert(result, $"Cannot create the program {(string.IsNullOrEmpty(s.Path) ? $"n.{nProgram}" : s.Path)}");
                programs[s] = id;
                nProgram++;
            }
            // Compile and link the programs
            var ids = programs.Keys.Select(k => programs[k]).ToArray();
            var paths = programs.Keys.Select(k => string.IsNullOrWhiteSpace(k.Path) ? "" : k.Path!).ToArray();
            try {
                foreach (var s in programs.Keys) {
                    try {
                        CL.Assert(
                            CompileProgram(
                                programs[s],
                                (uint)context.Devices.Count,
                                context.Devices.Select(d => d.ID).ToArray(),
                                null!,
                                (uint)programs.Keys.Count,
                                ids,
                                paths,
                                null!,
                                IntPtr.Zero));
                    }
                    catch (CLException exc) {
                        var excMessage = new StringBuilder();
                        foreach (var device in context.Devices) {
                            var log = IntPtr.Zero;
                            try {
                                GetProgramBuildInfo(programs[s], device.ID, CLProgramBuildInfo.BuildLog, IntPtr.Zero, IntPtr.Zero, out var logsize);
                                log = Marshal.AllocHGlobal(logsize);
                                GetProgramBuildInfo(programs[s], device.ID, CLProgramBuildInfo.BuildLog, logsize, log, out var _);
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
                }
                nint program = 0;
                try {
                    program = LinkProgram(
                                context.ID,
                                (uint)context.Devices.Count,
                                context.Devices.Select(d => d.ID).ToArray(),
                                null!,
                                (uint)programs.Keys.Count,
                                ids,
                                null!,
                                IntPtr.Zero,
                                out var result);
                    CL.Assert(result, "Linker error");
                    return new(context, program, sources.ToArray());
                }
                catch (CLException exc) {
                    try {
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
                    finally {
                        try {
                            ReleaseProgram(program);
                        }
                        catch (Exception) {
                        }
                    }
                }

            }
            finally {
                foreach (var id in ids) {
                    try {
                        ReleaseProgram(id);
                    }
                    catch (Exception) {
                    }
                }
            }
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clCompileProgram")]
        private static extern CLError CompileProgram(
            [In] nint program,
            [In] uint num_devices,
            [In] nint[] device_list,
            [In, MarshalAs(UnmanagedType.LPStr)] string options,
            [In] uint num_input_headers,
            [In] nint[] input_headers,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] header_include_names,
            [In] ComputeProgramBuildNotifier pfn_notify,
            [In] IntPtr user_data);
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
            return string.Join(Environment.NewLine, Sources?.SelectMany(s => s.Code).SelectMany(s => s).Take(20));
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
            Sources = null!;
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clLinkProgram")]
        private static extern nint LinkProgram(
            [In] nint context,
            [In] uint num_devices,
            [In] nint[] device_list,
            [In, MarshalAs(UnmanagedType.LPStr)] string options,
            [In] uint num_input_programs,
            [In] nint[] input_programs,
            [In] ComputeProgramBuildNotifier pfn_notify,
            [In] IntPtr user_data,
            [Out] out CLError errcode_ret);
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clReleaseProgram")]
        private static extern CLError ReleaseProgram([In] nint program);
        #endregion
    }
}
