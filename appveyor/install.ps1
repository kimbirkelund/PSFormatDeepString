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


# Install latest PowerShellGet
Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -Verbose;
Install-Module PowerShellGet -Confirm:$false -Force -AllowClobber -Verbose -Scope CurrentUser -MinimumVersion 1.6;
Import-Module PowerShellGet -MinimumVersion 1.6 -Force;

Get-Module PowerShellGet;
Get-Command Update-ModuleManifest;


# Install NerdBank.GitVersioning CLI and set env vars
dotnet tool install `
    --tool-path . `
    nbgv;
checkLastExitCode;

.\nbgv cloud;
checkLastExitCode;
