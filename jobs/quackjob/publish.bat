@echo off
setlocal enabledelayedexpansion

REM Usage: publish.bat [Configuration]
if "%1"=="" (
  set CONFIG=Release
) else (
  set CONFIG=%1
)

set PROJ=QuackView.QuackJob.csproj
set OUTDIR=.\publish

echo Publishing win-x64...
set RID=win-x64
set TARGETDIR=%OUTDIR%\%RID%
if not exist "%TARGETDIR%" mkdir "%TARGETDIR%"
dotnet publish "%PROJ%" -c %CONFIG% -r %RID% /p:PublishSingleFile=true /p:SelfContained=true /p:PublishTrimmed=false -o "%TARGETDIR%"

echo Publishing linux-x64...
set RID=linux-x64
set TARGETDIR=%OUTDIR%\%RID%
if not exist "%TARGETDIR%" mkdir "%TARGETDIR%"
dotnet publish "%PROJ%" -c %CONFIG% -r %RID% /p:PublishSingleFile=true /p:SelfContained=true /p:PublishTrimmed=false -o "%TARGETDIR%"

echo Done.
endlocal
pause