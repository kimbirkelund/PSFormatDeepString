[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;


Write-Host "### Run tests"
dotnet test src `
    --no-restore `
    --no-build `
    --configuration Release;
checkLastExitCode
Write-Host "### /Run tests"
