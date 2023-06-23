using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SmartCL
{
    /// <summary>
    /// OpenCL buffer base class bindings
    /// </summary>
    public abstract class CLBuffer : CLObject
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
        /// Owner program
        /// </summary>
        public CLProgram Program { get; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="id">The ID of the object</param>
        /// <param name="access">Access type</param>
        /// <param name="size">Size of the buffer in bytes</param>
        /// <param name="length">Length of buffer (elements count)</param>
        protected CLBuffer(CLProgram program, nint id, CLAccess access, nuint size, int length) : base(id)
        {
            Access = access;
            Length = length;
            Program = program;
            this.size = size;
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
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clEnqueueReadBuffer")]
        private static extern CLError EnqueueReadBuffer(
            [In] nint command_queue,
            [In] nint buffer,
            [In, MarshalAs(UnmanagedType.Bool)] bool blocking_read,
            [In] nuint offset,
            [In] nuint cb,
            [In] IntPtr ptr,
            [In] uint num_events_in_wait_list,
            [In] nint[] event_wait_list,
            [In, Out] ref nint new_event);
        /// <summary>
        /// See the OpenCL specification.
        /// </summary>
        [DllImport("OpenCL", EntryPoint = "clEnqueueWriteBuffer")]
        private static extern CLError EnqueueWriteBuffer(
            [In] nint command_queue,
            [In] nint buffer,
            [In, MarshalAs(UnmanagedType.Bool)] bool blocking_write,
            [In] nuint offset,
            [In] nuint cb,
            [In] IntPtr ptr,
            [In] uint num_events_in_wait_list,
            [In] nint[] event_wait_list,
            [In, Out] ref nint new_event);
        /// <summary>
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (ID == 0)
                return;
            ReleaseMemObject(ID);
            base.Dispose(disposing);
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
        /// Generic buffer read method
        /// </summary>
        /// <param name="offset">Offset in the buffer</param>
        /// <param name="size">Total buffer's size</param>
        /// <param name="array">Array of elements</param>
        /// <returns>Error code</returns>
        internal CLError EnqueueReadBuffer(nuint offset, nuint size, object array)
        {
            Debug.Assert(array != null);
            Debug.Assert(array == null || array.GetType().IsArray);
            Debug.Assert(array == null || array.GetType().GetArrayRank() == 1);
            var h = GCHandle.Alloc(array, GCHandleType.Pinned);
            try {
                return EnqueueReadBuffer(Program.Queue, ID, true, offset, size, h.AddrOfPinnedObject(), 0, null!, ref Unsafe.NullRef<nint>());
            }
            finally {
                h.Free();
            }
        }
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
        /// Generic buffer write method
        /// </summary>
        /// <param name="offset">Offset in the buffer</param>
        /// <param name="size">Total buffer's size</param>
        /// <param name="array">Array of elements</param>
        /// <returns>Error code</returns>
        internal CLError EnqueueWriteBuffer(nuint offset, nuint size, object array)
        {
            Debug.Assert(array != null);
            Debug.Assert(array == null || array.GetType().IsArray);
            Debug.Assert(array == null || array.GetType().GetArrayRank() == 1);
            var h = GCHandle.Alloc(array, GCHandleType.Pinned);
            try {
                return EnqueueWriteBuffer(Program.Queue, ID, true, offset, size, h.AddrOfPinnedObject(), 0, null!, ref Unsafe.NullRef<nint>());
            }
            finally {
                h.Free();
            }
        }
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
    public partial class CLBuffer<T> : CLBuffer where T : unmanaged
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
        /// <param name="program">Owner program</param>
        /// <param name="id">Id of the buffer</param>
        /// <param name="itemSize">Size of each element in the buffer</param>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        /// <param name="hostArrayhandle">Host supplied array handle</param>
        private CLBuffer(CLProgram program, nint id, int itemSize, int length, CLAccess access, GCHandle hostArrayhandle) :
            base(program, id, access, (nuint)(itemSize * length), length)
        {
            hostArrayHandle = hostArrayhandle;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        /// <param name="hostArray">Host supplied array</param>
        internal static CLBuffer<T> Create(CLProgram program, int length, CLAccess access, T[] hostArray = null!)
        {
            return new CLBuffer<T>(program, GetId(program, length, access, hostArray, out var handle), Unsafe.SizeOf<T>(), length, access, handle);
        }
        /// <summary>
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (hostArrayHandle.IsAllocated)
                hostArrayHandle.Free();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Create the buffer and return the ID
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        /// <param name="hostArray">User supplied array or null for device allocated memory</param>
        /// <returns>The buffer ID</returns>
        /// <exception cref="CLException"></exception>
        private static nint GetId(CLProgram program, int length, CLAccess access, T[] hostArray, out GCHandle hostArrayHandle)
        {
            var memFlags = access switch
            {
                CLAccess.Const => CLMemFlags.ReadOnly,
                CLAccess.WriteOnly => CLMemFlags.ReadOnly,
                CLAccess.ReadOnly => CLMemFlags.WriteOnly,
                CLAccess.ReadWrite => CLMemFlags.ReadWrite,
                _ => throw new CLException($"Invalid access type {access}"),
            };
            if (hostArray == null)
                memFlags |= CLMemFlags.AllocHostPtr;
            hostArrayHandle = GCHandle.Alloc(hostArray, GCHandleType.Pinned);
            var id = CreateBuffer(
                program.Context,
                memFlags,
                (nuint)(Unsafe.SizeOf<T>() * length),
                hostArrayHandle.AddrOfPinnedObject(),
                out var result);
            CL.CheckResult(result, "Cannot create the buffer");
            return id;
        }
        /// <summary>
        /// Map the buffer
        /// </summary>
        /// <param name="access">Access type</param>
        /// <param name="start">Start index in the buffer</param>
        /// <param name="length">Length of the map. whole buffer if < 0</param>
        /// <returns>The map of the buffer</returns>
        public Mapping Map(CLAccess access = CLAccess.ReadWrite, int start = 0, int length = -1)
        {
            return new(this, access, start, length >= 0 ? length : Length - start);
        }
        /// <summary>
        /// Map the buffer to read it
        /// </summary>
        /// <param name="start">Start index in the buffer</param>
        /// <param name="length">Length of the map. whole buffer if < 0</param>
        /// <returns>The map of the buffer</returns>
        public Mapping MapRead(int start = 0, int length = -1)
        {
            return Map(CLAccess.ReadOnly, start, length);
        }
        /// <summary>
        /// Map the buffer to write it
        /// </summary>
        /// <param name="start">Start index in the buffer</param>
        /// <param name="length">Length of the map. whole buffer if < 0</param>
        /// <returns>The map of the buffer</returns>
        public Mapping MapWrite(int start = 0, int length = -1)
        {
            return Map(CLAccess.WriteOnly, start, length);
        }
        #endregion
    }
}
