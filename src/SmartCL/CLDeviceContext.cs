using System;
using System.Runtime.InteropServices;

namespace SmartCL
{
    /// <summary>
    /// Device of a context
    /// </summary>
    public sealed class CLDeviceContext : CLDevice
    {
        #region Properties
        /// <summary>
        /// Owner context
        /// </summary>
        public CLContext Context { get; private set; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Owner context</param>
        /// <param name="device">Base device</param>
        internal CLDeviceContext(CLContext context, CLDevice device) : base(device.Platform, device.ID, device.DeviceType)
        {
            try {
                if (context == null)
                    throw new ArgumentNullException(nameof(context));
                if (device == null)
                    throw new ArgumentNullException(nameof(device));
                if (!context.Valid)
                    throw new CLException(CLError.InvalidContext);
                if (!device.Valid)
                    throw new CLException(CLError.InvalidDevice);
                Context = context;
            }
            catch (Exception) {
                InvalidateObject();
                Context = null!;
            }
        }
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
        /// <param name="name">The kernel's name</param>
        /// <returns>The kernel</returns>
        public CLKernel<Action> CreateKernel
            (string name
            ) => new(this, name, GetKernel(name));
        /// <summary>
        /// Create a kernel
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate</typeparam>
        /// <param name="name">The kernel's name</param>
        /// <param name="call">Delegate for kernel call</param>
        /// <returns>The kernel</returns>
        public CLKernel<TDelegate> CreateKernel<TDelegate>
            (string name, out TDelegate call
            ) where TDelegate : Delegate
        {
            var kernel = new CLKernel<TDelegate>(this, name, GetKernel(name));
            call = kernel.Call;
            return kernel;
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
        /// Initialize the program and return a kernel id
        /// </summary>
        /// <param name="name">Name of the kernel</param>
        /// <returns>The kernel ID</returns>
        private nint GetKernel(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!Context?.Valid ?? false)
                throw new CLException(CLError.InvalidContext);
            if (!Context?.Program?.Valid ?? false)
                throw new CLException(CLError.InvalidProgram);
            var kernelID = CreateKernel(Context!.Program!.ID, name, out var result);
            CL.Assert(result, $"Cannot create the kernel {name}");
            return kernelID;
        }
        #endregion
    }
}
