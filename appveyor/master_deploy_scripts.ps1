[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;


# Create tag
.\nbgv tag;
checkLastExitCode;


# Publish module
Publish-Module `
    -NuGetApiKey $env:PowerShellGalleryApiKey `
    -Path $env:APPVEYOR_PROJECT_NAME `
    -Repository PSGallery `
    -Verbose;


git push --tags;
checkLastExitCode;
