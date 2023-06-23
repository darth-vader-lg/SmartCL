using System;

namespace SmartCL
{
    [Flags]
    internal enum CLMapFlags : ulong
    {
        None = 0x0uL,
        Read = 0x1uL,
        Write = 0x2uL,
        WriteInvalidateRegion = 0x4uL
    }
}
