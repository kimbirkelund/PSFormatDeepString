[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;


Write-Host "### Publish module"
Publish-Module `
    -NuGetApiKey $env:PowerShellGalleryApiKey `
    -Path $env:APPVEYOR_PROJECT_NAME `
    -Repository PSGallery `
    -Verbose;
Write-Host "### /Publish module"

