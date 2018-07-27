dotnet restore src; 
checkLastExitCode;

dotnet build src `
    --no-restore `
    --configuration Release; 
checkLastExitCode;
