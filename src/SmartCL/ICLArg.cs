using System;

namespace SmartCL
{
    /// <summary>
    /// Interface for the CL Types
    /// </summary>
    public interface ICLArg
    {
        #region Properties
        /// <summary>
        /// Access type
        /// </summary>
        CLAccess Access { get; }
        /// <summary>
        /// Primitive type of variable or element of an array
        /// </summary>
        Type Type { get; }
        /// <summary>
        /// Initial value
        /// </summary>
        object Value { get; internal set; }
        #endregion
    }
}
