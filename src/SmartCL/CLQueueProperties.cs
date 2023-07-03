using System;

namespace SmartCL
{
    /// <summary>
    /// Command queue properties identifiers
    /// </summary>
    [Flags]
    internal enum CLQueueProperties : ulong
    {
        None = 0x0uL,
        OutOfOrderExecModeEnable = 0x1uL,
        ProfilingEnable = 0x2uL,
        OnDevice = 0x4uL,
        OnDeviceDefault = 0x8uL,
        ThreadLocalExecEnableIntel = 0x80000000uL,
        NoSyncOperationsIntel = 0x20000000uL
    }
}
