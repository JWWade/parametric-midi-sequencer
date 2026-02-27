@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%.." >nul

echo Building ParametricMidiSequencer.sln...
dotnet build ".\ParametricMidiSequencer.sln"
set "EXIT_CODE=%ERRORLEVEL%"

popd >nul
exit /b %EXIT_CODE%
