#requires -Version 3.0
<#
.Synopsis
	Build luncher for (https://github.com/nightroman/Invoke-Build)
	This script create spacial variable $BL
#>

param(
	$scriptFile = (Join-Path $PSScriptRoot ".build.ps1"),
	$major = 0,
	$minor = 0,
	$patch = 0,
	$buildCounter = 0,
	$psGitVersionStrategy = "standard"
)



# 
$BL = @{}
$BL.RepoRoot = (Resolve-Path ( & git rev-parse --show-toplevel))
$BL.BuildDateTime = ((Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"))
$BL.ScriptsPath = (Split-Path $MyInvocation.MyCommand.Path -Parent)
$BL.BuildOutPath = (Join-Path $BL.RepoRoot ".build" )
$BL.BuildScriptPath = $scriptFile
$BL.PsAutoHelpers = (Join-Path $BL.ScriptsPath "vendor\ps-auto-helpers") 
$BL.ib = (Join-Path $BL.ScriptsPath "vendor\ps-auto-helpers\tools\ib\Invoke-Build.ps1")
$BL.ibVersionFile = (Join-Path $BL.ScriptsPath "vendor\ps-auto-helpers\tools\ib-version.txt")

# import tools
. (Join-Path $BL.PsAutoHelpers "ps\psgitversion.ps1")
. (Join-Path $BL.PsAutoHelpers "ps\ib-update-tools.ps1")

# Invoke-Build info
Write-Output "Invoke-Build: Script file: $scriptFile"
IbUpdateIsNeeded $BL.ibVersionFile | Out-Null

$BL.BuildVersion = Get-GitVersion $psGitVersionStrategy $major $minor $patch $buildCounter
$buildMiscInfo = $BL.BuildVersion.AssemblyInformationalVersion
Write-Output "`$BL values"
$BL.GetEnumerator()| Sort-Object -Property name | Format-Table Name, Value -AutoSize

try {
	# Invoke the build and keep results in the variable Result
	& $BL.ib -File $BL.BuildScriptPath -Result Result  @args
}
catch {
	Write-Output $Result.Error
	Write-Output $_
	exit 1 # Failure
}

$Result.Tasks | Format-Table Elapsed, Name -AutoSize
exit 0
