using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace GenericTests
{
    /// <summary>
    /// Kernel tests
    /// </summary>
    public class Kernel : BaseEnvironment
    {
        #region AllDevices
        /// <summary>
        /// Devices enumerator
        /// </summary>
        internal class AllDevices : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var devices = CL.Platforms
                    .SelectMany(p => p.Devices
                    .Where(d => d.DeviceType == CLDeviceType.GPU || d.DeviceType == CLDeviceType.CPU));
                foreach (var device in devices)
                    yield return new object[] { device.DeviceType.ToString(), device.Platform.Name, device.Platform.Vendor };
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        #endregion
        #region Fields
        /// <summary>
        /// The program
        /// </summary>
        private static readonly string[] clCode = new[]
        {
            "__kernel void main0(__global int* arg0) {",
            "    arg0[get_global_id(0)] = arg0[get_global_id(0)] + " + valueAdd.ToString() + ";",
            "}",
            "__kernel void main1(",
            "    const int valueAdd,",
            "    __global int* arg1) {",
            "    arg1[get_global_id(0)] = arg1[get_global_id(0)] + valueAdd;",
            "}",
            "__kernel void main2(",
            "    const int valueAdd,",
            "    __global int* arg1,",
            "    __global int* arg2) {",
            "    arg2[get_global_id(0)] = arg1[get_global_id(0)] + valueAdd;",
            "}",
            "__kernel void main3(",
            "    const int valueAdd,",
            "    __global int* arg1,",
            "    __global int* arg2,",
            "    __global int* arg3) {",
            "    arg3[get_global_id(0)] =" +
            "       arg1[get_global_id(0)] +" +
            "       arg2[get_global_id(0)] +" +
            "       valueAdd;",
            "}",
            "__kernel void main4(",
            "    const int valueAdd,",
            "    __global int* arg1,",
            "    __global int* arg2,",
            "    __global int* arg3,",
            "    __global int* arg4) {",
            "    arg4[get_global_id(0)] =" +
            "       arg1[get_global_id(0)] +" +
            "       arg2[get_global_id(0)] +" +
            "       arg3[get_global_id(0)] +" +
            "       valueAdd;",
            "}",
            "__kernel void main5(",
            "    const int valueAdd,",
            "    __global int* arg1,",
            "    __global int* arg2,",
            "    __global int* arg3,",
            "    __global int* arg4,",
            "    __global int* arg5) {",
            "    arg5[get_global_id(0)] =" +
            "       arg1[get_global_id(0)] +" +
            "       arg2[get_global_id(0)] +" +
            "       arg3[get_global_id(0)] +" +
            "       arg4[get_global_id(0)] +" +
            "       valueAdd;",
            "}",
            "__kernel void main6(",
            "    const int valueAdd,",
            "    __global int* arg1,",
            "    __global int* arg2,",
            "    __global int* arg3,",
            "    __global int* arg4,",
            "    __global int* arg5,",
            "    __global int* arg6) {",
            "    arg6[get_global_id(0)] =" +
            "       arg1[get_global_id(0)] +" +
            "       arg2[get_global_id(0)] +" +
            "       arg3[get_global_id(0)] +" +
            "       arg4[get_global_id(0)] +" +
            "       arg5[get_global_id(0)] +" +
            "       valueAdd;",
            "}",
            "__kernel void main7(",
            "    const int valueAdd,",
            "    __global int* arg1,",
            "    __global int* arg2,",
            "    __global int* arg3,",
            "    __global int* arg4,",
            "    __global int* arg5,",
            "    __global int* arg6,",
            "    __global int* arg7) {",
            "    arg7[get_global_id(0)] =" +
            "       arg1[get_global_id(0)] +" +
            "       arg2[get_global_id(0)] +" +
            "       arg3[get_global_id(0)] +" +
            "       arg4[get_global_id(0)] +" +
            "       arg5[get_global_id(0)] +" +
            "       arg6[get_global_id(0)] +" +
            "       valueAdd;",
            "}",
            "__kernel void main8(",
            "    const int valueAdd,",
            "    __global int* arg1,",
            "    __global int* arg2,",
            "    __global int* arg3,",
            "    __global int* arg4,",
            "    __global int* arg5,",
            "    __global int* arg6,",
            "    __global int* arg7,",
            "    __global int* arg8) {",
            "    arg8[get_global_id(0)] =" +
            "       arg1[get_global_id(0)] +" +
            "       arg2[get_global_id(0)] +" +
            "       arg3[get_global_id(0)] +" +
            "       arg4[get_global_id(0)] +" +
            "       arg5[get_global_id(0)] +" +
            "       arg6[get_global_id(0)] +" +
            "       arg7[get_global_id(0)] +" +
            "       valueAdd;",
            "}",
            "__kernel void main9(",
            "    const int valueAdd,",
            "    __global int* arg1,",
            "    __global int* arg2,",
            "    __global int* arg3,",
            "    __global int* arg4,",
            "    __global int* arg5,",
            "    __global int* arg6,",
            "    __global int* arg7,",
            "    __global int* arg8,",
            "    __global int* arg9) {",
            "    arg9[get_global_id(0)] =" +
            "       arg1[get_global_id(0)] +" +
            "       arg2[get_global_id(0)] +" +
            "       arg3[get_global_id(0)] +" +
            "       arg4[get_global_id(0)] +" +
            "       arg5[get_global_id(0)] +" +
            "       arg6[get_global_id(0)] +" +
            "       arg7[get_global_id(0)] +" +
            "       arg8[get_global_id(0)] +" +
            "       valueAdd;",
            "}",
        };
        /// <summary>
        /// Array size
        /// </summary>
        private const int size = 512;
        /// <summary>
        /// Value to add
        /// </summary>
        private const int valueAdd = 2;
        #endregion
        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="output">Messages output interface</param>
        public Kernel(ITestOutputHelper output = null!) :
           base(output)
        {
        }
        /// <summary>
        /// Kernel invocation tests on all the available devices
        /// </summary>
        [Theory, ClassData(typeof(AllDevices)), Trait("Category", "Kernel")]
        [SuppressMessage("Style", "IDE0063", Justification = "To dispose each kernel after use, without waiting the end of the test")]
        public void Invoke(string deviceType, string platform, string vendor)
        {
            // Find the testing device
            var device =
                CL.Platforms
                .Where(p => p.Name == platform && p.Vendor == vendor)
                .First()
                .Devices
                .Where(d => d.DeviceType.ToString() == deviceType)
                .First();
            // Describe the device
            WriteLine($"Testing device {device.DeviceType} on platform {device.Platform.Name}");
            WriteLine("Platform info:");
            WriteLine($"Vendor:         {device.Platform.Vendor}");
            WriteLine($"Version:        {device.Platform.Version}");
            WriteLine($"Profile:        {device.Platform.Profile}");
            WriteLine($"IcdSuffixKhr:   {device.Platform.IcdSuffixKhr}");
            WriteLine($"Extensions:");
            foreach (var ext in device.Platform.Extensions)
                WriteLine($"- {ext}");
            // Create the OpenCL context
            using var context = device.CreateContext();
            // Create the OpenCL program
            context.CreateProgram(clCode);
            // Argument types
            var rd = CL.Array<int>(CLAccess.ReadOnly);
            var wr = CL.Array<int>(CLAccess.WriteOnly);
            var clInt = CL.Var<int>();
            // Create the first argument
            var arg0 = Enumerable.Range(0, size).Select(i => i).ToArray();
            // Simulate the result
            var result0 = arg0.Select(value => value + valueAdd).ToArray();
            // Call the kernel and check the result
            using (var main0 = context.DefaultDevice.CreateKernel("main0", arg0.AsCLArg())) {
                main0.GlobalSizes = new[] { size };
                main0.Call(arg0);
                Assert.True(arg0.Zip(result0).All(item => item.First == item.Second));
            }
            // Define arg1 and simulate the result
            var arg1 = Enumerable.Range(0, size).Select(i => i).ToArray();
            var result1 = arg1.Select(value => value + valueAdd).ToArray();
            // Call the kernel and check the result
            using (var main1 = context.DefaultDevice.CreateKernel("main1", valueAdd.AsCLArg(), arg1.AsCLArg())) {
                main1.GlobalSizes = new[] { size };
                main1.Call(valueAdd, arg1);
                Assert.True(arg1.Zip(result1).All(item => item.First == item.Second));
            }
            // Define arg2 and simulate the result
            var arg2 = new int[size];
            var result2 =
                arg1.Zip(arg2)
                .Select(value => value.First + value.Second + valueAdd)
                .ToArray();
            // Call the kernel and check the result
            using (var main2 = context.DefaultDevice.CreateKernel("main2", clInt, wr, rd)) {
                main2.GlobalSizes = new[] { size };
                main2.Call(valueAdd, arg1, arg2);
                Assert.True(arg2.Zip(result2).All(item => item.First == item.Second));
            }
            // Define arg3 and simulate the result
            var arg3 = new int[size];
            var result3 =
                arg1.Zip(arg2)
                .Select(value => value.First + value.Second)
                .Zip(arg3)
                .Select(value => value.First + value.Second + valueAdd)
                .ToArray();
            // Call the kernel and check the result
            using (var main3 = context.DefaultDevice.CreateKernel("main3", clInt, wr, wr, rd)) {
                main3.GlobalSizes = new[] { size };
                main3.Call(valueAdd, arg1, arg2, arg3);
                Assert.True(arg3.Zip(result3).All(item => item.First == item.Second));
            }
            // Define arg4 and simulate the result
            var arg4 = new int[size];
            var result4 =
                arg1.Zip(arg2)
                .Select(value => value.First + value.Second)
                .Zip(arg3)
                .Select(value => value.First + value.Second)
                .Zip(arg4)
                .Select(value => value.First + value.Second + valueAdd)
                .ToArray();
            // Call the kernel and check the result
            using (var main4 = context.DefaultDevice.CreateKernel("main4", clInt, wr, wr, wr, rd)) {
                main4.GlobalSizes = new[] { size };
                main4.Call(valueAdd, arg1, arg2, arg3, arg4);
                Assert.True(arg4.Zip(result4).All(item => item.First == item.Second));
            }
            // Define arg5 and simulate the result
            var arg5 = new int[size];
            var result5 =
                arg1.Zip(arg2)
                .Select(value => value.First + value.Second)
                .Zip(arg3)
                .Select(value => value.First + value.Second)
                .Zip(arg4)
                .Select(value => value.First + value.Second)
                .Zip(arg5)
                .Select(value => value.First + value.Second + valueAdd)
                .ToArray();
            // Call the kernel and check the result
            using (var main5 = context.DefaultDevice.CreateKernel("main5", clInt, wr, wr, wr, wr, rd)) {
                main5.GlobalSizes = new[] { size };
                main5.Call(valueAdd, arg1, arg2, arg3, arg4, arg5);
                Assert.True(arg5.Zip(result5).All(item => item.First == item.Second));
            }
            // Define arg6 and simulate the result
            var arg6 = new int[size];
            var result6 =
                arg1.Zip(arg2)
                .Select(value => value.First + value.Second)
                .Zip(arg3)
                .Select(value => value.First + value.Second)
                .Zip(arg4)
                .Select(value => value.First + value.Second)
                .Zip(arg5)
                .Select(value => value.First + value.Second)
                .Zip(arg6)
                .Select(value => value.First + value.Second + valueAdd)
                .ToArray();
            // Call the kernel and check the result
            using (var main6 = context.DefaultDevice.CreateKernel("main6", clInt, wr, wr, wr, wr, wr, rd)) {
                main6.GlobalSizes = new[] { size };
                main6.Call(valueAdd, arg1, arg2, arg3, arg4, arg5, arg6);
                Assert.True(arg6.Zip(result6).All(item => item.First == item.Second));
            }
            // Define arg7 and simulate the result
            var arg7 = new int[size];
            var result7 =
                arg1.Zip(arg2)
                .Select(value => value.First + value.Second)
                .Zip(arg3)
                .Select(value => value.First + value.Second)
                .Zip(arg4)
                .Select(value => value.First + value.Second)
                .Zip(arg5)
                .Select(value => value.First + value.Second)
                .Zip(arg6)
                .Select(value => value.First + value.Second)
                .Zip(arg7)
                .Select(value => value.First + value.Second + valueAdd)
                .ToArray();
            // Call the kernel and check the result
            using (var main7 = context.DefaultDevice.CreateKernel("main7", clInt, wr, wr, wr, wr, wr, wr, rd)) {
                main7.GlobalSizes = new[] { size };
                main7.Call(valueAdd, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                Assert.True(arg7.Zip(result7).All(item => item.First == item.Second));
            }
            // Define arg8 and simulate the result
            var arg8 = new int[size];
            var result8 =
                arg1.Zip(arg2)
                .Select(value => value.First + value.Second)
                .Zip(arg3)
                .Select(value => value.First + value.Second)
                .Zip(arg4)
                .Select(value => value.First + value.Second)
                .Zip(arg5)
                .Select(value => value.First + value.Second)
                .Zip(arg6)
                .Select(value => value.First + value.Second)
                .Zip(arg7)
                .Select(value => value.First + value.Second)
                .Zip(arg8)
                .Select(value => value.First + value.Second + valueAdd)
                .ToArray();
            // Call the kernel and check the result
            using (var main8 = context.DefaultDevice.CreateKernel("main8", clInt, wr, wr, wr, wr, wr, wr, wr, rd)) {
                main8.GlobalSizes = new[] { size };
                main8.Call(valueAdd, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                Assert.True(arg8.Zip(result8).All(item => item.First == item.Second));
            }
            // Define arg9 and simulate the result
            var arg9 = new int[size];
            var result9 =
                arg1.Zip(arg2)
                .Select(value => value.First + value.Second)
                .Zip(arg3)
                .Select(value => value.First + value.Second)
                .Zip(arg4)
                .Select(value => value.First + value.Second)
                .Zip(arg5)
                .Select(value => value.First + value.Second)
                .Zip(arg6)
                .Select(value => value.First + value.Second)
                .Zip(arg7)
                .Select(value => value.First + value.Second)
                .Zip(arg8)
                .Select(value => value.First + value.Second)
                .Zip(arg9)
                .Select(value => value.First + value.Second + valueAdd)
                .ToArray();
            // Call the kernel and check the result
            using (var main9 = context.DefaultDevice.CreateKernel("main9", clInt, wr, wr, wr, wr, wr, wr, wr, wr, rd)) {
                main9.GlobalSizes = new[] { size };
                main9.Call(valueAdd, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                Assert.True(arg9.Zip(result9).All(item => item.First == item.Second));
            }
        }
        #endregion
    }
}