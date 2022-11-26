dotnet build
dotnet coverlet .\Socksy.Core.Test\bin\Debug\net7.0\Socksy.Core.Test.dll --target "dotnet" --targetargs "test .\Socksy.Core.Test --no-build --nologo"
