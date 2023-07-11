using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SmartCL
{
    /// <summary>
    /// Buffer mapping structure
    /// </summary>
    /// <typeparam name="T">Type of data</typeparam>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public ref partial struct CLBufferMap<T> where T : struct
    {
        #region Fields
        /// <summary>
        /// Buffer
        /// </summary>
        private CLBuffer<T> buffer;
        /// <summary>
        /// Size of each element
        /// </summary>
        private readonly nuint itemSize;
        /// <summary>
        /// Pointer to the mapped area
        /// </summary>
        private nuint ptr;
        /// <summary>
        /// The commands queue
        /// </summary>
        private CLQueue queue;
        #endregion
        #region Properties
        /// <summary>
        /// Length of the map
        /// </summary>
        public int Length { get; }
        /// <summary>
        /// Start of the map in the buffer
        /// </summary>
        public int Start { get; }
        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>A reference to te item</returns>
        public readonly ref T this[int index]
        {
            // Probable compiler bug.
            // It causes NullReferenceException if compiled without this attribute in release mode.
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();
                if (ptr == 0)
                    throw new InvalidOperationException();
                return ref Unsafe.AddByteOffset(ref Unsafe.NullRef<T>(), ptr + (nuint)index * itemSize);
            }
        }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">Buffer to access</param>
        /// <param name="access">Type of access</param>
        /// <param name="start">Start in the buffer (as items index)</param>
        /// <param name="length">Length of the view (as items count)</param>
        internal CLBufferMap(CLQueue queue, CLBuffer<T> buffer, CLAccess access, int start, int length)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (!queue.Valid)
                throw new CLException(CLError.InvalidCommandQueue);
            if (queue.Device == null || !queue.Device.Valid)
                throw new CLException(CLError.InvalidDevice);
            if (!buffer.Valid)
                throw new CLException(CLError.InvalidMemObject);
            itemSize = (nuint)Unsafe.SizeOf<T>();
            if (start < 0 || start > (int)(buffer.size / itemSize))
                throw new ArgumentOutOfRangeException("Trying to map outside the boundaries");
            if (length < 0 || (start + length) > (int)(buffer.size / itemSize))
                throw new ArgumentOutOfRangeException("Trying to map outside the boundaries");
            if (queue.Device.Context != buffer.Context)
                throw new InvalidOperationException("The queue and the buffer must share the same context");
            this.buffer = buffer;
            Start = start;
            Length = length;
            var mapFlags = access switch
            {
                CLAccess.Const => CLMapFlags.WriteInvalidateRegion,
                CLAccess.WriteOnly => CLMapFlags.WriteInvalidateRegion,
                CLAccess.ReadOnly => CLMapFlags.Read,
                CLAccess.ReadWrite => CLMapFlags.Read | CLMapFlags.Write, // TODO: Verify if it's possible the read/write combination after OpenCL 1.2
                _ => throw new ArgumentException("Invalid access type for the buffer", nameof(access)),
            };
            ptr = (nuint)CLBuffer.EnqueueMapBuffer(
                queue.ID,
                buffer.ID,
                true,
                mapFlags,
                (nuint)start * itemSize,
                (nuint)length * itemSize,
                0,
                null!,
                ref Unsafe.NullRef<nint>(),
                out var result).ToInt64();
            CL.Assert(result, "Cannot map buffer");
            this.queue = queue;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            try {
                if (ptr != 0 && queue != null && queue.Valid && buffer != null && buffer.Valid) {
                    var result = CLBuffer.EnqueueUnmapMemObject(
                        queue.ID,
                        buffer.ID,
                        new IntPtr((long)ptr),
                        0,
                        null!,
                        ref Unsafe.NullRef<nint>());
                    CL.Assert(result, "Cannot unmap buffer");
                }
            }
            finally {
                ptr = 0;
                buffer = null!;
                queue = null!;
            }
        }
        /// <summary>
        /// Debug display
        /// </summary>
        /// <returns>The human readable string</returns>
        private readonly string GetDebuggerDisplay()
        {
            return $"Start={Start}, Length={Length}";
        }
        /// <summary>
        /// Enumerator
        /// </summary>
        /// <returns>The enumerator</returns>
        public readonly CLBufferMap<T>.Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        /// <summary>
        /// Convert the map to array of managed items
        /// </summary>
        /// <returns>The array</returns>
        public readonly T[] ToArray()
        {
            if (buffer == null || !buffer.Valid)
                throw new InvalidOperationException("Invalid buffer");
            var result = new T[buffer.Length];
            var i = 0;
            foreach (ref var item in this)
                result[i++] = item;
            return result;
        }
        #endregion
    }

    /// <summary>
    /// Enumerator
    /// </summary>
    public ref partial struct CLBufferMap<T> where T : struct
    {
        public ref struct Enumerator
        {
            #region Fields
            /// <summary>
            /// Current address
            /// </summary>
            private nuint currentAddr;
            /// <summary>
            /// Current index
            /// </summary>
            private int currentIx;
            /// <summary>
            /// Owner map
            /// </summary>
            private readonly CLBufferMap<T> owner;
            #endregion
            #region Methods
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="owner">Owner map</param>
            public Enumerator(CLBufferMap<T> owner)
            {
                this.owner = owner;
                currentIx = -1;
                currentAddr = owner.ptr;
            }
            /// <summary>
            /// Current element
            /// </summary>
            public readonly ref T Current
            {
                get
                {
                    if (owner.buffer == null || currentIx < 0 || currentIx >= owner.buffer.Length)
                        throw new InvalidOperationException();
                    ref var result = ref Unsafe.AddByteOffset(ref Unsafe.NullRef<T>(), currentAddr);
                    return ref result;
                }
            }
            /// <summary>
            /// Move to next element
            /// </summary>
            /// <returns>true if inside boundaries</returns>
            public bool MoveNext()
            {
                if (owner.buffer == null)
                    throw new InvalidOperationException();
                if (currentIx < owner.buffer.Length) {
                    currentAddr += owner.itemSize;
                    return ++currentIx < owner.buffer.Length;
                }
                return false;
            }
            #endregion
        }
    }
}
