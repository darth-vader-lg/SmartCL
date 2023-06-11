namespace SmartCL
{
    /// <summary>
    /// Extensions for CL argument creation
    /// </summary>
    public static class CLArgExtensions
    {
        #region Methods
        /// <summary>
        /// Return a variable definition for a standard type value
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value">Value</param>
        /// <returns>The value as a variable definition</returns>
        public static CLArg<T> AsCLArg<T>(this T value) where T : struct
        {
            return new(CLAccess.Const, value);
        }
        /// <summary>
        /// Return a variable definition for a buffer
        /// </summary>
        /// <typeparam name="T">Type of the values in the buffer</typeparam>
        /// <param name="buffer">Buffer</param>
        /// <returns>The buffer as a variable definition</returns>
        public static CLArg<CLBuffer<T>> AsCLArg<T>(this CLBuffer<T> buffer) where T : struct
        {
            return new(buffer?.Access ?? CLAccess.ReadWrite, buffer!);
        }
        /// <summary>
        /// Return a variable definition for a standard type array
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Array of standard values</param>
        /// <param name="access">Access type for the array</param>
        /// <returns>The array as a variable definition</returns>
        public static CLArg<T[]> AsCLArg<T>(this T[] array, CLAccess access = CLAccess.ReadWrite) where T : struct
        {
            return new(access, array);
        }
        /// <summary>
        /// Return a variable definition for a readonly standard type array
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Array of standard values</param>
        /// <returns>The array as a variable definition</returns>
        public static CLArg<T[]> AsCLArgR<T>(this T[] array) where T : struct
        {
            return new(CLAccess.ReadOnly, array);
        }
        /// <summary>
        /// Return a variable definition for a read/write standard type array
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Array of standard values</param>
        /// <returns>The array as a variable definition</returns>
        public static CLArg<T[]> AsCLArgRW<T>(this T[] array) where T : struct
        {
            return new(CLAccess.ReadWrite, array);
        }
        /// <summary>
        /// Return a variable definition for writeonly standard type array
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Array of standard values</param>
        /// <returns>The array as a variable definition</returns>
        public static CLArg<T[]> AsCLArgW<T>(this T[] array) where T : struct
        {
            return new(CLAccess.WriteOnly, array);
        }
        #endregion
    }
}
