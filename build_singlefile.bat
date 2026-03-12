@echo off
setlocal enabledelayedexpansion

REM Build single-file EXE (self-contained) for Windows x64
set CONFIG=Release
set RUNTIME=win-x64
set OUTDIR=%~dp0artifacts\publish

if not exist "%OUTDIR%" mkdir "%OUTDIR%"

pushd "%~dp0"

dotnet publish .\McTestPanel\McTestPanel.csproj -c %CONFIG% -r %RUNTIME% ^
  -p:PublishSingleFile=true ^
  -p:SelfContained=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:PublishTrimmed=false ^
  -o "%OUTDIR%"

popd

if %errorlevel% neq 0 (
  echo Publish failed.
  pause
  exit /b %errorlevel%
)

echo Publish succeeded.
for %%F in ("%OUTDIR%\*.exe") do (
  echo Output: %%~fF
)

endlocal
