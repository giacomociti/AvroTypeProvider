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

- mkdir src
- cd src
- mkdir AvroTypeProvider
- cd AvroTypeProvider
- dotnet new sln
- dotnet new classlib -lang F#
- dotnet sln add AvroTypeProvider.fsproj
- dotnet build

### Reference Type Provider SDK files

- .paket/paket.exe install
- [edit AvroTypeProvider.fsproj]

### Reference Avro library

- dotnet add package Confluent.Apache.Avro --version 1.7.7.5

### Reference libraries in test

- Hack: Referenced downloaded nuget packages in smoke test

### Add test solution

- cd tests
- mkdir AvroTypeProvider.Tests
- cd AvroTypeProvider.Tests
- dotnet new sln
- dotnet new xunit -lang F#
- dotnet sln add AvroTypeProvider.Tests.fsproj
- dotnet build
- dotnet test