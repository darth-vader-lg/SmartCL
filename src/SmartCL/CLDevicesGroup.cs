using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SmartCL
{
    /// <summary>
    /// Context devices list
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class CLDevicesGroup : IReadOnlyList<CLDevice>
    {
        #region Fields
        /// <summary>
        /// Array of devices
        /// </summary>
        private readonly CLDevice[] devices;
        #endregion
        #region Properties
        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => devices.Length;
        /// <summary>
        /// Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index in the read-only list.</returns>
        public CLDevice this[int index] => devices[index];
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="devices">Set of devices</param>
        /// <exception cref="ArgumentNullException">devices is null</exception>
        /// <exception cref="ArgumentException">devices are not in the same platform</exception>
        public CLDevicesGroup(IEnumerable<CLDevice> devices)
        {
            if (devices == null)
                throw new ArgumentNullException(nameof(devices), "Parameter cannot be null");
            this.devices = devices.ToArray();
            if (this.devices.Length > 0 && !this.devices.All(d => d.Platform.ID == this.devices[0].Platform.ID))
                throw new ArgumentException("All devices must be in the same platform", nameof(devices));
        }
        /// <summary>
        /// Create a context
        /// </summary>
        /// <returns>The created context</returns>
        public CLDisposableContext CreateContext() => CLDisposableContext.Create(this);
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<CLDevice> GetEnumerator()
        {
            foreach (var device in devices)
                yield return device;
        }
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return devices.GetEnumerator();
        }

        private string GetDebuggerDisplay()
        {
            var sb = new StringBuilder();
            foreach (var device in devices) {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(device.DeviceType);
            }
            return $"[{sb}]";
        }
        #endregion
    }
}
