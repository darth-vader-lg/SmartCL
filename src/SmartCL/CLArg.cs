using System;

namespace SmartCL
{
    /// <summary>
    /// Argument
    /// </summary>
    public abstract class CLArg
    {
        #region Methods
        /// <summary>
        /// Readonly argument from value
        /// </summary>
        /// <typeparam name="T">Value's type</typeparam>
        /// <param name="value">Value</param>
        /// <returns>A readonly argument</returns>
        public static CLArgR<T> R<T>(T value) => new(value);
        /// <summary>
        /// Read/write argument from value
        /// </summary>
        /// <typeparam name="T">Value's type</typeparam>
        /// <param name="value">Value</param>
        /// <returns>A read/write argument</returns>
        public static CLArgRW<T> RW<T>(T value) => new(value);
        /// <summary>
        /// Writeonly argument from value
        /// </summary>
        /// <typeparam name="T">Value's type</typeparam>
        /// <param name="value">Value</param>
        /// <returns>A writeonly argument</returns>
        public static CLArg<T> W<T>(T value) => new(value, CLAccess.WriteOnly);
        #endregion
    }

    /// <summary>
    /// Argument
    /// </summary>
    /// <typeparam name="T">Type of the argument</typeparam>
    public class CLArg<T> : ICLArg
    {
        #region Fields
        /// <summary>
        /// Value
        /// </summary>
        private object value;
        #endregion
        #region Properties
        /// <summary>
        /// Access type
        /// </summary>
        public CLAccess Access { get; }
        /// <summary>
        /// Value
        /// </summary>
        object ICLArg.Value { get => value; set => this.value = value; }
        /// <summary>
        /// Value
        /// </summary>
        public Type Type { get; }
        /// <summary>
        /// Value
        /// </summary>
        public T Value { get => (T)value!; set => this.value = value!; }
        #endregion
        #region Methods
        /// <summary>
        /// Argument's value
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="access">Access type</param>
        public CLArg(T value = default(T)!, CLAccess access = CLAccess.WriteOnly)
        {
            Access = access;
            Type = typeof(T);
            this.value = value!;
        }
        /// <summary>
        /// Convertion of T to CLArg<T>
        /// </summary>
        /// <param name="arg">Argument</param>
        public static implicit operator CLArg<T>(T arg) => new(arg);
        /// <summary>
        /// Convertion of CLArg<T> to T
        /// </summary>
        /// <param name="arg">Argument</param>
        public static implicit operator T(CLArg<T> arg) => arg.Value;
        /// <summary>
        /// Readonly argument from value
        /// </summary>
        /// <typeparam name="T">Value's type</typeparam>
        /// <param name="value">Value</param>
        /// <returns>A readonly argument</returns>
        public static CLArgR<T> R(T value) => new(value);
        /// <summary>
        /// Read/write argument from value
        /// </summary>
        /// <typeparam name="T">Value's type</typeparam>
        /// <param name="value">Value</param>
        /// <returns>A read/write argument</returns>
        public static CLArgRW<T> RW(T value) => new(value);
        /// <summary>
        /// Writeonly argument from value
        /// </summary>
        /// <typeparam name="T">Value's type</typeparam>
        /// <param name="value">Value</param>
        /// <returns>A writeonly argument</returns>
        public static CLArg<T> W(T value) => new(value, CLAccess.WriteOnly);
        #endregion
    }

    /// <summary>
    /// Read only argument
    /// </summary>
    /// <typeparam name="T">Type of the argument</typeparam>
    public sealed class CLArgR<T> : CLArg<T>
    {
        #region Methods
        /// <summary>
        /// Argument's value
        /// </summary>
        /// <param name="value">Value</param>
        public CLArgR(T value = default!) : base(value, CLAccess.ReadOnly) { }
        #endregion
    }

    /// <summary>
    /// Read write argument
    /// </summary>
    /// <typeparam name="T">Type of the argument</typeparam>
    public sealed class CLArgRW<T> : CLArg<T>
    {
        #region Methods
        /// <summary>
        /// Argument's value
        /// </summary>
        /// <param name="value">Value</param>
        public CLArgRW(T value = default!) : base(value, CLAccess.ReadWrite) { }
        #endregion
    }

    /// <summary>
    /// Write only argument
    /// </summary>
    /// <typeparam name="T">Type of the argument</typeparam>
    public sealed class CLArgW<T> : CLArg<T>
    {
        #region Methods
        /// <summary>
        /// Argument's value
        /// </summary>
        /// <param name="value">Value</param>
        public CLArgW(T value = default!) : base(value, CLAccess.WriteOnly) { }
        #endregion
    }
}
