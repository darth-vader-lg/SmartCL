using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SmartCL
{
    public abstract class CLKernel : CLObject
    {
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">The ID of the object</param>
        protected CLKernel(nint id) : base(id)
        {
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clEnqueueNDRangeKernel")]
        private protected static extern CLError EnqueueNDRangeKernel(
            [In] nint command_queue,
            [In] nint kernel,
            [In] uint work_dim,
            [In] nuint[] global_work_offset,
            [In] nuint[] global_work_size,
            [In] nuint[] local_work_size,
            [In] uint num_events_in_wait_list,
            [In] nint[] event_wait_list,
            [In, Out] ref nint new_event);
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clFinish")]
        private protected static extern CLError Finish([In] nint command_queue);
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clSetKernelArg")]
        internal static extern CLError SetKernelArg(
            [In] nint kernel,
            [In] uint arg_index,
            [In] nuint arg_size,
            [In] IntPtr arg_value);
        #endregion
    }

    /// <summary>
    /// Kernel object
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class CLKernel<TDelegate> : CLKernel where TDelegate : Delegate
    {
        #region Fields
        /// <summary>
        /// Parameters info
        /// </summary>
        private readonly ICLArg[] args;
        /// <summary>
        /// Buffers
        /// </summary>
        private readonly CLBuffer[] buffers;
        /// <summary>
        /// Tensor dimensione and rank
        /// </summary>
        private nuint[] globalSizes = { 1 };
        /// <summary>
        /// Read kernel results actions
        /// </summary>
        private readonly Action[] readResultsActions;
        /// <summary>
        /// Set kernel arguments actions
        /// </summary>
        private readonly Action[] setArgActions;
        /// <summary>
        /// Parameters validations
        /// </summary>
        private readonly bool[] validations;
        #endregion
        #region Properties
        /// <summary>
        /// Invocation delegate
        /// </summary>
        public TDelegate Call { get; private set; }
        /// <summary>
        /// Dimensions and rank of the global data tensor
        /// </summary>
        public int[] GlobalSizes
        {
            get => globalSizes.Select(dim => (int)dim).ToArray();
            set
            {
                if (value == null)
                    throw new CLException("Dims cannot be null");
                if (value.Length < 1 || value.Length > 3)
                    throw new CLException("Dims must have a rank grather than 0 and less than 4");
                globalSizes = value.Select(dim => (nuint)dim).ToArray();
            }
        }
        /// <summary>
        /// The name of the kernel
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The owner program
        /// </summary>
        public CLProgram Program { get; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">The owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(id)
        {
            // Store parameters
            Program = program;
            Name = name;
            this.args = args;
            // Create data
            setArgActions = new Action[args.Length];
            readResultsActions = new Action[args.Length];
            buffers = new CLBuffer[this.args.Length];
            validations = new bool[this.args.Length];
            // Create the delegate for kernel invocation
            var invoke = GetType().GetMethod("InternalInvoke", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Call = (TDelegate)invoke.CreateDelegate(typeof(TDelegate), this);
            // Create actions for writing 
            for (var i = 0; i < args.Length; i++) {
                // Current argument
                var arg = args[i];
                // CLBuffer(s) management
                if (arg.Type.IsGenericType && typeof(CLBuffer<>).GetGenericTypeDefinition().IsAssignableFrom(arg.Type.GetGenericTypeDefinition())) {
                    // Store the buffer id and the element size
                    buffers[i] = (CLBuffer)arg.Value;
                    // Create the set kernel argument action
                    var ix = (uint)i;
                    setArgActions[i] = new(() =>
                    {
                        var h = GCHandle.Alloc(buffers[ix].ID, GCHandleType.Pinned);
                        try {
                            var result = SetKernelArg(ID, ix, (nuint)IntPtr.Size, h.AddrOfPinnedObject());
                            CL.CheckResult(result, $"Cannot set kernel arg {ix}");
                        }
                        finally {
                            h.Free();
                        }
                    });
                    readResultsActions[i] = null!;
                }
                // Managed arrays management
                else if (arg.Type.IsArray) {
                    // Create buffer function
                    var CreateBufferMethod =
                        typeof(CLKernel<TDelegate>)
                        .GetMethod(nameof(CreateBuffer), BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(args[i].Type.GetElementType());
                    var CreateBuffer_T = (Func<int, CLAccess, object, CLBuffer>)CreateBufferMethod.CreateDelegate(typeof(Func<int, CLAccess, object, CLBuffer>), this);
                    // Create the write buffer and set kernel argument action
                    var ix = i;
                    setArgActions[i] = new(() =>
                    {
                        // Current argument and buffer data
                        ref var arg = ref args[ix];
                        ref var buffer = ref buffers[ix];
                        // Check if the array is null to release the buffer
                        if (arg.Value == null) {
                            if (buffer != null) {
                                buffer.Dispose();
                                buffer = null;
                            }
                        }
                        // Buffer creation
                        else {
                            // length of buffer
                            var length = ((ICollection)arg.Value).Count;
                            // Check if the buffer must be re-allocated
                            if (length != (buffer?.Length ?? 0)) {
                                // Release previous buffer
                                buffer?.Dispose();
                                // Create the buffer and store its info
                                buffer = CreateBuffer_T(length, arg.Access, null!);
                            }
                        }
                        // Write the buffer if it's not read-only
                        if (arg.Access != CLAccess.ReadOnly && arg.Value != null && buffer != null) {
                            var result = buffer.EnqueueWriteBuffer(0, buffer.size, arg.Value);
                            CL.CheckResult(result, $"Cannot enqueue write of the argument {ix}");
                        }
                        // Set the kernel argument
                        if (buffer != null) {
                            var h = GCHandle.Alloc(buffer.ID, GCHandleType.Pinned);
                            try {
                                var result = SetKernelArg(ID, (uint)ix, (nuint)IntPtr.Size, h.AddrOfPinnedObject());
                                CL.CheckResult(result, $"Cannot set kernel arg {ix}");
                            }
                            finally {
                                h.Free();
                            }
                        }
                        else {
                            var result = SetKernelArg(ID, (uint)ix, (nuint)IntPtr.Size, IntPtr.Zero);
                            CL.CheckResult(result, $"Cannot set kernel arg {ix}");
                        }
                    });
                    if (arg.Access == CLAccess.WriteOnly || arg.Access == CLAccess.Const)
                        readResultsActions[i] = null!;
                    else {
                        readResultsActions[i] = new(() =>
                        {
                            ref var arg = ref args[ix];
                            ref var buffer = ref buffers[ix];
                            if (arg.Value == null || buffer == null)
                                return;
                            var result = buffer.EnqueueReadBuffer(0, buffer.size, arg.Value);
                            CL.CheckResult(result, $"Cannot enqueue write of the argument {ix}");
                        });
                    }
                }
                // Standard value types management
                else {
                    // Store buffer information
                    var size = (nuint)Marshal.SizeOf(args[i].Type);
                    // Create the kernel set argument function
                    var setMethod =
                        GetType()
                        .GetMethod(nameof(SetKernelArg), BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(args[i].Type);
                    var setFunc = (Func<uint, nuint, object, CLError>)setMethod.CreateDelegate(typeof(Func<uint, nuint, object, CLError>), this);
                    var ix = i;
                    // Create the set argument action
                    setArgActions[i] = new(() =>
                    {
                        ref var arg = ref args[ix];
                        var result = setFunc((uint)ix, size, arg.Value);
                        CL.CheckResult(result, $"Cannot set kernel arg {ix}");
                    });
                    readResultsActions[i] = null!;
                }
            }
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="T">Type of data</param>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        /// <param name="hostArray">Host supplied array</param>
        private CLBuffer CreateBuffer<T>(int length, CLAccess access, object hostArray = null!) where T : unmanaged
        {
            return CLBuffer<T>.Create(Program, length, access, (T[])hostArray);
        }
        /// <summary>
        /// Create a delegate for the kernel call
        /// </summary>
        /// <typeparam name="T">Type of the delegate</typeparam>
        /// <returns>The delegate</returns>
        public T CreateDelegate<T>() where T : Delegate
        {
            var invoke = GetType().GetMethod("InternalInvoke", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (T)invoke.CreateDelegate(typeof(T), this);
        }
        /// <summary>
        /// Dispose implementation
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (ID == 0)
                return;
            for (var i = 0; i < buffers.Length; i++) {
                buffers[i]?.Dispose();
                buffers[i] = null!;
            }
            Call = null!;
            base.Dispose(disposing);
        }
        /// <summary>
        /// Return a parameter access type
        /// </summary>
        /// <param name="index">Index of the parameter</param>
        /// <returns>The access type</returns>
        public CLAccess GetAccess(int index) => args[index].Access;
        /// <summary>
        /// Debugger visualization
        /// </summary>
        /// <returns></returns>
        private string GetDebuggerDisplay()
        {
            var sb = new StringBuilder(Name);
            sb.Append("(");
            for (var i = 0; i < args.Length; i++) {
                sb.Append(args[i].Type.Name);
                sb.Append(i < args.Length - 1 ? ", " : ")");
            }
            return sb.ToString();
        }
        /// <summary>
        /// Return a parameter type
        /// </summary>
        /// <param name="index">Index of the parameter</param>
        /// <returns>The type</returns>
        public Type GetType(int index) => args[index].Type;
        /// <summary>
        /// Return a parameter value
        /// </summary>
        /// <param name="index">Index of the parameter</param>
        /// <returns>The value</returns>
        public T GetArg<T>(int index) => (T)args[index].Value;
        /// <summary>
        /// Execute the kernel
        /// </summary>
        public void Invoke()
        {
            // Set kernel arguments
            for (var (i, iLen) = (0, args.Length); i < iLen; ++i) {
                if (validations[i])
                    continue;
                setArgActions[i]();
                validations[i] = true;
            }
            // Enqueue the kernel invocation
            var result = EnqueueNDRangeKernel(
                Program.Queue,
                ID,
                (uint)globalSizes.Length,
                null!,
                globalSizes,
                null!,
                0u,
                null!,
                ref Unsafe.NullRef<nint>());
            CL.CheckResult(result, $"Cannot enqueue kernel {Name} execution");
            // Read results
            for (var i = (uint)0; i < args.Length; i++) {
                if (readResultsActions[i] == null || args[i].Value == null)
                    continue;
                readResultsActions[i]();
            }
            result = Finish(Program.Queue);
            CL.CheckResult(result, $"Error reading result of {Name}");
        }
        /// <summary>
        /// Set arguments and invoke the kernel
        /// </summary>
        /// <param name="args">arguments</param>
        protected void SetArgsAndInvoke(params object[] args)
        {
            for (var i = 0; i < args.Length; i++)
                SetArg(i, args[i]);
            Invoke();
        }
        /// <summary>
        /// Set a parameter value
        /// </summary>
        /// <param name="index">Index of the parameter</param>
        /// <param name="value">Value</param>
        /// <returns>The value</returns>
        public void SetArg<T>(int index, T value)
        {
            args[index].Value = value!;
            validations[index] = false;
        }
        /// <summary>
        /// Generics kernel argument setting method
        /// </summary>
        /// <typeparam name="T">Type of argument</typeparam>
        /// <param name="index">Argument index</param>
        /// <param name="size">Size of argument</param>
        /// <param name="arg">Argument</param>
        /// <returns></returns>
        internal CLError SetKernelArg<T>(uint index, nuint size, object arg) where T : unmanaged
        {
            var h = GCHandle.Alloc(arg, GCHandleType.Pinned);
            try {
                return SetKernelArg(ID, index, size, h.AddrOfPinnedObject());
            }
            finally {
                h.Free();
            }
        }
        #endregion
    }

    /// <summary>
    /// Kernel with one parameter
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
    /// <typeparam name="T0">Type of the first parameter</typeparam>
    public class CLKernel<TDelegate, T0> : CLKernel<TDelegate> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T0 Arg0 { get => GetArg<T0>(0); set => SetArg(0, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0) => SetArgsAndInvoke(a0!);
        #endregion
    }

    /// <summary>
    /// Kernel with two parameters
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
    /// <typeparam name="T0">Type of the first parameter</typeparam>
    /// <typeparam name="T1">Type of the second parameter</typeparam>
    public class CLKernel<TDelegate, T0, T1> : CLKernel<TDelegate, T0> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T1 Arg1 { get => GetArg<T1>(1); set => SetArg(1, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0, T1 a1) => SetArgsAndInvoke(a0!, a1!);
        #endregion
    }

    /// <summary>
    /// Kernel with three parameters
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
    /// <typeparam name="T0">Type of the first parameter</typeparam>
    /// <typeparam name="T1">Type of the second parameter</typeparam>
    /// <typeparam name="T2">Type of the third parameter</typeparam>
    public class CLKernel<TDelegate, T0, T1, T2> : CLKernel<TDelegate, T0, T1> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T2 Arg2 { get => GetArg<T2>(2); set => SetArg(2, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0, T1 a1, T2 a2) => SetArgsAndInvoke(a0!, a1!, a2!);
        #endregion
    }

    /// <summary>
    /// Kernel with four parameters
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
    /// <typeparam name="T0">Type of the first parameter</typeparam>
    /// <typeparam name="T1">Type of the second parameter</typeparam>
    /// <typeparam name="T2">Type of the third parameter</typeparam>
    /// <typeparam name="T3">Type of the fourth parameter</typeparam>
    public class CLKernel<TDelegate, T0, T1, T2, T3> : CLKernel<TDelegate, T0, T1, T2> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T3 Arg3 { get => GetArg<T3>(3); set => SetArg(3, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0, T1 a1, T2 a2, T3 a3) => SetArgsAndInvoke(a0!, a1!, a2!, a3!);
        #endregion
    }

    /// <summary>
    /// Kernel with five parameters
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
    /// <typeparam name="T0">Type of the first parameter</typeparam>
    /// <typeparam name="T1">Type of the second parameter</typeparam>
    /// <typeparam name="T2">Type of the third parameter</typeparam>
    /// <typeparam name="T3">Type of the fourth parameter</typeparam>
    /// <typeparam name="T4">Type of the fifth parameter</typeparam>
    public class CLKernel<TDelegate, T0, T1, T2, T3, T4> : CLKernel<TDelegate, T0, T1, T2, T3> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T4 Arg4 { get => GetArg<T4>(4); set => SetArg(4, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0, T1 a1, T2 a2, T3 a3, T4 a4) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!);
        #endregion
    }

    /// <summary>
    /// Kernel with six parameters
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
    /// <typeparam name="T0">Type of the first parameter</typeparam>
    /// <typeparam name="T1">Type of the second parameter</typeparam>
    /// <typeparam name="T2">Type of the third parameter</typeparam>
    /// <typeparam name="T3">Type of the fourth parameter</typeparam>
    /// <typeparam name="T4">Type of the fifth parameter</typeparam>
    /// <typeparam name="T5">Type of the sixth parameter</typeparam>
    public class CLKernel<TDelegate, T0, T1, T2, T3, T4, T5> : CLKernel<TDelegate, T0, T1, T2, T3, T4> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T5 Arg5 { get => GetArg<T5>(5); set => SetArg(5, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0, T1 a1, T2 a2, T3 a3, T4 a4, T5 a5) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!);
        #endregion
    }

    /// <summary>
    /// Kernel with seven parameters
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
    /// <typeparam name="T0">Type of the first parameter</typeparam>
    /// <typeparam name="T1">Type of the second parameter</typeparam>
    /// <typeparam name="T2">Type of the third parameter</typeparam>
    /// <typeparam name="T3">Type of the fourth parameter</typeparam>
    /// <typeparam name="T4">Type of the fifth parameter</typeparam>
    /// <typeparam name="T5">Type of the sixth parameter</typeparam>
    /// <typeparam name="T6">Type of the seventh parameter</typeparam>
    public class CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6> : CLKernel<TDelegate, T0, T1, T2, T3, T4, T5> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T6 Arg6 { get => GetArg<T6>(6); set => SetArg(6, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0, T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!, a6!);
        #endregion
    }

    /// <summary>
    /// Kernel with eight parameters
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
    /// <typeparam name="T0">Type of the first parameter</typeparam>
    /// <typeparam name="T1">Type of the second parameter</typeparam>
    /// <typeparam name="T2">Type of the third parameter</typeparam>
    /// <typeparam name="T3">Type of the fourth parameter</typeparam>
    /// <typeparam name="T4">Type of the fifth parameter</typeparam>
    /// <typeparam name="T5">Type of the sixth parameter</typeparam>
    /// <typeparam name="T6">Type of the seventh parameter</typeparam>
    /// <typeparam name="T7">Type of the eighth parameter</typeparam>
    public class CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7> : CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T7 Arg7 { get => GetArg<T7>(7); set => SetArg(7, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0, T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!, a6!, a7!);
        #endregion
    }

    /// <summary>
    /// Kernel with nine parameters
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
    /// <typeparam name="T0">Type of the first parameter</typeparam>
    /// <typeparam name="T1">Type of the second parameter</typeparam>
    /// <typeparam name="T2">Type of the third parameter</typeparam>
    /// <typeparam name="T3">Type of the fourth parameter</typeparam>
    /// <typeparam name="T4">Type of the fifth parameter</typeparam>
    /// <typeparam name="T5">Type of the sixth parameter</typeparam>
    /// <typeparam name="T6">Type of the seventh parameter</typeparam>
    /// <typeparam name="T7">Type of the eighth parameter</typeparam>
    /// <typeparam name="T8">Type of the ninth parameter</typeparam>
    public class CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7, T8> : CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T8 Arg8 { get => GetArg<T8>(8); set => SetArg(8, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0, T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!, a6!, a7!, a8!);
        #endregion
    }

    /// <summary>
    /// Kernel with ten parameters
    /// </summary>
    /// <typeparam name="TDelegate">Type of the delegate</typeparam>
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
    public class CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : CLKernel<TDelegate, T0, T1, T2, T3, T4, T5, T6, T7, T8> where TDelegate : Delegate
    {
        #region Properties
        /// <summary>
        /// Argument
        /// </summary>
        public T9 Arg9 { get => GetArg<T9>(9); set => SetArg(9, value!); }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="name">Name of the kernel</param>
        /// <param name="id">ID of the kernel</param>
        /// <param name="args">Arguments</param>
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program, name, id, args) { }
        /// <summary>
        /// Internal invocation method
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by reflection")]
        private void InternalInvoke(T0 a0, T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8, T9 a9) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!, a6!, a7!, a8!, a9!);
        #endregion
    }
}
