@echo off
set cwd=%cd%
call build_native.bat
if %ERRORLEVEL% neq 0 goto :error
cd %cwd%
call build_managed.bat
if %ERRORLEVEL% neq 0 goto :error
cd %cwd%
exit /b 0
goto :end
:error
echo "error"
cd %cwd%
exit /b 1
:end

