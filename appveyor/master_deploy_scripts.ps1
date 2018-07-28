[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;

.\nbgv tag;
checkLastExitCode;

$name = $env:APPVEYOR_PROJECT_NAME;

Publish-Module `
    -NuGetApiKey $env:PowerShellGalleryApiKey `
    -Path (Resolve-Path $name) `
    -Repository PSGallery `
    -Verbose;

git push --tags;
checkLastExitCode;
