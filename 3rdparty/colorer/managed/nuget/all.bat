@echo off
set cwd=%cd%
call build_native.bat
if %ERRORLEVEL% neq 0 goto :error
call build_managed.bat
if %ERRORLEVEL% neq 0 goto :error
cd %cwd%
dotnet restore nuget.proj
msbuild nuget.proj -t:Prepare
msbuild nuget.proj -t:NuSpec,NuPack,NuCopy
exit /B 0
goto :end
:error
echo "build failed"
cd %cwd%
exit /B 1
:end
