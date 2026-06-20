@echo off
REM Double-click (or run) this to launch the simulator. Reads config.local.json + bundled data,
REM pulls live results on startup, then shows the menu. No setup required.
dotnet run --project "%~dp0src\WorldCup.Cli" -c Release
pause
