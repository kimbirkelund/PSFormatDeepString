dotnet tool install `
    --tool-path . `
    nbgv; 
checkLastExitCode;

.\nbgv cloud; 
checkLastExitCode;

dotnet restore src; 
checkLastExitCode;

dotnet build src `
    --no-restore `
    --configuration Release; 
checkLastExitCode;
