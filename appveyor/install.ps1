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

Set-PSRepository -Name PSGallery -InstallationPolicy Trusted;
Update-Module PowerShellGet -RequiredVersion 1.6 -Confirm:$false -Force;

dotnet tool install `
    --tool-path . `
    nbgv;
checkLastExitCode;

.\nbgv cloud;
checkLastExitCode;
