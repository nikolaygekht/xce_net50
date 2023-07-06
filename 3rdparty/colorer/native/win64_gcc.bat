if not exist build64gcc md build64gcc
cd build64gcc
SET CC=gcc.exe
SET CXX=g++.exe
cmake -G "MinGW Makefiles" -DCMAKE_BUILD_TYPE=Release ..
mingw32-make.exe