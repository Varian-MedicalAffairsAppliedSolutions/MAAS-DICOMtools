[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [String]
    $CsprojFileName,
    [Parameter(Mandatory=$false)]
    [String]
    $CsprojFilePath = "."
)

# xml.Save requires an absolute path.
$csprojAbsoluteFilePath = Join-Path $CsprojFilePath $CsprojFileName | Resolve-Path
Write-Host "Updating $csprojAbsoluteFilePath"

if (-not $env:GITHUB_WORKSPACE_PACKAGES_PATH) {
    throw "GITHUB_WORKSPACE_PACKAGES_PATH is not set."
}

$xml = [xml](Get-Content $csprojAbsoluteFilePath)

$references = $xml.Project.ItemGroup |
    Foreach-Object { $_.Reference } |
    Where-Object { $_ -and $_.Include -match "VMS.TPS.Common.Model" }

if (-not $references) {
    throw "No VMS.TPS.Common.Model references were found in $csprojAbsoluteFilePath"
}

$references |
Foreach-Object {
    # Remove everything after the package name (Version, Culture, PublicKeyToken, etc.)
    $newInclude = $_.Include -replace ",.*", ""
    Write-Host "Replacing $($_.Include) with $newInclude"

    $_.RemoveAll()
    $_.SetAttribute("Include", $newInclude)

    $specificVersion = $xml.CreateElement("SpecificVersion", $xml.DocumentElement.NamespaceURI)
    $specificVersion.InnerText = "False"
    $_.AppendChild($specificVersion) | Out-Null

    $copyToLocal = $xml.CreateElement("Private", $xml.DocumentElement.NamespaceURI)
    $copyToLocal.InnerText = "False"
    $_.AppendChild($copyToLocal) | Out-Null

    $hintPath = $xml.CreateElement("HintPath", $xml.DocumentElement.NamespaceURI)
    $hintPath.InnerText = Join-Path $Env:GITHUB_WORKSPACE_PACKAGES_PATH "$newInclude.dll"
    $_.AppendChild($hintPath) | Out-Null
    Write-Host "Added hint path for ${newInclude}: $($hintPath.InnerText)"
}

Write-Host "Saving updated CSPROJ"
$xml.Save($csprojAbsoluteFilePath)
