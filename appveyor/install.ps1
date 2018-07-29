[CmdletBinding()]
PARAM()

$ErrorActionPreference = "Stop";
Set-StrictMode -Version Latest;


function checkLastExitCode
{
    if ($LASTEXITCODE -ne 0)
    {
        Write-Error "Error in last command. Exit code was $LASTEXITCODE, which is -ne 0."
    }
}


Write-Host "### Install latest PowerShellGet"
Set-PSRepository -Name PSGallery -InstallationPolicy Trusted;

Install-Module PowerShellGet -Confirm:$false -Force -AllowClobber -Verbose -Scope CurrentUser -MinimumVersion 1.6.6;
Import-Module PowerShellGet -MinimumVersion 1.6.6 -Force;
Import-PackageProvider -Name PowerShellGet -Force -MinimumVersion 1.6.6;
Get-Module PowerShellGet;
Write-Host "### /Install latest PowerShellGet"


Write-Host "### Install NerdBank.GitVersioning CLI and set env vars"
dotnet tool install `
    --tool-path . `
    nbgv;
checkLastExitCode;
Write-Host "### /Install NerdBank.GitVersioning CLI and set env vars"


Write-Host "### Call nbgv cloud"
.\nbgv cloud;
checkLastExitCode;
Write-Host "### /Call nbgv cloud"
