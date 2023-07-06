# Building Colorer Take 5 Native

The build targets two platforms:
* x64 Windows
* x64 Linux

For x64 Linux default gcc compiler is used

For x64 Windows three options can be used:

* Microsoft Visual Studio 2022
* Microsoft Build Tools (https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022)
* GCC x64 Compiler for Windows (https://www.codeblocks.org/downloads/binaries/)

The build also requires cmake version 3.26 or newer installed (https://cmake.org/).

For windows tools must be properly configured (e.g. INCLUDE, LIB variables for Visual Studio)
and must accessible via `PATH` variable.
