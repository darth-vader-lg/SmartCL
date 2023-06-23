using System;

namespace SmartCL
{
    /// <summary>
    /// Device types
    /// </summary>
    [Flags]
    public enum CLDeviceType : ulong
    {
        None = 0x0uL,
        Default = 0x1uL,
        Cpu = 0x2uL,
        Gpu = 0x4uL,
        Accelerator = 0x8uL,
        All = 0xFFFFFFFFuL,
        Custom = 0x10uL
    }
}
