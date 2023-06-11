using System.Diagnostics;
using SmartCL;

// Operational buffer
var a = new int[1024];
// Verification buffer
var b = new int[a.Length];
// Multiplication factor
var c = 2;

// Prepare the buffers
for (var i = 0; i < a.Length; i++) {
    a[i] = i;
    b[i] = i * c * 2;
}

// Get the first GPU device
var device = CL.GetFirstGpuOrDefault();

// Create a program running on GPU
using var program = device.CreateProgram(new[]
{
    "__kernel void multiply_by(__global int* A, const int c) {",
    "   A[get_global_id(0)] = c * A[get_global_id(0)];",
    "}"
});

// Test kernel call with defined delegate
using var kernel1 = program.CreateKernel("multiply_by", out KernelFunction multiply_by, a.AsCLArg(), c.AsCLArg());
kernel1.Dims = new(a.Length);
multiply_by(a, 2);

// Test kernel call with standard delegate
using var kernel2 = program.CreateKernel("multiply_by", a.AsCLArg(), c.AsCLArg());
kernel2.Dims = new(a.Length);
kernel2.Call(a, 2);

// Check the result
Debug.Assert(a.Zip(b).All(item => item.First == item.Second));

// Create a program which fill an OpenCL direct access buffer
using var program1 = device.CreateProgram(new[]
{
    "__kernel void fill(__global int* buffer) {",
    "   buffer[get_global_id(0)] = get_global_id(0);",
    "}"
});

// Create the kernel and a device buffer
using var fillDevice = program1.CreateKernel("fill", program1.CreateBuffer<int>(a.Length, CLAccess.ReadOnly).AsCLArg());
fillDevice.Dims = new(a.Length);
// Fill the buffer
fillDevice.Invoke();

// Check the result mapping the buffer for reading
using (var map = fillDevice.Arg0.MapRead()) {
    var mapResult = map.Span.ToArray();
    Debug.Assert(mapResult[^1] == mapResult.Length - 1);
}

// Create the kernel and a host buffer
var hostBuf = new int[a.Length];
using var fillHost = program1.CreateKernel("fill", program1.CreateBuffer<int>(hostBuf, CLAccess.ReadOnly).AsCLArg());
fillHost.Dims = new(a.Length);
// Fill the buffer
fillHost.Invoke();

// Check the result mapping the buffer for reading
using (var map = fillHost.Arg0.MapRead()) {
    var mapResult = map.Span.ToArray();
    Debug.Assert(mapResult[^1] == mapResult.Length - 1);
}


// Delegate for kernel function
delegate void KernelFunction(int[] values, int mul);
