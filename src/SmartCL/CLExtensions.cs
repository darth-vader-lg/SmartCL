using System.Linq;

namespace SmartCL
{
    /// <summary>
    /// Miscellaneous extension methods
    /// </summary>
    public static class CLExtensions
    {
        #region Methods
        /// <summary>
        /// Execute a source code, in a device, for the specified array of elements
        /// </summary>
        /// <typeparam name="T">Type of elements</typeparam>
        /// <param name="deviceContext">Device context of execution</param>
        /// <param name="array">The array with data</param>
        /// <param name="sourceCode">The source code to be run on the device</param>
        /// <param name="args">Arguments</param>
        /// <remarks>
        /// The kernel function must be called with reserved name "main".
        /// The first argument passed to the kernel will be always the array.
        /// The the args list will start from the second parameter passed to the kernel
        /// </remarks>
        public static void ExecuteOnDevice<T>(this T[] array, CLDeviceContext deviceContext, string[] sourceCode, params object[] args) where T : struct
        {
            deviceContext.Execute(sourceCode, "main", array.Length, new[] { array }.Concat(args).ToArray());
        }
        #endregion
    }
}
