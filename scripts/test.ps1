try {
    # Go to root directory
    if (-not($PSScriptRoot) -and $psISE) { $scriptRoot = Split-Path $psISE.CurrentFile.FullPath } else { $scriptRoot = $PSScriptRoot }
    cd $PSScriptRoot\..

    # Clean packages
    Get-ChildItem * -Include *.nupkg -Recurse | Remove-Item

    # Test the solution
    dotnet test -c Release -l trx -v n --no-build -d .\artifacts\test\test.log --results-directory .\artifacts\test
    exit $LASTEXITCODE
}
catch {
    Write-Host $_.ScriptStackTrace
    exit 1
}
