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
    b[i] = i * (int)Math.Pow(c, 3) /* three calls before the check */;
}

// Context and device
if (CL.DefaultDevice is var device && device is null)
    throw new Exception("Cannot find a default device");

// Quick operation on an array
a.ExecuteOnDevice(device, new[]
{
    "__kernel void clmain(__global int* A, const int c) {",
    "   A[get_global_id(0)] = c * A[get_global_id(0)];",
    "}"
}, 2);

// Create a program running on GPU
device.Context.CreateProgram(new[]
{
    "__kernel void multiply_by(__global int* A, const int c) {",
    "   A[get_global_id(0)] = c * A[get_global_id(0)];",
    "}"
});

// Test kernel call with defined delegate
using var kernel1 = device.CreateKernel("multiply_by", out KernelFunction multiply_by, a.AsCLArg(), c.AsCLArg());
kernel1.GlobalSizes = new[] { a.Length };
multiply_by(a, 2);

// Test kernel call with standard delegate
using var kernel2 = device.CreateKernel("multiply_by", a.AsCLArg(), c.AsCLArg());
kernel2.GlobalSizes = new[] { a.Length };
kernel2.Call(a, 2);

// Check the result
Debug.Assert(a.Zip(b).All(item => item.First == item.Second));

// Create a program which fill an OpenCL direct access buffer
device.Context.CreateProgram(new[]
{
    "__kernel void fill(__global int* buffer) {",
    "   buffer[get_global_id(0)] = get_global_id(0);",
    "}"
});

// Create the kernel and a device buffer
using var fillDevice = device.CreateKernel("fill", device.Context.CreateBuffer<int>(a.Length, CLAccess.ReadOnly).AsCLArg());
fillDevice.GlobalSizes = new[] { a.Length };
// Fill the buffer
fillDevice.Invoke();

// Check the result mapping the buffer for reading
using (var map = fillDevice.MapRead(fillDevice.Arg0)) {
    Debug.Assert(map[a.Length - 1] == a.Length - 1);
}

// Create the kernel and a host buffer
var hostBuf = new int[a.Length];
using var fillHost = device.CreateKernel("fill", device.Context.CreateBuffer<int>(hostBuf, CLAccess.ReadOnly).AsCLArg());
fillHost.GlobalSizes = new[] { a.Length };
// Fill the buffer
fillHost.Invoke();

// Check the result mapping the buffer for reading
using (var map = fillHost.MapRead(fillHost.Arg0)) {
    Debug.Assert(map[a.Length - 1] == a.Length - 1);
}


// Delegate for kernel function
delegate void KernelFunction(int[] values, int mul);
