::@echo off
:: Should be run from the quackjob folder

setlocal enabledelayedexpansion

set DIST_DIR=..\..\dist
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"
set SLN=QuackView.QuackJob.sln

echo Publishing win-x64...

dotnet publish "%SLN%" -c Release -r win-x64 ^
  /p:PublishSingleFile=true /p:SelfContained=true ^
  /p:PublishTrimmed=false /p:UseAppHost=true

if not exist "%DIST_DIR%\quackjob\win-x64\" mkdir "%DIST_DIR%\quackjob\win-x64\"
copy /Y "src\QuackView.QuackJob\bin\Release\net9.0\win-x64\publish\*" "%DIST_DIR%\quackjob\win-x64\"

del /Q "%DIST_DIR%\quackjob\win-x64\*.pdb"

echo Publishing linux-x64...

dotnet publish "%SLN%" -c Release -r linux-x64 ^
  /p:PublishSingleFile=false /p:SelfContained=false ^
  /p:PublishTrimmed=false /p:UseAppHost=false

if not exist "%DIST_DIR%\quackjob\linux-x64\" mkdir "%DIST_DIR%\quackjob\linux-x64\"
copy /Y "src\QuackView.QuackJob\bin\Release\net9.0\linux-x64\publish\*" "%DIST_DIR%\quackjob\linux-x64\"

del /Q "%DIST_DIR%\quackjob\linux-x64\*.pdb"

:: if not exist "%DIST_DIR%\quackjob\examples\" mkdir "%DIST_DIR%\quackjob\examples\"
:: ;copy /Y "src\QuackView.QuackJob\examples\*" ^
:: "%DIST_DIR%\quackjob\examples"

if not exist "..\..\scheduler\dist\quackjob\linux-x64\" mkdir "..\..\scheduler\dist\quackjob\linux-x64\"
copy /Y "%DIST_DIR%\quackjob\linux-x64\*" "..\..\scheduler\dist\bin\"

echo Done.
endlocal
