using IOPath = System.IO.Path;

namespace Common.Tests
{
    /// <summary>
    /// Test file
    /// </summary>
    public class TestData
    {
        #region Fields
        /// <summary>
        /// Path relative to the root
        /// </summary>
        private readonly string path;
        /// <summary>
        /// Root of the path on disk
        /// </summary>
        private readonly string root;
        #endregion
        #region Properties
        /// <summary>
        /// The full path of the destination
        /// </summary>
        public string FullPath => IOPath.GetFullPath(IOPath.Combine(root, path));
        /// <summary>
        /// Mnemonic name of the object
        /// </summary>
        public string Name { get; }
        #endregion
        #region Delegates
        /// <summary>
        /// Custom creator delegate
        /// </summary>
        /// <param name="fullPath">Full path of the object to create</param>
        public delegate void Builder(string fullPath);
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of the object</param>
        /// <param name="root">Root of the path on disk</param>
        /// <param name="path">Path relative to the root</param>
        /// <param name="url">Url for download</param>
        /// <param name="isFolder">Interpreted as folder</param>
        public TestData(string name, string root, string path)
        {
            Name = name;
            this.root = root;
            this.path = path;
        }
        #endregion
    }
}
