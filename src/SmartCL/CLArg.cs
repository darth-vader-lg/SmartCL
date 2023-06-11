using System;
using System.Diagnostics;
using System.Text;

namespace SmartCL
{
    /// <summary>
    /// CL Variable/argument type
    /// </summary>
    /// <typeparam name="T">Type of variable</typeparam>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public struct CLArg<T> : ICLArg
    {
        #region Properties
        /// <summary>
        /// Access type
        /// </summary>
        public CLAccess Access { get; }
        /// <summary>
        /// Initial value
        /// </summary>
        object ICLArg.Value { readonly get => Value!; set => Value = (T)value ?? default!; }
        /// <summary>
        /// Primitive type of variable or element of an array
        /// </summary>
        public Type Type { get; }
        /// <summary>
        /// Value
        /// </summary>
        public T Value { get; set; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="access">Access type</param>
        public CLArg(CLAccess access) : this(access, typeof(T), default(T)!)
        {
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="access">Access type</param>
        /// <param name="value">Initial value</param>
        public CLArg(CLAccess access, T value) : this(access, typeof(T), value!)
        {
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="access">Access type</param>
        /// <param name="type">Primitive type</param>
        /// <param name="value">Initial value</param>
        private CLArg(CLAccess access, Type type, object value)
        {
            Access = access;
            Type = type;
            if (type.IsArray) {
                if (type.GetElementType().IsArray || !type.GetElementType().IsValueType)
                    throw new ArgumentException("A CL type array can only be a single dimension array of value types");
            }
            else if (!type.IsValueType && (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(CLBuffer<>).GetGenericTypeDefinition()))
                throw new ArgumentException("A CL type can only be a value type");
            Value = (T)value ?? default!;
        }
        /// <summary>
        /// Debug display
        /// </summary>
        /// <returns>The human readable string</returns>
        private readonly string GetDebuggerDisplay()
        {
            if (Type.IsArray) {
                var sb = new StringBuilder();
                sb.Append($"{Access} {Type.Name} ");
                if (Value == null)
                    sb.Append("null");
                else {
                    var array = (Array)(object)Value;
                    for (var i = 0; i < array.Length; i++) {
                        sb.Append(array.GetValue(i));
                        if (i < array.Length - 1)
                            sb.Append(" ");
                        if (sb.Length > 80) {
                            sb.Append("...");
                            break;
                        }
                    }
                }
                return sb.ToString();
            }
            else
                return $"{Access} {Type.Name} {Value}";
        }
        #endregion
    }
}
