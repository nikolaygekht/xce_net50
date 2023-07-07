if not exist build64 md build64
cd build64
SET CC=cl.exe
SET CXX=cl.exe
cmake -G "Visual Studio 17" -A x64 -DCMAKE_BUILD_TYPE=Release ..
msbuild colorertake5.sln /p:Configuration=Release /p:Platform=x64