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
        #region Methods
        /// <summary>
        /// Initializes a new instance of the SmartOpenCLException class.
        /// </summary>
        public CLException()
        {
        }
        /// <summary>
        /// Initializes a new instance of the SmartOpenCLException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CLException(string message) : base(message)
        {
        }
        /// <summary>
        /// Initializes a new instance of the SmartOpenCLException class with a specified error
        /// message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// (Nothing in Visual Basic) if no inner exception is specified.</param>
        public CLException(string message, Exception innerException) : base(message, innerException)
        {
        }
        /// <summary>
        /// Initializes a new instance of the SmartOpenCLException class with serialized data.
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
