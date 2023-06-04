using System.Diagnostics;
using Xunit.Abstractions;

namespace Common.Tests
{
    public partial class BaseEnvironment : ITestOutputHelper
    {
        #region Fields
        /// <summary>
        /// Helper for the test output messages
        /// </summary>
        [NonSerialized]
        private readonly ITestOutputHelper output;
        #endregion
        #region Properties
        /// <summary>
        /// Root path of the folder containing test workspace's data
        /// </summary>
        static protected string DataFolder => Path.GetFullPath(Path.Combine(ProjectInfo.ProjectPath, "..", "..", "data"));
        /// <summary>
        /// Root path of the folder containing test workspace's data
        /// </summary>
        static protected string WorkspaceFolder => Path.GetFullPath(Path.Combine(ProjectInfo.ProjectPath, "..", "data"));
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="output">Optional output interface</param>
        public BaseEnvironment(ITestOutputHelper output = null!) => this.output = output ?? this;
        /// <summary>
        /// Do a save action without exception consequence
        /// </summary>
        /// <param name="action">Action to do</param>
        public static void SafeAction(Action action)
        {
            try {
                action();
            }
            catch (Exception exc) {
                try {
                    Debug.WriteLine(exc);
                }
                catch {
                }
            }
        }
        /// <summary>
        /// Write the output to the tracer
        /// </summary>
        /// <param name="message">Message to output</param>
        public void WriteLine(string message)
        {
            if (output != this) {
                output.WriteLine(message);
                Debug.WriteLine(message);
            }
            else
                Trace.WriteLine(message);
        }
        /// <summary>
        /// Write the output to the tracer
        /// </summary>
        /// <param name="format">Format string</param>
        /// <param name="args">Arguments</param>
        void ITestOutputHelper.WriteLine(string format, params object[] args)
        {
            if (output != this) {
                output.WriteLine(format, args);
                Debug.WriteLine(format, args);
            }
            else
                Trace.WriteLine(string.Format(format, args));
        }
        #endregion
    }
}
