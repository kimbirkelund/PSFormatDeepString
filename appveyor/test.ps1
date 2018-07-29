[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;


dotnet test src `
    --no-restore `
    --no-build `
    --configuration Release;
checkLastExitCode
