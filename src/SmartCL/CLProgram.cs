using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Silk.NET.OpenCL;

namespace SmartCL
{
    /// <summary>
    /// An OpenCL program without return value
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class CLProgram : CLObject
    {
        #region Fields
        /// <summary>
        /// The OpenCL context
        /// </summary>
        private Lazy<nint> context;
        /// <summary>
        /// The program
        /// </summary>
        private Lazy<nint> program;
        /// <summary>
        /// The commands queue
        /// </summary>
        private Lazy<nint> queue;
        #endregion
        #region Properties
        /// <summary>
        /// The OpenCL context
        /// </summary>
        public nint Context => context?.Value ?? throw new ObjectDisposedException(nameof(CLProgram));
        /// <summary>
        /// The device
        /// </summary>
        public CLDevice Device { get; }
        /// <summary>
        /// The commands queue
        /// </summary>
        public nint Queue => queue?.Value ?? throw new ObjectDisposedException(nameof(CLProgram));
        /// <summary>
        /// The source code
        /// </summary>
        public string[] SourceCode { get; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="device">The device</param>
        /// <param name="sourceCode">The source code of the program</param>
        internal CLProgram(CLDevice device, string[] sourceCode) : base(device.CL, device.ID)
        {
            Device = device;
            SourceCode = sourceCode;
            context = new(() =>
            {
                unsafe {
                    var props = stackalloc nint[3];
                    props[0] = (nint)ContextProperties.Platform;
                    props[1] = Device.Platform.ID;
                    props[2] = 0;
                    var deviceId = Device.ID;
                    static unsafe void NotifyFunc(byte* errinfo, void* privateinfo, nuint cb, void* userdata)
                    {
                        Console.WriteLine($"Notification: {Marshal.PtrToStringAnsi((nint)errinfo)}");
                    }
                    var context = CL.Api.CreateContext(props, 1, &deviceId, NotifyFunc, null, out var result);
                    CL.CheckResult(result, "Cannot create the context");
                    return context;
                }
            }, LazyThreadSafetyMode.PublicationOnly);
            queue = new(() =>
            {
                var queue = CL.Api.CreateCommandQueue(Context, Device.ID, CommandQueueProperties.None, out var result);
                CL.CheckResult(result, "Cannot create the commands queue");
                return queue;

            }, LazyThreadSafetyMode.PublicationOnly);
            program = new(() =>
            {
                unsafe {
                    var program = CL.Api.CreateProgramWithSource(Context, (uint)SourceCode.Length, SourceCode, null, out var result);
                    CL.CheckResult(result, "Cannot create the program");
                    try {
                        CL.CheckResult(CL.Api.BuildProgram(program, 0, null, (byte*)null, null, null));
                    }
                    catch (Exception ex) {
                        var logsize = UIntPtr.Zero;
                        CL.Api.GetProgramBuildInfo(program, Device.ID, ProgramBuildInfo.BuildLog, 0, null, &logsize);
                        var log = Marshal.AllocHGlobal((nint)logsize.ToPointer());
                        CL.Api.GetProgramBuildInfo(program, Device.ID, ProgramBuildInfo.BuildLog, logsize, log.ToPointer(), (nuint*)null);
                        throw new CLException(Marshal.PtrToStringAnsi(log), ex);
                    }
                    return program;
                }
            }, LazyThreadSafetyMode.PublicationOnly);
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
            return new CLBuffer<T>(this, length, access);
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
            return new CLBuffer<T>(this, array?.Length ?? 0, access, array ?? Array.Empty<T>());
        }
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
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (queue != null) {
                CL.Api.ReleaseCommandQueue(Queue);
                queue = null!;
            }
            if (context != null) {
                CL.Api.ReleaseContext(Context);
                context = null!;
            }
            if (program != null) {
                CL.Api.ReleaseProgram(program.Value);
                program = null!;
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Initialize the program and return a kernel id
        /// </summary>
        /// <param name="name">Name of the kernel</param>
        /// <returns>The kernel ID</returns>
        /// <exception cref="CLException">Exception</exception>
        private nint GetKernel(string name)
        {
            if (program == null)
                throw new ObjectDisposedException(nameof(CLProgram));
            var kernelID = CL.Api.CreateKernel(program.Value, name, out var result);
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
        #endregion
    }
}
