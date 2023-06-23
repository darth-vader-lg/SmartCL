namespace SmartCL
{
    /// <summary>
    /// Context properties identifiers
    /// </summary>
    internal enum CLContextProperties : ulong
    {
        Platform = 4228uL,
        InteropUserSync = 4229uL,
        TerminateKhr = 8242uL,
        PrintfCallbackArm = 16560uL,
        PrintfBuffersizeArm = 16561uL,
        ShowDiagnosticsIntel = 16646uL,
        DiagnosticsLevelAllIntel = 0xFFuL,
        DiagnosticsLevelGoodIntel = 1uL,
        DiagnosticsLevelBadIntel = 2uL,
        DiagnosticsLevelNeutralIntel = 4uL
    }
}
