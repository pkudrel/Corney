#requires -Version 3.0
<#
.Synopsis
	Misc Updates
#>


[CmdletBinding()]
param(
    $scriptsPath = (Split-Path $MyInvocation.MyCommand.Path -Parent),
    $toolsPath = (Join-Path $scriptsPath "\tools\"),
    $ibVersionFile = (Join-Path $toolsPath "ib-version.txt")
)



. (Join-Path $scriptsPath "ps\ib-update-tools.ps1")


$isUptodate = IbUpdateIsNeeded $ibVersionFile
if ($isUptodate -eq $true) {
    IbUpdateInvokeBuild $toolsPath
} else {
    "Invoke-Build is uptodate"
}


    