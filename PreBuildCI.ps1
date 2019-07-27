# Build prepartion script for continous integration
# This script will update version ID in AppXManifest file

param(
	[string] $Major,
	[string] $Minor,
	[string] $BuildConfig,
	[string] $ProjectDir
)

# Check if this is a CI build
$isCiBuild = ($env:BUILD_DEFINITIONNAME -ne $null)
$rev = 0
$tfPath = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\TF.exe"

$ProjectDir = $ProjectDir.Substring(0, $ProjectDir.Length - 1)

if ($isCiBuild -ne $true)
{
	$isTfPresent = Test-Path $tfPath

	if ($isTfPresent -ne $true)
	{
		Write-Warning "Unable to detect VS2017 TFVC tools."
		return
	}

	try
	{
		# Query TFVC for current changeset
		$tfQueryResult = . $tfPath history "$($ProjectDir)" /r /noprompt /stopafter:1 /version:W
		Write-Output $tfQueryResult
		$matchResult = "$tfQueryResult" -match "\d+"
	}
	catch
	{
		Write-Warning "Unable to retrieve changeset information."
		return
	}
}

# Default rev is 0
$rev = 0

# Local version will always be Major.Minor.Changeset.MMdd
# Store version will always be Major.Minor.Changeset.1000
# CI version will always be Major.Minor.Changeset.1100

# If debug configuration is used
if ($BuildConfig -eq "Debug")
{
	$rev = (Get-Date).ToString("MMdd")
}

if ($BuildConfig -eq "Release")
{
	$rev = 1000
}

if ($isCiBuild)
{
	$rev = 1100
	$buildVersion = $env:BUILD_SOURCEVERSION 
}
else
{
	$buildVersion = $matches[0]
}

if ($buildVersion)
{
	$version = "$($Major).$($Minor).$($buildVersion).$($rev)"
	[xml]$appxManifest = Get-Content "$($ProjectDir)Package.appxmanifest"
	$appxManifest.Package.Identity.Version = $version
	$appxManifest.Save("$($ProjectDir)Package.appxmanifest")
}