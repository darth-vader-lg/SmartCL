using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SmartCL
{
    /// <summary>
    /// OpenCL buffer base class bindings
    /// </summary>
    public abstract class CLBuffer : CLObject, IDisposable
    {
        #region Fields
        /// <summary>
        /// Size of the buffer in bytes
        /// </summary>
        internal readonly nuint size;
        #endregion
        #region Properties
        /// <summary>
        /// Access type
        /// </summary>
        public CLAccess Access { get; }
        /// <summary>
        /// Length of the buffer (elements count)
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }
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
        /// <param name="id">The ID of the object</param>
        /// <param name="access">Access type</param>
        /// <param name="size">Size of the buffer in bytes</param>
        /// <param name="length">Length of buffer (elements count)</param>
        protected CLBuffer(CLContext context, nint id, CLAccess access, nuint size, int length) : base(id)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Buffer length must be >= 0");
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Buffer size must be >= 0");
            Access = access;
            Length = length;
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!context.Valid)
                throw new CLException(CLError.InvalidContext);
            Context = context;
            this.size = size;
        }
        /// <summary>
        /// Finalizer
        /// </summary>
        ~CLBuffer()
        {
            Dispose(disposing: false);
        }
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clCreateBuffer")]
        protected private static extern nint CreateBuffer(
            [In] nint context,
            [In] CLMemFlags flags,
            [In] nuint size,
            [In] IntPtr host_ptr,
            [Out] out CLError errcode_ret);
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically dispose</param>
        protected virtual void Dispose(bool disposing)
        {
            try {
                if (Valid)
                    ReleaseMemObject(ID);
            }
            catch { }
            Context = null!;
            InvalidateObject();
        }
        /// <summary>
        /// Enqueue a buffer mapping
        /// </summary>
        /// <param name="commandQueue"></param>
        /// <param name="buffer"></param>
        /// <param name="blockingMap"></param>
        /// <param name="mapFlags"></param>
        /// <param name="offset"></param>
        /// <param name="cb"></param>
        /// <param name="numEventsInWaitList"></param>
        /// <param name="eventWaitList"></param>
        /// <param name="event"></param>
        /// <param name="errCodeRet"></param>
        /// <returns></returns>
        [DllImport("OpenCL", EntryPoint = "clEnqueueMapBuffer")]
        internal static extern IntPtr EnqueueMapBuffer(
            [In] nint commandQueue,
            [In] nint buffer,
            [In] bool blockingMap,
            [In] CLMapFlags mapFlags,
            [In] nuint offset,
            [In] nuint cb,
            [In] uint numEventsInWaitList,
            [In] nint[] eventWaitList,
            [In, Out] ref nint @event,
            [Out] out CLError errCodeRet);
        /// <summary>
        /// Enqueue a memory object unmapping
        /// </summary>
        /// <param name="commandQueue"></param>
        /// <param name="memObj"></param>
        /// <param name="mappedPtr"></param>
        /// <param name="numEventsInWaitList"></param>
        /// <param name="eventWaitList"></param>
        /// <param name="event"></param>
        /// <returns></returns>
        [DllImport("OpenCL", EntryPoint = "clEnqueueUnmapMemObject")]
        internal static extern CLError EnqueueUnmapMemObject(
            [In] nint commandQueue,
            [In] nint memObj,
            [In] IntPtr mappedPtr,
            [In] uint numEventsInWaitList,
            [In] nint[] eventWaitList,
            [In, Out] ref nint @event);
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clReleaseMemObject")]
        protected private static extern CLError ReleaseMemObject([In] nint memobj);
        #endregion
    }

    /// <summary>
    /// OpenCL generic buffer
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class CLBuffer<T> : CLBuffer where T : struct
    {
        #region Fields
        /// <summary>
        /// Host pinned array
        /// </summary>
        private readonly GCHandle hostArrayHandle;
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Owner context</param>
        /// <param name="id">Id of the buffer</param>
        /// <param name="itemSize">Size of each element in the buffer</param>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        /// <param name="hostArrayhandle">Host supplied array handle</param>
        private CLBuffer(CLContext context, nint id, int itemSize, int length, CLAccess access, GCHandle hostArrayhandle) :
            base(context, id, access, (nuint)(itemSize * length), length)
        {
            hostArrayHandle = hostArrayhandle;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Owner context</param>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        /// <param name="hostArray">Host supplied array</param>
        internal static CLBuffer<T> Create(CLContext context, int length, CLAccess access, T[] hostArray = null!)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            return new CLBuffer<T>(context, GetId(context, length, access, hostArray, out var handle), Unsafe.SizeOf<T>(), length, access, handle);
        }
        /// <summary>
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically dispose</param>
        protected override void Dispose(bool disposing)
        {
            try {
                if (hostArrayHandle.IsAllocated)
                    hostArrayHandle.Free();
            }
            catch (Exception) {
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Debug display
        /// </summary>
        /// <returns>The human readable string</returns>
        private string GetDebuggerDisplay()
        {
            return $"{typeof(T)}[{Length}] (size in bytes={size}{(hostArrayHandle.IsAllocated ? ", host" : "")})";
        }
        /// <summary>
        /// Create the buffer and return the ID
        /// </summary>
        /// <param name="context">Owner context</param>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        /// <param name="hostArray">User supplied array or null for device allocated memory</param>
        /// <returns>The buffer ID</returns>
        /// <exception cref="CLException"></exception>
        private static nint GetId(CLContext context, int length, CLAccess access, T[] hostArray, out GCHandle hostArrayHandle)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!context.Valid)
                throw new CLException(CLError.InvalidContext);
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Buffer length must be >= 0");
            var memFlags = access switch
            {
                CLAccess.Const => CLMemFlags.ReadOnly,
                CLAccess.WriteOnly => CLMemFlags.ReadOnly,
                CLAccess.ReadOnly => CLMemFlags.WriteOnly,
                CLAccess.ReadWrite => CLMemFlags.ReadWrite,
                _ => throw new ArgumentException($"Invalid access type {access}"),
            };
            if (hostArray == null) {
                memFlags |= CLMemFlags.AllocHostPtr;
                hostArrayHandle = default;
            }
            else
                hostArrayHandle = GCHandle.Alloc(hostArray, GCHandleType.Pinned);
            var id = CreateBuffer(
                context.ID,
                memFlags,
                (nuint)(Unsafe.SizeOf<T>() * length),
                hostArrayHandle.IsAllocated ? hostArrayHandle.AddrOfPinnedObject() : IntPtr.Zero,
                out var result);
            CL.Assert(result, "Cannot create the buffer");
            return id;
        }
        #endregion
    }
}
