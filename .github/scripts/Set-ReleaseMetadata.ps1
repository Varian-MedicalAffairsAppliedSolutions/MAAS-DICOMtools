[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [String]
    $ProjectName,
    [Parameter(Mandatory=$true)]
    [String]
    $BuildNumber,
    [Parameter(Mandatory=$false)]
    [String]
    $ExpirationDate
)

$releaseDate = Get-Date -Format 'MM-dd-yyyy'
$tagDate = Get-Date -Format 'yyyyMMdd'

$releaseTag = "$ProjectName-v1.0.$BuildNumber-$tagDate"
$releaseName = "$ProjectName-v1.0.$BuildNumber ($releaseDate)"
$releaseFileName = "$ProjectName-v1.0.$BuildNumber-$releaseDate"

if (-not [string]::IsNullOrWhiteSpace($ExpirationDate)) {
    $normalizedExpirationDate = $ExpirationDate -replace '/', '-'
    $releaseName = "$releaseName exp-$ExpirationDate"
    $releaseFileName = "$releaseFileName-exp-$normalizedExpirationDate"
}

"RELEASE_TAG=$releaseTag" >> $env:GITHUB_OUTPUT
"RELEASE_NAME=$releaseName" >> $env:GITHUB_OUTPUT
"RELEASE_FILE_NAME=$releaseFileName" >> $env:GITHUB_OUTPUT
