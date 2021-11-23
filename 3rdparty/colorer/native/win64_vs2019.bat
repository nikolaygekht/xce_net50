if not exist build64 md build64
cd build64
cmake -G "Visual Studio 16" -A x64 -DCMAKE_BUILD_TYPE=Release ..
msbuild colorertake5.sln /p:Configuration=Release /p:Platform=x64