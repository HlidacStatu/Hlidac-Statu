param(
    [Parameter(Mandatory = $true)]
    [string]$CsprojPath
)

if (-not (Test-Path $CsprojPath)) {
    Write-Error "File not found: $CsprojPath"
    exit 1
}

[xml]$xml = Get-Content -Path $CsprojPath

# Get Version element (handles multiple PropertyGroup blocks)
$versionNodes = $xml.Project.PropertyGroup.Version
if ($versionNodes -is [System.Array]) {
    $version = $versionNodes[0]
} else {
    $version = $versionNodes
}

if (-not $version) {
    Write-Error "No <Version> element found in $CsprojPath"
    exit 1
}

Write-Host "##teamcity[setParameter name='WEBGENERATOR_VERSION' value='$version']"

