using System;

namespace SmartCL
{
    [Flags]
    internal enum CLMemFlags : ulong
    {
        None = 0x0uL,
        ReadWrite = 0x1uL,
        WriteOnly = 0x2uL,
        ReadOnly = 0x4uL,
        UseHostPtr = 0x8uL,
        AllocHostPtr = 0x10uL,
        CopyHostPtr = 0x20uL,
        HostWriteOnly = 0x80uL,
        HostReadOnly = 0x100uL,
        HostNoAccess = 0x200uL,
        SvmFineGrainBuffer = 0x400uL,
        SvmAtomics = 0x800uL,
        KernelReadAndWrite = 0x1000uL,
        ExtHostPtrQCom = 0x20000000uL,
        UseUncachedCpuMemoryImg = 0x4000000uL,
        UseCachedCpuMemoryImg = 0x8000000uL,
        UseGrallocPtrImg = 0x10000000uL,
        NoAccessIntel = 0x1000000uL,
        AccessFlagsUnrestrictedIntel = 0x2000000uL,
        ForceHostMemoryIntel = 0x100000uL,
        ProtectedAllocArm = 0x1000000000uL
    }
}
