using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartCL
{
    /// <summary>
    /// Represent a source code
    /// </summary>
    public class CLSource
    {
        #region Properties
        /// <summary>
        /// A virtual path for the code
        /// </summary>
        public string? Path { get; set; }
        /// <summary>
        /// Lines of code
        /// </summary>
        public string[]? Code { get; set; }
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lines">Lines of code</param>
        public CLSource(IEnumerable<string>? lines)
        {
            if (lines == null) {
                Code = Array.Empty<string>();
                return;
            }
            Code = lines.Select(line => (line ?? string.Empty) + Environment.NewLine).ToArray();
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">A virtual path for the code</param>
        /// <param name="lines">Lines of code</param>
        public CLSource(string? path, IEnumerable<string>? lines) : this(lines)
        {
            Path = path;
        }
        #endregion
    }
}
