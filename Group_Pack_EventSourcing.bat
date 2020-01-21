@echo off
setlocal
set PROGRAMS=%ProgramFiles(x86)%
for %%e in (Community Professional Enterprise) do (
    if exist "%PROGRAMS%\Microsoft Visual Studio\2019\%%e\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD=%PROGRAMS%\Microsoft Visual Studio\2019\%%e\MSBuild\Current\Bin\MSBuild.exe"
    )
)
if exist "%MSBUILD%" goto :operations

:nomsbuild
echo Microsoft Build version 15.1 (or later) does not appear to be
echo installed on this machine, which is required to build the solution.
goto end

:operations
setlocal

"%MSBUILD%" "Ark.Tools.EventSourcing\Ark.Tools.EventSourcing.csproj"								/t:Pack /p:Configuration=Release
"%MSBUILD%" "Ark.Tools.EventSourcing.Rebus\Ark.Tools.EventSourcing.Rebus.csproj"					/t:Pack /p:Configuration=Release
"%MSBUILD%" "Ark.Tools.EventSourcing.SimpleInjector\Ark.Tools.EventSourcing.SimpleInjector.csproj"	/t:Pack /p:Configuration=Release
"%MSBUILD%" "Ark.Tools.EventSourcing.RavenDb\Ark.Tools.EventSourcing.RavenDb.csproj"				/t:Pack /p:Configuration=Release

pause

:end