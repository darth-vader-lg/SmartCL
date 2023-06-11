[CmdletBinding(PositionalBinding=$false)]
Param(
    [string][Alias('c')]$configuration = "Release",
    [string]$platform = $null,
    [string][Alias('k', "api-key")]$apiKey,
    [int][Alias('t')]$timeout = 3601, # Workaround for the timeout; it must not be a multiple of 60, otherwise it will be ignored
    [string][Alias('u')]$url,
    [switch][Alias('n')]$nuget,
    [switch][Alias('g')]$github,
    [switch][Alias('h')] $help,
    [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

function Print-Usage() {
    Write-Host "  -configuration <value>  Build configuration: 'Debug' or 'Release' (short: -c)"
    Write-Host "  -platform <value>       Platform configuration: 'x86', 'x64' or any valid Platform value"
    Write-Host "  -url <value>            The url wher to publish (short: -u)"
    Write-Host "  -nuget                  Publish on the standard NuGet.org index (short: -n)"
    Write-Host "  -github                 Publish on the standard GitHub index (short: -g)"
    Write-Host "  -api-key <value>        The api key with write rights for publishing (short: -k)"
    Write-Host "  -timeout <value>        The timeout for publishing, in seconds (short: -t)"
    Write-Host "  -help                   Print help and exit"
}

try {
    # Go to root directory
    if (-not($PSScriptRoot) -and $psISE) { $scriptRoot = Split-Path $psISE.CurrentFile.FullPath } else { $scriptRoot = $PSScriptRoot }
    cd $PSScriptRoot\..

    if ($help -or (($null -ne $properties) -and ($properties.Contains('/help') -or $properties.Contains('/?'))) -or (-not $url -and -not $nuget -and -not $github)) {
        Print-Usage
        exit 0
    }
    # Check for the api key consistence
    if (($apiKey -eq "") -and $nuget) {
        if (-not (Test-Path env:NUGET_NUPKG_PUSH_KEY) -or $env:NUGET_NUPKG_PUSH_KEY -eq "") {
            Write-Error "Push api-key not specified!"
            Write-Error "Use the -api-key switch or define the environment variable NUGET_NUPKG_PUSH_KEY"
            Print-Usage
            exit 1
        }
    }
    if (($apiKey -eq "") -and $github) {
        if (-not (Test-Path env:GITHUB_NUPKG_PUSH_KEY) -or $env:GITHUB_NUPKG_PUSH_KEY -eq "") {
            Write-Error "Push api-key not specified!"
            Write-Error "Use the -api-key switch or define the environment variable GITHUB_NUPKG_PUSH_KEY"
            Print-Usage
            exit 1
        }
    }

    # Directory relative to the selected platform
    if ($platform -ne "") {
        $platformDir = $platform, $configuration -join "."
    }
    else {
        $platformDir = $configuration
    }

    # Create packages
    dotnet pack .\src\SmartCL\ -c Release -v n -o .\artifacts

    # List of packages to publish
    $packages = Get-ChildItem -Path .\artifacts -Filter "*.nupkg" -Recurse

    # Publish to url
    if ($url) {
        # Push the packages
        Write-Output "Pushing the packages on $url..."
        foreach ($package in $packages) {
            $prms = @($package.FullName, "--force-english-output", "-n",  "--timeout", $timeout, "--source", $url)
            $pathInfo = [System.Uri]$url
            if ($pathInfo.IsUnc) {
                $prms = $prms + @("--skip-duplicate")
                if ($key) { $prms = $prms + @("--api-key", $key) }
            }
            elseif (-not(Test-Path -PathType Container -Path $url)) {
                New-Item -ItemType Directory -Path $url
            }
            dotnet nuget push $prms
        }
    }

    # Publish on nuget
    if ($nuget) {
        # Push the packages
        Write-Output "Pushing the packages on NuGet..."
        if ($apiKey -eq "") { $key = $env:NUGET_NUPKG_PUSH_KEY } else { $key = $apiKey }
        foreach ($package in $packages) {
            dotnet nuget push $package.FullName --force-english-output --skip-duplicate -n --timeout $timeout --source "https://api.nuget.org/v3/index.json" --api-key $key
        }
    }

    # Publish on GitHub
    if ($github) {
        # GitHub package destination
        $pushOrigin = (((git remote -v) -match "(push)") -split '\t')[0]
        $pushUrl = git remote get-url --push $pushOrigin
        $user = Split-Path -leaf (Split-Path $pushUrl)
        $source = "https://nuget.pkg.github.com/", $user, "/index.json" -join ''
        # Push the packages
        Write-Output "Pushing the packages on GitHub..."
        if ($apiKey -eq "") { $key = $env:GITHUB_NUPKG_PUSH_KEY } else { $key = $apiKey }
        foreach ($package in $packages) {
            dotnet nuget push $package.FullName --force-english-output --skip-duplicate -n --timeout $timeout --source $source --api-key $key
        }
    }

    exit 0
}
catch {
    Write-Host $_.ScriptStackTrace
    exit 1
}
