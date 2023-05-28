using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;

namespace SmartCL
{
    /// <summary>
    /// Extensions for the Type
    /// </summary>
    internal static class TypeExtensions
    {
        #region Fields
        /// <summary>
        /// Type size cache
        /// </summary>
        private static readonly ConcurrentDictionary<Type, int> cache = new();
        #endregion
        #region Methods
        /// <summary>
        /// Get the size of a type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The size</returns>
        internal static int GetSize(this Type type)
        {
            return cache.GetOrAdd(type, _ =>
            {
                var dm = new DynamicMethod("SizeOfType", typeof(int), Array.Empty<Type>());
                ILGenerator il = dm.GetILGenerator();
                il.Emit(OpCodes.Sizeof, type);
                il.Emit(OpCodes.Ret);
                return (int)dm.Invoke(null, null)!;
            });
        }
        /// <summary>
        /// Pin an object
        /// </summary>
        /// <param name="obj">Object to pin</param>
        /// <returns>The pinned object</returns>
        internal static CLHandle Pin(this object obj)
        {
            return new CLHandle(obj);
        }
        #endregion
    }
}
