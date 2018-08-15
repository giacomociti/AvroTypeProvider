### Initialize the repository with a suitable _.gitignore_ file:

- mkdir AvroTypeProvider
- cd AvroTypeProvider
- git init
- curl -o .gitignore https://www.gitignore.io/api/fsharp,linux,windows,macos,vim,emacs,visualstudio,visualstudiocode

### Add _Paket_ 

- mkdir .paket
- cd .paket
- curl -L -o paket.exe https://github.com/fsprojects/Paket/releases/download/5.176.4/paket.bootstrapper.exe
- chmod +x paket.exe
- cd ..
- .paket/paket.exe init

### Add main project

mkdir src
cd src
mkdir AvroTypeProvider
cd AvroTypeProvider
dotnet new sln
dotnet new classlib -lang F#
dotnet sln add AvroTypeProvider.fsproj
dotnet build