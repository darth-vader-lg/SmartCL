using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SmartCL
{
    /// <summary>
    /// An OpenCL program without return value
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class CLProgram : CLObject
    {
        #region Properties
        /// <summary>
        /// The OpenCL context
        /// </summary>
        public nint Context { get; private set; }
        /// <summary>
        /// The device
        /// </summary>
        public CLDevice Device { get; private set; }
        /// <summary>
        /// The commands queue
        /// </summary>
        public nint Queue { get; private set; }
        /// <summary>
        /// The source code
        /// </summary>
        public string[] SourceCode { get; }
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
        /// <param name="device">The device</param>
        /// <param name="id">The program identifier</param>
        /// <param name="context">The context</param>
        /// <param name="queue">The queue</param>
        /// <param name="sourceCode">The source code of the program</param>
        private CLProgram(CLDevice device, nint id, nint context, nint queue, string[] sourceCode) : base(id)
        {
            Context = context;
            Device = device;
            Queue = queue;
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
        /// <param name="device">Destination device</param>
        /// <param name="sourceCode">Source code</param>
        /// <returns>The program</returns>
        internal static CLProgram Create(CLDevice device, string[] sourceCode)
        {
            var context = CreateContext(
                new[] { (nint)CLContextProperties.Platform, device.Platform.ID, 0 },
                1,
                new[] { device.ID },
                null!,
                IntPtr.Zero,
                out var result);
            CL.CheckResult(result, "Cannot create the context");
            var program = CreateProgramWithSource(context, (uint)sourceCode.Length, sourceCode, null!, out result);
            CL.CheckResult(result, "Cannot create the program");
            try {
                CL.CheckResult(BuildProgram(program, 0, null!, null!, null!, IntPtr.Zero));
            }
            catch (Exception ex) {
                var log = IntPtr.Zero;
                try {
                    GetProgramBuildInfo(program, device.ID, CLProgramBuildInfo.BuildLog, IntPtr.Zero, IntPtr.Zero, out var logsize);
                    log = Marshal.AllocHGlobal(logsize);
                    GetProgramBuildInfo(program, device.ID, CLProgramBuildInfo.BuildLog, logsize, log, out var _);
                    var message = Marshal.PtrToStringAnsi(log);
                    throw new CLException(message, ex);
                }
                finally {
                    if (log != IntPtr.Zero)
                        Marshal.FreeHGlobal(log);
                }
            }
            var queue = CreateCommandQueue(context, device.ID, CLCommandQueueProperties.None, out result);
            CL.CheckResult(result, "Cannot create the commands queue");
            return new(device, program, context, queue, sourceCode);
        }

        /// <summary>
        /// Create a buffer on the device
        /// </summary>
        /// <typeparam name="T">Type of data</typeparam>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        /// <returns>The buffer</returns>
        public CLBuffer<T> CreateBuffer<T>(int length, CLAccess access = CLAccess.ReadWrite) where T : unmanaged
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
        public CLBuffer<T> CreateBuffer<T>(T[] array, CLAccess access = CLAccess.ReadWrite) where T : unmanaged
        {
            return CLBuffer<T>.Create(this, array?.Length ?? 0, access, array!);
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clCreateCommandQueue")]
        private static extern nint CreateCommandQueue(
            [In] nint context,
            [In] nint device,
            [In] CLCommandQueueProperties properties,
            [Out] out CLError errcode_ret);
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
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clCreateKernel")]
        private static extern nint CreateKernel(
            [In] nint program,
            [In, MarshalAs(UnmanagedType.LPStr)] string kernel_name,
            [Out] out CLError errcode_ret);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0>, T0> CreateKernel<T0>
            (string name,
            CLArg<T0> arg0
            ) => new(this, name, GetKernel(name), arg0!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0> CreateKernel<TDelegate, T0>
            (string name, out TDelegate call,
            CLArg<T0> arg0
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0>(this, name, GetKernel(name), arg0!);
            call = kernel.Call;
            return kernel;
        }
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0, T1>, T0, T1> CreateKernel<T0, T1>
            (string name,
            CLArg<T0> arg0,
            CLArg<T1> arg1
            ) => new(this, name, GetKernel(name), arg0!, arg1!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0, T1> CreateKernel<TDelegate, T0, T1>
            (string name, out TDelegate call,
            CLArg<T0> arg0,
            CLArg<T1> arg1
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0, T1>(this, name, GetKernel(name), arg0!, arg1!);
            call = kernel.Call;
            return kernel;
        }
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0, T1, T2>, T0, T1, T2> CreateKernel<T0, T1, T2>
            (string name,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2) => new(this, name, GetKernel(name), arg0!, arg1!, arg2!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0, T1, T2> CreateKernel<TDelegate, T0, T1, T2>
            (string name, out TDelegate call,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0, T1, T2>(this, name, GetKernel(name), arg0!, arg1!, arg2!);
            call = kernel.Call;
            return kernel;
        }
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0, T1, T2, T3>, T0, T1, T2, T3> CreateKernel<T0, T1, T2, T3>
            (string name,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3
            ) => new(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0, T1, T2, T3> CreateKernel<TDelegate, T0, T1, T2, T3>
            (string name, out TDelegate call,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0, T1, T2, T3>(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!);
            call = kernel.Call;
            return kernel;
        }
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0, T1, T2, T3, T4>, T0, T1, T2, T3, T4> CreateKernel<T0, T1, T2, T3, T4>
            (string name,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4
            ) => new(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0, T1, T2, T3, T4> CreateKernel<TDelegate, T0, T1, T2, T3, T4>
            (string name, out TDelegate call,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0, T1, T2, T3, T4>(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!);
            call = kernel.Call;
            return kernel;
        }
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0, T1, T2, T3, T4, T5>, T0, T1, T2, T3, T4, T5> CreateKernel<T0, T1, T2, T3, T4, T5>
            (string name,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5
            ) => new(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0, T1, T2, T3, T4, T5> CreateKernel<TDelegate, T0, T1, T2, T3, T4, T5>
            (string name, out TDelegate call,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0, T1, T2, T3, T4, T5>(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!);
            call = kernel.Call;
            return kernel;
        }
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <typeparam name="T6">Type of the seventh parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0, T1, T2, T3, T4, T5, T6>, T0, T1, T2, T3, T4, T5, T6> CreateKernel<T0, T1, T2, T3, T4, T5, T6>
            (string name,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5,
            CLArg<T6> arg6
            ) => new(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!, arg6!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <typeparam name="T6">Type of the seventh parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6> CreateKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6>
            (string name, out TDelegate call,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5,
            CLArg<T6> arg6
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6>(
                this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!, arg6!);
            call = kernel.Call;
            return kernel;
        }
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <typeparam name="T6">Type of the seventh parameter</typeparam>
        /// <typeparam name="T7">Type of the eighth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0, T1, T2, T3, T4, T5, T6, T7>, T0, T1, T2, T3, T4, T5, T6, T7> CreateKernel<T0, T1, T2, T3, T4, T5, T6, T7>
            (string name,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5,
            CLArg<T6> arg6,
            CLArg<T7> arg7
            ) => new(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!, arg6!, arg7!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <typeparam name="T6">Type of the seventh parameter</typeparam>
        /// <typeparam name="T7">Type of the eighth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7> CreateKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7>
            (string name, out TDelegate call,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5,
            CLArg<T6> arg6,
            CLArg<T7> arg7
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7>(
                this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!, arg6!, arg7!);
            call = kernel.Call;
            return kernel;
        }
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <typeparam name="T6">Type of the seventh parameter</typeparam>
        /// <typeparam name="T7">Type of the eighth parameter</typeparam>
        /// <typeparam name="T8">Type of the ninth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0, T1, T2, T3, T4, T5, T6, T7, T8>, T0, T1, T2, T3, T4, T5, T6, T7, T8>
            CreateKernel<T0, T1, T2, T3, T4, T5, T6, T7, T8>
            (string name,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5,
            CLArg<T6> arg6,
            CLArg<T7> arg7,
            CLArg<T8> arg8
            ) => new(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!, arg6!, arg7!, arg8!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <typeparam name="T6">Type of the seventh parameter</typeparam>
        /// <typeparam name="T7">Type of the eighth parameter</typeparam>
        /// <typeparam name="T8">Type of the ninth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7, T8> CreateKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7, T8>
            (string name, out TDelegate call,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5,
            CLArg<T6> arg6,
            CLArg<T7> arg7,
            CLArg<T8> arg8
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7, T8>(
                this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!, arg6!, arg7!, arg8!);
            call = kernel.Call;
            return kernel;
        }
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <typeparam name="T6">Type of the seventh parameter</typeparam>
        /// <typeparam name="T7">Type of the eighth parameter</typeparam>
        /// <typeparam name="T8">Type of the ninth parameter</typeparam>
        /// <typeparam name="T9">Type of the tenth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            CreateKernel<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            (string name,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5,
            CLArg<T6> arg6,
            CLArg<T7> arg7,
            CLArg<T8> arg8,
            CLArg<T9> arg9
            ) => new(this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!, arg6!, arg7!, arg8!, arg9!);
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <typeparam name="T0">Type of the first parameter</typeparam>
        /// <typeparam name="T1">Type of the second parameter</typeparam>
        /// <typeparam name="T2">Type of the third parameter</typeparam>
        /// <typeparam name="T3">Type of the fourth parameter</typeparam>
        /// <typeparam name="T4">Type of the fifth parameter</typeparam>
        /// <typeparam name="T5">Type of the sixth parameter</typeparam>
        /// <typeparam name="T6">Type of the seventh parameter</typeparam>
        /// <typeparam name="T7">Type of the eighth parameter</typeparam>
        /// <typeparam name="T8">Type of the ninth parameter</typeparam>
        /// <typeparam name="T9">Type of the tenth parameter</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> CreateKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            (string name, out TDelegate call,
            CLArg<T0> arg0,
            CLArg<T1> arg1,
            CLArg<T2> arg2,
            CLArg<T3> arg3,
            CLArg<T4> arg4,
            CLArg<T5> arg5,
            CLArg<T6> arg6,
            CLArg<T7> arg7,
            CLArg<T8> arg8,
            CLArg<T9> arg9
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
                this, name, GetKernel(name), arg0!, arg1!, arg2!, arg3!, arg4!, arg5!, arg6!, arg7!, arg8!, arg9!);
            call = kernel.Call;
            return kernel;
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
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (Queue != 0) {
                ReleaseCommandQueue(Queue);
                Queue = 0;
            }
            if (Context != 0) {
                ReleaseContext(Context);
                Context = 0;
            }
            if (ID != 0)
                ReleaseProgram(ID);
            base.Dispose(disposing);
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
        /// Initialize the program and return a kernel id
        /// </summary>
        /// <param name="name">Name of the kernel</param>
        /// <returns>The kernel ID</returns>
        /// <exception cref="CLException">Exception</exception>
        private nint GetKernel(string name)
        {
            if (ID == 0)
                throw new ObjectDisposedException(nameof(CLProgram));
            var kernelID = CreateKernel(ID, name, out var result);
            CL.CheckResult(result, $"Cannot create the kernel {name}");
            return kernelID;
        }
        /// <summary>
        /// Debugger visualization
        /// </summary>
        /// <returns>The text</returns>
        private string GetDebuggerDisplay()
        {
            return string.Join(Environment.NewLine, SourceCode);
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clReleaseCommandQueue")]
        private static extern CLError ReleaseCommandQueue([In] nint command_queue);
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clReleaseContext")]
        private static extern CLError ReleaseContext([In] nint context);
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clReleaseProgram")]
        private static extern CLError ReleaseProgram([In] nint program);
        #endregion
    }
}
