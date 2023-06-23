using System;
using System.Runtime.CompilerServices;

namespace SmartCL
{
    public partial class CLBuffer<T>
    {
        /// <summary>
        /// Buffer mapping structure
        /// </summary>
        /// <typeparam name="T">Tipe of data</typeparam>
        public ref partial struct Mapping
        {
            #region Fields
            /// <summary>
            /// Buffer
            /// </summary>
            private readonly CLBuffer<T> buffer;
            /// <summary>
            /// Size of each element
            /// </summary>
            private readonly nuint itemSize;
            /// <summary>
            /// Pointer to the mapped area
            /// </summary>
            private nuint ptr;
            #endregion
            #region Properties
            /// <summary>
            /// Length of the map
            /// </summary>
            public int Length { get; }
            /// <summary>
            /// Indexer
            /// </summary>
            /// <param name="index">Index</param>
            /// <returns>A reference to te item</returns>
            public readonly ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (index < 0 || index > Length)
                        throw new IndexOutOfRangeException();
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
            internal Mapping(CLBuffer<T> buffer, CLAccess access, int start, int length)
            {
                itemSize = (nuint)Unsafe.SizeOf<T>();
                if (start < 0 || start > (int)(buffer.size / itemSize))
                    throw new ArgumentException("Trying to map outside the boundaries", nameof(start));
                if (length < 0 || (start + length) > (int)(buffer.size / itemSize))
                    throw new ArgumentException("Trying to map outside the boundaries", nameof(length));
                this.buffer = buffer;
                Length = length;
                var mapFlags = access switch
                {
                    CLAccess.Const => CLMapFlags.WriteInvalidateRegion,
                    CLAccess.WriteOnly => CLMapFlags.WriteInvalidateRegion,
                    CLAccess.ReadOnly => CLMapFlags.Read,
                    CLAccess.ReadWrite => CLMapFlags.Read | CLMapFlags.Write, // TODO: Verify if it's possible the read/write combination after OpenCL 1.2
                    _ => throw new CLException("Invalid access type for the buffer"),
                };
                ptr = (nuint)CLBuffer.EnqueueMapBuffer(
                    buffer.Program.Queue,
                    buffer.ID,
                    true,
                    mapFlags,
                    (nuint)start * itemSize,
                    (nuint)length * itemSize,
                    0,
                    null!,
                    ref Unsafe.NullRef<nint>(),
                    out var result).ToInt64();
                CL.CheckResult(result, "Cannot map buffer");
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                if (ptr == 0)
                    return;
                try {
                    var result = CLBuffer.EnqueueUnmapMemObject(
                        buffer.Program.Queue,
                        buffer.ID,
                        new IntPtr((long)ptr),
                        0,
                        null!,
                        ref Unsafe.NullRef<nint>());
                    CL.CheckResult(result, "Cannot unmap buffer");
                }
                finally {
                    ptr = 0;
                }
            }
            /// <summary>
            /// Enumerator
            /// </summary>
            /// <returns>The enumerator</returns>
            public readonly CLBuffer<T>.Mapping.Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }
            /// <summary>
            /// Convert the map to array of managed items
            /// </summary>
            /// <returns>The array</returns>
            public readonly T[] ToArray()
            {
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
        public ref partial struct Mapping
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
                private readonly Mapping owner;
                #endregion
                #region Methods
                /// <summary>
                /// Constructor
                /// </summary>
                /// <param name="owner">Owner map</param>
                public Enumerator(Mapping owner)
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
                        if (currentIx < 0 || currentIx >= owner.buffer.Length)
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
}
