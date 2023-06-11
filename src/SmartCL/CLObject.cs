using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SmartCL
{
    /// <summary>
    /// Represent the base for OpenCL objects
    /// </summary>
    public abstract class CLObject : IDisposable
    {
        #region Fields
        /// <summary>
        /// The CL context
        /// </summary>
        internal CL CL { get; private set; }
        /// <summary>
        /// The ID of the object
        /// </summary>
        internal nint ID { get; private set; }
        #endregion
        #region Delegates
        /// <summary>
        /// Delegate for info readers
        /// </summary>
        /// <typeparam name="InfoType">Type of information</typeparam>
        /// <param name="id">The ID of the object</param>
        /// <param name="paramName">The name of the parameter</param>
        /// <param name="paramValueSize">The size of the parameter</param>
        /// <param name="paramValue">The value of the parameter</param>
        /// <param name="paramValueSizeRet">The returned size</param>
        /// <returns></returns>
        unsafe internal delegate int GetInfoDelegate<InfoType>(nint id, InfoType paramName, nuint paramValueSize, void* paramValue, out nuint paramValueSizeRet);
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cl">The CL context</param>
        /// <param name="id">The ID of the object</param>
        protected CLObject(CL cl, nint id)
        {
            ID = id;
            CL = cl;
        }
        /// <summary>
        /// Finalizer
        /// </summary>
        ~CLObject()
        {
            Dispose(disposing: false);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose operations
        /// </summary>
        /// <param name="disposing">Programmatically disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (ID == 0)
                return;
            ID = 0;
        }
        /// <summary>
        /// Get information in string format
        /// </summary>
        /// <typeparam name="InfoType">Type of information</typeparam>
        /// <param name="id">The ID of the object</param>
        /// <param name="paramName">The name of the parameter</param>
        /// <param name="getInfoDelegate">Delegate of info reader</param>
        /// <returns></returns>
        unsafe internal static string GetStringInfo<InfoType>(nint id, InfoType paramName, GetInfoDelegate<InfoType> getInfoDelegate)
        {
            byte[] arrayInfo = GetArrayInfo<InfoType, byte>(id, paramName, getInfoDelegate);
            char[] chars = Encoding.ASCII.GetChars(arrayInfo, 0, arrayInfo.Length);
            string text = new string(chars);
            char[] trimChars = new char[1];
            return text.TrimEnd(trimChars);
        }
        /// <summary>
        /// Get the information in array format
        /// </summary>
        /// <typeparam name="InfoType">Type of information</typeparam>
        /// <typeparam name="QueriedType">Type of returned value</typeparam>
        /// <param name="id">The ID of the object</param>
        /// <param name="paramName">Name of the info parameter</param>
        /// <param name="getInfoDelegate">Delegate of info reader</param>
        /// <returns></returns>
        unsafe internal static QueriedType[] GetArrayInfo<InfoType, QueriedType>(nint id, InfoType paramName, GetInfoDelegate<InfoType> getInfoDelegate)
        {
            CL.CheckResult(getInfoDelegate(id, paramName, 0, null, out var paramValueSizeRet), "Cannot read the size of the info");
            QueriedType[] array = new QueriedType[paramValueSizeRet / (uint)Marshal.SizeOf(typeof(QueriedType))];
            GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try {
                CL.CheckResult(getInfoDelegate(id, paramName, paramValueSizeRet, gCHandle.AddrOfPinnedObject().ToPointer(), out paramValueSizeRet), "Cannot get the info");
            }
            finally {
                gCHandle.Free();
            }
            return array;
        }
        #endregion
    }
}
