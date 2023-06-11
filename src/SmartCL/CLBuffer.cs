using System.Runtime.CompilerServices;
using Silk.NET.OpenCL;

namespace SmartCL
{
    /// <summary>
    /// OpenCL generic buffer
    /// </summary>
    public class CLBuffer<T> : CLObject where T : struct
    {
        #region Fields
        /// <summary>
        /// Host pinned array
        /// </summary>
        private readonly CLHandle hostArrayHandle;
        /// <summary>
        /// Size of the buffer in bytes
        /// </summary>
        internal nuint size;
        #endregion
        #region Properties
        /// <summary>
        /// Access type
        /// </summary>
        public CLAccess Access { get; }
        /// <summary>
        /// Length of the buffer
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
        /// <param name="id">Id of the buffer</param>
        /// <param name="itemSize">Size of each element in the buffer</param>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        private CLBuffer(CLProgram program, nint id, int itemSize, int length, CLAccess access, CLHandle hostArray) : base(program.CL, id)
        {
            Access = access;
            Length = length;
            Program = program;
            hostArrayHandle = hostArray;
            size = (nuint)(itemSize * length);
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Owner program</param>
        /// <param name="length">Length of buffer</param>
        /// <param name="access">Access type</param>
        internal CLBuffer(CLProgram program, int length, CLAccess access, T[] hostArray = null!) :
            this(program, GetId(program, length, access, hostArray, out var handle), Unsafe.SizeOf<T>(), length, access, handle)
        {
        }
        /// <summary>
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (ID == 0)
                return;
            CL.Api.ReleaseMemObject(ID);
            hostArrayHandle?.Dispose();
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
        private static nint GetId(CLProgram program, int length, CLAccess access, T[] hostArray, out CLHandle hostArrayHandle)
        {
            var memFlags = access switch
            {
                CLAccess.Const => MemFlags.ReadOnly,
                CLAccess.WriteOnly => MemFlags.ReadOnly,
                CLAccess.ReadOnly => MemFlags.WriteOnly,
                CLAccess.ReadWrite => MemFlags.ReadWrite,
                _ => throw new CLException($"Invalid access type {access}"),
            };
            if (hostArray == null)
                memFlags |= MemFlags.AllocHostPtr;
            unsafe {
                hostArrayHandle = hostArray?.Pin()!;
                var id = program.CL.Api.CreateBuffer(
                    program.Context,
                    memFlags,
                    (nuint)(Unsafe.SizeOf<T>() * length),
                    hostArrayHandle != null ? hostArrayHandle.ToPointer() : null,
                    out var result);
                CL.CheckResult(result, "Cannot create the buffer");
                return id;
            }
        }
        /// <summary>
        /// Map the buffer
        /// </summary>
        /// <param name="access">Access type</param>
        /// <returns>The map of the buffer</returns>
        public CLBufferMap<T> Map(CLAccess access = CLAccess.ReadWrite)
        {
            return new(this, access);
        }
        /// <summary>
        /// Map the buffer to read it
        /// </summary>
        /// <returns>The map of the buffer</returns>
        public CLBufferMap<T> MapRead()
        {
            return new(this, CLAccess.ReadOnly);
        }
        /// <summary>
        /// Map the buffer to write it
        /// </summary>
        /// <returns>The map of the buffer</returns>
        public CLBufferMap<T> MapWrite()
        {
            return new(this, CLAccess.WriteOnly);
        }
        #endregion
    }
}
