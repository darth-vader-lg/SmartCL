using System.Runtime.InteropServices;
using static CLKernels;

Console.WriteLine("Hello, World!");
unsafe {
    var data = stackalloc int[] { 123 };
    main(data); 
}

static class CLKernels
{
    [DllImport("CLKernels", EntryPoint = "_main")]
    public extern static unsafe void main(int* data);
}
