if not exist build32 md build32
cd build32
SET CC=cl.exe
SET CXX=cl.exe
cmake -G "Visual Studio 17" -A win32 -DCMAKE_BUILD_TYPE=Release ..
msbuild colorertake5.sln /p:Configuration=Release
