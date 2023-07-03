using System;
using System.Runtime.Serialization;

namespace SmartCL
{
    /// <summary>
    /// SmartOpenCL exception
    /// </summary>
    [Serializable]
    public class CLException : Exception
    {
        #region Properties
        /// <summary>
        /// The error which generated the exception
        /// </summary>
        public CLError Error { get; }
        #endregion
        #region Methods
        /// <summary>
        /// Initializes a new instance of the CLException class with a specified error
        /// message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// (Nothing in Visual Basic) if no inner exception is specified.</param>
        private CLException(CLError error, string message, Exception innerException) : base(message, innerException)
        {
            Error = error;
        }
        /// <summary>
        /// Initializes a new instance of the CLException class with a specified error.
        /// </summary>
        /// <param name="error">The error.</param>
        public CLException(CLError error) : this(error, null!, null!)
        {
            Error = error;
        }
        /// <summary>
        /// Initializes a new instance of the CLException class with a specified error and message.
        /// </summary>
        /// <param name="message">The optional message that describes the error.</param>
        public CLException(CLError error, string message = null!) : base(message)
        {
            Error = error;
        }
        /// <summary>
        /// Initializes a new instance of the CLException class with a
        /// a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// (Nothing in Visual Basic) if no inner exception is specified.</param>
        public CLException(CLError error, Exception innerException) : this(error, null!, innerException)
        {
        }
        /// <summary>
        /// Initializes a new instance of the CLException class with serialized data.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.</param>
        protected CLException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        #endregion
    }
}
