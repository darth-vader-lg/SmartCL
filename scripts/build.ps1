try {
    # Go to root directory
    if (-not($PSScriptRoot) -and $psISE) { $scriptRoot = Split-Path $psISE.CurrentFile.FullPath } else { $scriptRoot = $PSScriptRoot }
    cd $PSScriptRoot\..

    # Clean packages
    Get-ChildItem * -Include *.nupkg -Recurse | Remove-Item

    # Build the solution
    dotnet clean -c Release -v n
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
    dotnet build -c Release -v n
    exit $LASTEXITCODE
}
catch {
    Write-Host $_.ScriptStackTrace
    exit 1
}
