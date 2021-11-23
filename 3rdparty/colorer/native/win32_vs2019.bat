if not exist build32 md build32
cd build32
cmake -G "Visual Studio 16" -A Win32 -DCMAKE_BUILD_TYPE=Release ..
msbuild colorertake5.sln /p:Configuration=Release /p:Platform=Win32