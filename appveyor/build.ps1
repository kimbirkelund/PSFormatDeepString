# Restore pacakges
dotnet restore src;
checkLastExitCode;


#Build code
dotnet build src `
    --no-restore `
    --configuration Release;
checkLastExitCode;


# Create module folder
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

$moduleManifest = @"
@{
    RootModule        = "$($env:APPVEYOR_PROJECT_NAME).dll"

    ModuleVersion     = "$simpleVersion"
    GUID              = "$($env:ModuleGuid)"
    Description       = "$(Get-Content -Raw .\README.md)"

    Author            = "Kim Birkelund"
    Copyright         = "(c) 2018 Kim Birkelund. All rights reserved."

    FunctionsToExport = "*"
    CmdletsToExport   = "*"
    VariablesToExport = "*"
    AliasesToExport   = "*"

    PrivateData       = @{
        PSData = @{
            LicenseUri = "https://github.com/$($env:APPVEYOR_REPO_NAME)/blob/master/LICENSE"
            Prerelease = "$prereleaseVersion"
        }
    }
}
"@

$moduleManifest |
    Set-Content (Join-Path $name "$name.psd1");

Write-Host "### Dist folder ###"
Get-ChildItem $name
Write-Host "### /Dist folder ###"
