[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;


Write-Host "Create tag"
.\nbgv tag;
checkLastExitCode;
Write-Host "/Create tag"


Write-Host "Publish module"
Publish-Module `
    -NuGetApiKey $env:PowerShellGalleryApiKey `
    -Path $env:APPVEYOR_PROJECT_NAME `
    -Repository PSGallery `
    -Verbose;
Write-Host "/Publish module"


Write-Host "Push tags"
git push origin --tags;
checkLastExitCode;
Write-Host "/Push tags"
