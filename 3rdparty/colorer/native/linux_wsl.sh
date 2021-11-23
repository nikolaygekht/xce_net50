#!/bin/bash

echo Build for linux
mkdir -p linux64wsl
cd linux64wsl
cmake -G "Unix Makefiles" -DCMAKE_BUILD_TYPE=Release ..
make

