[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;

.\nbgv tag; 
checkLastExitCode;

$name = $env:APPVEYOR_PROJECT_NAME;

Remove-Item $name -Recurse -Force -ErrorAction SilentlyContinue;
New-Item -ItemType Directory $name;

Copy-Item .\src\PSFormatDeepString\bin\Release\netstandard2.0\* $name;

$prereleaseVersion = .\nbgv get-version -f json | `
    ConvertFrom-Json | `
    Select-Object -ExpandProperty PrereleaseVersion;

$simpleVersion = .\nbgv get-version -f json | 
    ConvertFrom-Json | 
    Select-Object -ExpandProperty CloudBuildAllVars | 
    Select-Object -ExpandProperty NBGV_SimpleVersion;

New-ModuleManifest `
    -Path (Join-Path $name "$name.psd1") `
    -Guid "$env:ModuleGuid" `
    -Author "$env:APPVEYOR_ACCOUNT_NAME" `
    -Copyright "(c) 2018 $env:APPVEYOR_ACCOUNT_NAME. All rights reserved." `
    -RootModule "$name.dll" `
    -ModuleVersion $simpleVersion `
    -Description (Get-Content -Raw .\README.md) `
    -CmdletsToExport "*" `
    -LicenseUri "https://github.com/$($env:APPVEYOR_REPO_NAME)/blob/master/LICENSE" `
    -PrivateData @{ Prerelease = $prereleaseVersion };

Publish-Module `
    -NuGetApiKey $env:PowerShellGalleryApiKey `
    -Path (Resolve-Path $name) `
    -Repository PSGallery `
    -Verbose;

git push --tags; 
checkLastExitCode;