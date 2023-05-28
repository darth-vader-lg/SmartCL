using System;

namespace SmartCL
{
    /// <summary>
    /// Argument interface
    /// </summary>
    internal interface ICLArg
    {
        #region Properties
        /// <summary>
        /// Access type
        /// </summary>
        CLAccess Access { get; }
        /// <summary>
        /// Value
        /// </summary>
        Type Type { get; }
        /// <summary>
        /// Value
        /// </summary>
        object Value { get; set; }
        #endregion
    }
}
