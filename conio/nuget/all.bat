cd ..
msbuild Gehtsoft.Xce.Conio.sln /p:Configuration=Release
cd nuget
msbuild nuget.proj -t:Prepare
msbuild nuget.proj -t:NuSpec,NuPack,NuCopy
