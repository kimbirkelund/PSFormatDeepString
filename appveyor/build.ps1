[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;


Write-Host "Restore pacakges"
dotnet restore src;
checkLastExitCode;
Write-Host "/Restore pacakges"


Write-Host "Build code"
dotnet build src `
    --no-restore `
    --configuration Release;
checkLastExitCode;
Write-Host "/Build code"


Write-Host "Create and populate module folder"
$name = $env:APPVEYOR_PROJECT_NAME;

Remove-Item $name -Recurse -Force -ErrorAction SilentlyContinue;
New-Item -ItemType Directory $name;

Copy-Item .\src\PSFormatDeepString\bin\Release\netstandard2.0\* $name;
Write-Host "/Create and populate module folder"


Write-Host "Create module manifest"
$prereleaseVersion = .\nbgv get-version -f json | `
    ConvertFrom-Json | `
    Select-Object -ExpandProperty PrereleaseVersion;

$simpleVersion = .\nbgv get-version -f json |
    ConvertFrom-Json |
    Select-Object -ExpandProperty CloudBuildAllVars |
    Select-Object -ExpandProperty NBGV_SimpleVersion;

$moduleManifestPath = Join-Path $name "$name.psd1";
New-ModuleManifest $moduleManifestPath;

Update-ModuleManifest `
    -Path $moduleManifestPath `
    -Description (Get-Content -Raw .\README.md) `
    -RootModule "$($env:APPVEYOR_PROJECT_NAME).dll" `
    -Guid $env:ModuleGuid `
    -Author "Kim Birkelund" `
    -CompanyName "Kim Birklund" `
    -Copyright "(c) 2018 Kim Birkelund. All rights reserved." `
    -ModuleVersion $simpleVersion `
    -Prerelease $prereleaseVersion `
    -ProjectUri "https://github.com/$($env:APPVEYOR_REPO_NAME)" `
    -LicenseUri "https://github.com/$($env:APPVEYOR_REPO_NAME)/blob/master/LICENSE" `
    -AliasesToExport "*" `
    -CmdletsToExport "*" `
    -FunctionsToExport "*" `
    -VariablesToExport "*" `
    -PassThru;

$manifest = Test-ModuleManifest $moduleManifestPath;

Update-ModuleManifest `
    -Path $moduleManifestPath `
    -CmdletsToExport ($manifest.ExportedCmdlets.Keys);

Test-ModuleManifest $moduleManifestPath;
Write-Host "/Create module manifest"


Write-Host "Dist folder"
Get-ChildItem $name
Write-Host "/Dist folder"
