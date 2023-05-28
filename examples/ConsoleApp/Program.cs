using System.Diagnostics;
using SmartCL;

var a = new int[1024];
var b = new int[a.Length];
var c = 2;

for (var i = 0; i < a.Length; i++) {
    a[i] = i;
    b[i] = i * c * 2;
}

var device = CL.GetFirstGpuOrDefault();

using var program = device.CreateProgram(new[]
{
    "__kernel void multiply_by(__global int* A, const int c) {",
    "   A[get_global_id(0)] = c * A[get_global_id(0)];",
    "}"
});

using var kernel1 = program.CreateKernel("multiply_by", out KernelFunction multiply_by, CLArg.RW(a!), CLArg.W(c));
kernel1.Dims = new(a.Length);
multiply_by(a, 2);

using var kernel2 = program.CreateKernel("multiply_by", CLArg.RW(a!), CLArg.W(c));
kernel2.Dims = new(a.Length);
kernel2.Call(a, 2);

Debug.Assert(a.Zip(b).All(item => item.First == item.Second));

delegate void KernelFunction(int[] values, int mul);
