<!-- Begin exclude from NuGet readme. -->
<h1 align="center">SmartCL</h1>

SmartCL is a wrapper library for simple accelerated computing in NET environment, providing a managed binding to OpenCL!

SmartCL works on any .NET Standard 2.0 / 2.1 compliant platform, including .NET 6.0, Xamarin, .NET Framework 4.6.1+, and .NET Core 2.0+.

<!-- Package description inserted here automatically. -->

<h1 align="center">Features</h1>

### Performance

Several optimizations for fast access to OpenCL native functions.

### Easy to use

Is it possible writing code without knowledge about low-level OpenCL calls.
Invocation of GPU kernel function are done by standard NET delegates.
Managing of kernel parameters sending and retrieving is transparently done by the library. 

<!-- Begin exclude from NuGet readme. -->

<h1 align="center">The team</h1>

We currently have the following maintainers:
- [Luigi Generale](https://github.com/darth-vader-lg)

<h1 align="center">Building from source</h1>

Prerequisites
- **Must**: .NET 6 SDK
- **Could**: Visual Studio 2022 Community version 17.0 or later

Instructions
- Clone the repository (recursively)
- In the script folder there are scripts for build, test and publish the release configuration of the API. Just tun them.

<h1 align="center">Contributing</h1>

SmartCL uses and encourages [Early Pull Requests](https://medium.com/practical-blend/pull-request-first-f6bb667a9b6). Please don't wait until you're done to open a PR!

1. [Fork SmartCL](https://github.com/darth-vader-lg/SmartCL/fork)
2. Add an empty commit to a new branch to start your work off: `git commit --allow-empty -m "start of [thing you're working on]"`
3. Once you've pushed a commit, open a [**draft pull request**](https://github.blog/2019-02-14-introducing-draft-pull-requests/). Do this **before** you actually start working.
4. Make your commits in small, incremental steps with clear descriptions.
5. Tag a maintainer when you're done and ask for a review!

<!-- End exclude from NuGet readme. -->

<h1 align="center">Further resources</h1>

- Several examples can be found in the [examples folder](https://github.com/darth-vader-lg/SmartCL/tree/master/examples).
- [ConsoleApp](https://github.com/darth-vader-lg/SmartCL/tree/master/examples/ConsoleApp): A console application showing how to create a program, kernels and use the buffers.
- [CLKernels](https://github.com/darth-vader-lg/SmartCL/tree/master/examples/CLKernels): A cpp project running both as DLL and as program on th GPU device.
- [CLProgram](https://github.com/darth-vader-lg/SmartCL/tree/master/examples/CLProgram): A console application to call CLKernel library from standard application and to build and call it on the GPU.

<h1 align="center">Licensing and governance</h1>

SmartCL is distributed under the very permissive MIT/X11 license and all dependencies are distributed under MIT-compatible licenses.

