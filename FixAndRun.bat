@echo off
cd /d "%~dp0"
del /q *.sln
dotnet new sln -n MdarSystem
dotnet sln MdarSystem.sln add Mdar.API/Mdar.API.csproj Mdar.Core/Mdar.Core.csproj Mdar.Infrastructure/Mdar.Infrastructure.csproj
start MdarSystem.sln
