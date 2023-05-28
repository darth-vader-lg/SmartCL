using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Silk.NET.OpenCL;

namespace SmartCL
{
    /// <summary>
    /// Kernel object
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class CLKernel<TDelegate> : CLObject, IDisposable where TDelegate : Delegate
    {
        #region Fields
        /// <summary>
        /// Parameters info
        /// </summary>
        private readonly ICLArg[] args;
        /// <summary>
        /// Buffers
        /// </summary>
        private readonly (nint id, nuint size)[] buffers;
        /// <summary>
        /// Parameters validations
        /// </summary>
        private readonly bool[] validations;
        #endregion
        #region Properties
        /// <summary>
        /// Invocation delegate
        /// </summary>
        public TDelegate Call { get; }
        /// <summary>
        /// Dimensions of the tensor of the global data
        /// </summary>
        public CLDims Dims { get; set; } = new();
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
        internal CLKernel(CLProgram program, string name, nint id, params ICLArg[] args) : base(program.cl, id)
        {
            Program = program;
            Name = name;
            this.args = args!;
            buffers = new (nint id, nuint size)[this.args.Length];
            validations = new bool[this.args.Length];
            var invoke = GetType().GetMethod("InternalInvoke", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Call = (TDelegate)invoke.CreateDelegate(typeof(TDelegate), this);
        }
        /// <summary>
        /// Finalizer
        /// </summary>
        ~CLKernel()
        {
            Dispose(disposing: false);
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
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose implementation
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            for (var i = 0; i < buffers.Length; i++) {
                if (buffers[i].id == 0)
                    continue;
                cl.Api.ReleaseMemObject(buffers[i].id);
                buffers[i] = default;
            }
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
        public object GetArg(int index) => args[index].Value;
        /// <summary>
        /// Execute the kernel
        /// </summary>
        unsafe public void Invoke()
        {
            int result;
            for (var i = 0; i < args.Length; i++) {
                if (validations[i])
                    continue;
                if (args[i].Type.IsArray) {
                    if (args[i].Value == null) {
                        if (buffers[i].id != 0) {
                            cl.Api.ReleaseMemObject(buffers[i].id);
                            buffers[i] = default;
                        }
                    }
                    else {
                        var size = (nuint)(((Array)args[i].Value).LongLength * args[i].Type.GetElementType().GetSize());
                        if (size != buffers[i].size) {
                            if (buffers[i].id != 0)
                                cl.Api.ReleaseMemObject(buffers[i].id);
                            var memFlags = args[i].Access switch
                            {
                                CLAccess.Const => MemFlags.WriteOnly,
                                CLAccess.WriteOnly => MemFlags.WriteOnly,
                                CLAccess.ReadOnly => MemFlags.ReadOnly,
                                CLAccess.ReadWrite => MemFlags.ReadWrite,
                                _ => throw new CLException($"Invalid access type for the parameter {i}"),
                            };
                            var id = cl.Api.CreateBuffer(Program.Context, memFlags, size, null, out int errcode_ret);
                            CL.CheckResult(errcode_ret, $"Cannot create the buffer for the parameter {i}");
                            buffers[i] = (id, size);
                        }
                    }
                    if (args[i].Access != CLAccess.ReadOnly && args[i] != null) {
                        using var value = args[i].Value.Pin();
                        result = cl.Api.EnqueueWriteBuffer(
                            Program.Queue,
                            buffers[i].id,
                            true,
                            0,
                            (nuint)(((Array)args[i].Value).LongLength * args[i].Type.GetElementType().GetSize()),
                            value.ToPointer(),
                            0,
                            null,
                            (nint*)null);
                        CL.CheckResult(result, $"Cannot enqueue write of the parameter {i}");
                    }
                }
                else
                    buffers[i] = (0, (nuint)args[i].Type.GetSize());
                validations[i] = true;
            }
            for (var i = (uint)0; i < args.Length; i++) {
                if (buffers[i].id != 0) {
                    var bufferId = buffers[i].id;
                    cl.Api.SetKernelArg(id, i, (nuint)sizeof(void*), &bufferId);
                }
                else {
                    using var value = args[i].Value.Pin();
                    cl.Api.SetKernelArg(id, i, buffers[i].size, value.ToPointer());
                }
            }
            {
                using var globals = Dims.Globals.Select(item => (nuint)item).ToArray().Pin();
                using var locals = Dims.Locals.Select(item => (nuint)item).ToArray().Pin();
                using var offsets = Dims.Offsets.Select(item => (nuint)item).ToArray().Pin();
                result = cl.Api.EnqueueNdrangeKernel(
                    Program.Queue,
                    id,
                    (uint)Dims.Globals.Length,
                    offsets.ToPointer<nuint>(),
                    globals.ToPointer<nuint>(),
                    locals.ToPointer<nuint>(),
                    0u,
                    null,
                    null);
                CL.CheckResult(result, $"Cannot enqueue kernel {Name} execution");
                result = cl.Api.Finish(Program.Queue);
                CL.CheckResult(result, $"Error executing {Name}");
            }
            var waitRead = false;
            for (var i = (uint)0; i < args.Length; i++) {
                if (!args[i].Type.IsArray || args[i].Access == CLAccess.WriteOnly || args[i].Access == CLAccess.Const || args[i].Value == null)
                    continue;
                using var value = args[i].Value.Pin();
                result = cl.Api.EnqueueReadBuffer(
                    Program.Queue,
                    buffers[i].id,
                    true,
                    0,
                    (nuint)(((Array)args[i].Value).LongLength * args[i].Type.GetElementType().GetSize()),
                    value.ToPointer(),
                    0,
                    null,
                    (nint*)null);
                CL.CheckResult(result, $"Cannot enqueue read of the parameter {i}");
                waitRead = true;
            }
            if (waitRead) {
                result = cl.Api.Finish(Program.Queue);
                CL.CheckResult(result, $"Error reading result of {Name}");
            }
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
        public void SetArg(int index, object value)
        {
            args[index].Value = value;
            validations[index] = false;
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
        public T0 Arg0 { get => (T0)GetArg(0); set => SetArg(0, value!); }
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
        public T1 Arg1 { get => (T1)GetArg(1); set => SetArg(1, value!); }
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
        public T2 Arg2 { get => (T2)GetArg(2); set => SetArg(2, value!); }
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
        private void InternalInvoke(T0 a0, T1 a1, T1 a2) => SetArgsAndInvoke(a0!, a1!, a2!);
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
        public T3 Arg3 { get => (T3)GetArg(3); set => SetArg(3, value!); }
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
        private void InternalInvoke(T0 a0, T1 a1, T1 a2, T1 a3) => SetArgsAndInvoke(a0!, a1!, a2!, a3!);
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
        public T4 Arg4 { get => (T4)GetArg(4); set => SetArg(4, value!); }
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
        private void InternalInvoke(T0 a0, T1 a1, T1 a2, T1 a3, T1 a4) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!);
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
        public T5 Arg5 { get => (T5)GetArg(5); set => SetArg(5, value!); }
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
        private void InternalInvoke(T0 a0, T1 a1, T1 a2, T1 a3, T1 a4, T1 a5) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!);
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
        public T6 Arg6 { get => (T6)GetArg(6); set => SetArg(6, value!); }
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
        private void InternalInvoke(T0 a0, T1 a1, T1 a2, T1 a3, T1 a4, T1 a5, T1 a6) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!, a6!);
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
        public T7 Arg7 { get => (T7)GetArg(7); set => SetArg(7, value!); }
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
        private void InternalInvoke(T0 a0, T1 a1, T1 a2, T1 a3, T1 a4, T1 a5, T1 a6, T1 a7) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!, a6!, a7!);
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
        public T8 Arg8 { get => (T8)GetArg(8); set => SetArg(8, value!); }
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
        private void InternalInvoke(T0 a0, T1 a1, T1 a2, T1 a3, T1 a4, T1 a5, T1 a6, T1 a7, T1 a8) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!, a6!, a7!, a8!);
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
        public T9 Arg9 { get => (T9)GetArg(9); set => SetArg(9, value!); }
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
        private void InternalInvoke(T0 a0, T1 a1, T1 a2, T1 a3, T1 a4, T1 a5, T1 a6, T1 a7, T1 a8, T1 a9) => SetArgsAndInvoke(a0!, a1!, a2!, a3!, a4!, a5!, a6!, a7!, a8!, a9!);
        #endregion
    }
}
