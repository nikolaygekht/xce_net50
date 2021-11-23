@echo off
if not exist runtimes mkdir runtimes
if not exist runtimes\win-x86 mkdir runtimes\win-x86
if not exist runtimes\win-x86\native mkdir runtimes\win-x86\native
copy ..\..\native\build32\release\colorertake5.dll runtimes\win-x86\native
if not exist runtimes\win-x64 mkdir runtimes\win-x64
if not exist runtimes\win-x64\native mkdir runtimes\win-x64\native
copy ..\..\native\build64\release\colorertake5.dll runtimes\win-x64\native
if not exist runtimes\linux-x64 mkdir runtimes\linux-x64
if not exist runtimes\linux-x64\native mkdir runtimes\linux-x64\native
copy ..\..\native\linux64wsl\colorertake5.so runtimes\linux-x64\native
