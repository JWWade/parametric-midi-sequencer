@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%.." >nul

echo Building ParametricMidiSequencer.sln...
dotnet build ".\ParametricMidiSequencer.sln"
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
	echo Build failed. UI will not launch.
	popd >nul
	exit /b %EXIT_CODE%
)

echo Launching Parametric MIDI Sequencer UI...
dotnet run --project ".\ui\ParametricMidiSequencer.UI.csproj"
set "EXIT_CODE=%ERRORLEVEL%"

popd >nul
exit /b %EXIT_CODE%
