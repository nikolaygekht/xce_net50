@echo off
set cwd=%cd%
cd ..
dotnet build CailLomecb.ColorerTake5.sln /p:Configuration=Release
if %ERRORLEVEL% neq 0 goto :error
dotnet test CailLomecb.ColorerTake5.sln /p:Configuration=Release
if %ERRORLEVEL% neq 0 goto :error
cd %cwd%
exit /b 0
goto :end
:error
cd %cwd%
echo "build and test failed..."
exit /b 1
:end