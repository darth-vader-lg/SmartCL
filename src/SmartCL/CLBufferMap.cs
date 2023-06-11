using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Silk.NET.OpenCL;

namespace SmartCL
{
    public class CLBufferMap<T> : IDisposable, IEnumerable<T> where T : struct
    {
        #region Fields
        /// <summary>
        /// Buffer
        /// </summary>
        private CLBuffer<T> buffer;
        /// <summary>
        /// Pointer to the mapped area
        /// </summary>
        private readonly IntPtr ptr;
        #endregion
        #region Properties
        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns></returns>
        unsafe public T this[int index]
        {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((T*)ptr)[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => ((T*)ptr)[index] = value;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        }
        /// <summary>
        /// Underlying span
        /// </summary>
        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe {
                    return new((void*)ptr, buffer.Length);
                }
            }
        }
        #endregion
        #region Methods
        /// <summary>
        /// Private constructor
        /// </summary>
        private CLBufferMap()
        {
            buffer = null!;
            ptr = IntPtr.Zero;
        }
        /// <summary>
        /// Finalizer
        /// </summary>
        ~CLBufferMap()
        {
            Dispose(disposing: false);
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="buffer">Buffer to access</param>
        /// <param name="access">Type of access</param>
        internal CLBufferMap(CLBuffer<T> buffer, CLAccess access)
        {
            this.buffer = buffer;
            var mapFlags = access switch
            {
                CLAccess.Const => MapFlags.WriteInvalidateRegion,
                CLAccess.WriteOnly => MapFlags.WriteInvalidateRegion,
                CLAccess.ReadOnly => MapFlags.Read,
                CLAccess.ReadWrite => MapFlags.Read | MapFlags.Write, // TODO: Verify if it's possible the read/write combination after OpenCL 1.2
                _ => throw new CLException("Invalid access type for the buffer"),
            };
            unsafe {
                ptr = (IntPtr)buffer.CL.Api.EnqueueMapBuffer(buffer.Program.Queue, buffer.ID, true, mapFlags, 0, buffer.size, 0, null, null, out var result);
                CL.CheckResult(result, "Cannot map buffer");
            }
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            // Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose operation
        /// </summary>
        /// <param name="disposing">Programmatically dispose</param>
        protected virtual void Dispose(bool disposing)
        {
            if (buffer == null)
                return;
            try {
                unsafe {
                    var result = buffer.CL.Api.EnqueueUnmapMemObject(buffer.Program.Queue, buffer.ID, (void*)ptr, 0, null, null);
                    CL.CheckResult(result, "Cannot unmap buffer");
                }
            }
            finally {
                buffer = null!;
            }
        }
        /// <summary>
        /// Enumerator
        /// </summary>
        /// <returns>The enumerator</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var (i, iLen) = (0, buffer.Length); i < iLen; ++i)
                yield return this[i];
        }
        /// <summary>
        /// Enumerator
        /// </summary>
        /// <returns>The enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
        #endregion
    }
}
