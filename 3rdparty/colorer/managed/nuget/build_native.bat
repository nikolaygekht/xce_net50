echo off
set cwd=%cd%
cd ..\..\native
call win64_vs2022.bat
cd %cwd%
cd ..\..\native
wsl ./linux_wsl.sh
cd %cwd%
cd ..\CailLomecb.ColorerTake5\
del .\runtimes\linux-x64\native\colorertake5.so
del .\runtimes\win-x64\native\colorertake5.dll
call copy-runtimes.bat
if not exist .\runtimes\win-x64\native\colorertake5.dll goto :error
if not exist .\runtimes\linux-x64\native\colorertake5.so goto :error
cd %cwd%
echo "done"
exit /b 0
goto :end
:error
cd %cwd%
echo "native build failed"
exit /b 1
:end
echo "build complete"
