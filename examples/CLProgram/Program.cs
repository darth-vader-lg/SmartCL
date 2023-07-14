using System.Runtime.InteropServices;
using Common.Tests;
using SmartCL;
using static CLKernels;

// Context and device
if (CL.DefaultDevice is var device && device is null)
    throw new Exception("Cannot find a default device");

// ====== Simulation ======
// Call the main function of the simulation library
var data = new int[] { 123 };
Console.WriteLine("Calling main from DLL...");
main(data);

// ====== OpenCL device ======
// Read source files from the DLL project 
var path = Path.Combine(ProjectInfo.ProjectPath, "..", "CLKernels");
var sourcesFiles = Directory.GetFiles(path).Where(path => new[] { ".cpp", ".h" }.Any(ext => Path.GetExtension(path).ToLower() == ext));
var sources = sourcesFiles.Select(file => new CLSource(Path.GetRelativePath(path, file), File.ReadAllLines(file)));
// Create the program, in the device, with all sources
device.Context.CreateProgram(sources.ToArray());
var kdata = new int[] { 456 };
using (var kernel = device.CreateKernel("main", kdata.AsCLArg())) {
    Console.WriteLine("Calling main from SmartCL...");
    kernel.Invoke();
}

// Imports from simulation DLL
static class CLKernels
{
    [DllImport("CLKernels", EntryPoint = "main")]
    public extern static void main(int[] data);
}
