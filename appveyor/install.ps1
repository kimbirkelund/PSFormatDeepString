function checkLastExitCode 
{
    if ($LASTEXITCODE -ne 0)
    {
        Write-Error "Error in last command. Exit code was $LASTEXITCODE, which is -ne 0." 
    } 
}

dotnet tool install `
    --tool-path . `
    nbgv; 
checkLastExitCode;

.\nbgv cloud; 
checkLastExitCode;
