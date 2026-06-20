#!/usr/bin/env pwsh
# Launch the simulator (Release). Reads config.local.json + bundled data, pulls live results on
# startup, then shows the menu. No setup required.
dotnet run --project "$PSScriptRoot/src/WorldCup.Cli" -c Release
