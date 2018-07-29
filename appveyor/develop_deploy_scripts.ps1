[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;


if ([string]::IsNullOrWhiteSpace($global:VersionInfo.PrereleaseVersion))
{
    Write-Host "Branch will only deploy when version is a prerelease version.";
    return;
}


Write-Host "### Publish module"
Publish-Module `
    -NuGetApiKey $env:PowerShellGalleryApiKey `
    -Path $env:APPVEYOR_PROJECT_NAME `
    -Repository PSGallery `
    -Verbose;
Write-Host "### /Publish module"

