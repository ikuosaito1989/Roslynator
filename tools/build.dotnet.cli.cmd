@echo off

dotnet restore --force "..\src\CommandLine.sln"

rd /S /Q "..\src\CommandLine\bin\Release\publish"

"C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild" "..\src\CommandLine.sln" ^
 /t:Clean,Publish ^
 /p:Configuration=Release,RoslynatorDotNetCli=true,Deterministic=true,TreatWarningsAsErrors=true,WarningsNotAsErrors="1591" ^
 /v:normal ^
 /m

if errorlevel 1 (
 pause
 exit
)

del /Q "..\src\CommandLine\bin\Release\Roslynator.DotNet.Cli.*.nupkg"

dotnet pack "..\src\CommandLine\CommandLine.csproj" -c Release -v normal ^
 /p:RoslynatorDotNetCli=true,Deterministic=true,TreatWarningsAsErrors=true,WarningsNotAsErrors="1591"

echo OK
pause
