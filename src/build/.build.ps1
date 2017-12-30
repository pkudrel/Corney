<#
.Synopsis
	Build script (https://github.com/nightroman/Invoke-Build)
#>

[cmdletBinding()]
param(
	$appName = "Corney",
	$serverDir = "C:\work\users\AntyPiracy\",
	$toolsDir = (Join-Path $BL.ScriptsPath  "tools"),
	$scriptsPath = $BL.ScriptsPath,
	$nuget = (Join-Path $toolsDir  "nuget/nuget.exe"),
	$libz = (Join-Path $toolsDir  "LibZ.Tool/tools/libz.exe"),
	$7zip = (Join-Path $toolsDir  "7-Zip.CommandLine/tools/7za.exe"),

	$srcDir = (Join-Path $BL.RepoRoot "src"),
	$sln  = (Join-Path $BL.RepoRoot  "/src/Corney.sln" ),
	$buildTmpDir  = (Join-Path $BL.BuildOutPath "tmp" ),

	$buildReadyDir  = (Join-Path $BL.BuildOutPath "ready" ),
	$Dirs = (@{"marge" = "marge"; "build" = "build"; "nuget" = "nuget"; "main" = "main"}),
	$buildWorkDir  = (Join-Path $buildTmpDir "build" ),
	$target  = "Release",
	$donotMarge =  @(),
	$projectCorney  = @{
		name = "Corney";
		file = (Join-Path $BL.RepoRoot  "/src/Corney/Corney.csproj" );
		exe = "Corney.exe";
		dir = "Corney";
		dstExe = "Corney.exe";
	},

	$projects = @($projectCorney)
)





# Msbuild 
Set-Alias MSBuild (Resolve-MSBuild)

# inser tools
. (Join-Path $BL.ScriptsPath  "vendor\ps-auto-helpers\ps\misc.ps1")
. (Join-Path $BL.ScriptsPath  "vendor\ps-auto-helpers\ps\io.ps1")
. (Join-Path $BL.ScriptsPath  "vendor\ps-auto-helpers\ps\syrup.ps1")
. (Join-Path $BL.ScriptsPath  "vendor\ps-auto-helpers\ps\assembly-tools.ps1")


# Synopsis: Package-Restore
task RestorePackage {

	 Set-Location   $BL.RepoRoot
	"Restore packages: Sln: {$sln}"
	exec {  &$nuget restore $sln  }
}

task Startup-TeamCity {

	if ($env:TEAMCITY_VERSION) {
		$tvc = $env:TEAMCITY_VERSION
		Write-Build Green "Setup TeamCity: $tvc" 
		$s = $BL.BuildVersion.SemVer
		"##teamcity[buildNumber '$s']"
		"##vso[build.updatebuildnumber]$s"
		try {
			$max = $host.UI.RawUI.MaxPhysicalWindowSize
			if($max) {
			$host.UI.RawUI.BufferSize = New-Object System.Management.Automation.Host.Size(9999,9999)
			$host.UI.RawUI.WindowSize = New-Object System.Management.Automation.Host.Size($max.Width,$max.Height)
		}
		} catch {}
	}

}

task CheckTools {
	Write-Build Green "Check: Nuget"
	DownloadIfNotExists "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" $nuget 
	DownloadNugetIfNotExists $nuget "LibZ.Tool" $toolsDir $libz
	DownloadNugetIfNotExists $nuget "7-Zip.CommandLine" $toolsDir $7zip

}

# Synopsis: Build the project.
task Build {
	
	Write-Build Green "*** Build *** "
	$outMain = (Join-Path $buildTmpDir $Dirs.build  )
	EnsureDirExistsAndIsEmpty $buildWorkDir

	foreach ($p in $projects) {
		
			$out = (Join-Path $outMain  $p.dir )
			
			try {

				EnsureDirExistsAndIsEmpty $out 
				$projectFile = $p.file

				"Build; Project file: $projectFile"
				"Build; out dir: $out"
				"Build; Target: $target"
				
				$bv = $BL.BuildVersion

				"AssemblyVersion: $($bv.AssemblyVersion)"
				"AssemblyVersion: $($bv.AssemblyFileVersion)"
				"AssemblyVersion: $($bv.AssemblyInformationalVersion)"

				$srcWorkDir = Join-Path $srcDir $p.dir
				BackupTemporaryFiles $srcWorkDir  "Properties\AssemblyInfo.cs"
				UpdateAssemblyInfo $srcWorkDir $bv.AssemblyVersion $bv.AssemblyFileVersion $bv.AssemblyInformationalVersion $p.name "DenebLab" "DenebLab"
				exec { MSBuild $projectFile /v:quiet  /p:Configuration=$target /p:OutDir=$out  } 
			}

			catch {
				RestoreTemporaryFiles $srcWorkDir
				throw $_.Exception
				exit 1
			}
			finally {
				RestoreTemporaryFiles $srcWorkDir
			}
	}
}



# Synopsis: Marge 
task Marge  {	

	Write-Build Green "*** Marge ***"
	foreach ($p in $projects) {

		$buildDir = [System.IO.Path]::Combine( $buildTmpDir , $Dirs.build,  $p.dir )
		$margedDir = [System.IO.Path]::Combine( $buildTmpDir , $Dirs.marge,  $p.dir )
	
		Set-Location  $buildDir
		EnsureDirExistsAndIsEmpty $margedDir 

		$dlls = [System.IO.Directory]::GetFiles($buildDir, "*.dll")
		$exclude = $donotMarge | Foreach-Object { "--exclude=$_" }

		foreach ($f in  $dlls ){
			Copy-Item $f -Destination $margedDir
		}
	
		Copy-Item $p.exe  -Destination $margedDir 
		Copy-Item "$($p.exe).config"  -Destination $margedDir 
		
		
		$src = "$scriptsPath/nlog/$($p.name).NLog.config"
		$dst = "$margedDir/NLog.config"
		"Copy fixed version NLog.config; Src: $src ; Dst: $dst "
		Copy-Item   $src  -Destination $dst -Force

		
		if([System.IO.File]::Exists("NLog.config")){
			Copy-Item "NLog.config"  -Destination $margedDir
		}
		Set-Location  $margedDir
		& $libz inject-dll --assembly $p.exe --include *.dll  $exclude --move	
	
	}
	
	# fix for nlog
	#$src = [System.IO.Path]::Combine( $buildTmpDir , $Dirs.build,  "ManualEvaluator.Desktop", "NLog.Windows.Forms.dll" )
	#$dst = [System.IO.Path]::Combine( $buildTmpDir , $Dirs.marge,   "ManualEvaluator.Desktop" )
	#Copy-Item   $src  -Destination $dst -Force

	#$src = [System.IO.Path]::Combine( $buildTmpDir , $Dirs.build,  "SiteScraper.Desktop", "NLog.Windows.Forms.dll" )
	#$dst = [System.IO.Path]::Combine( $buildTmpDir , $Dirs.marge,   "SiteScraper.Desktop" )
	#Copy-Item   $src  -Destination $dst -Force

}


# Synopsis: Make nuget file
task Make-Nuget  {

	 "*** Make-Nuget  ***"
	$margedDir = (Join-Path $buildTmpDir $Dirs.marge  )
	$nugetDir = (Join-Path $buildTmpDir $Dirs.nuget  )
	$mainDir = (Join-Path $nugetDir $Dirs.main  )
	$syrupDir = Join-Path $nugetDir "/_syrup"
	$syruScriptspDir = Join-Path $syrupDir "scripts"

	

	EnsureDirExistsAndIsEmpty $nugetDir
	EnsureDirExistsAndIsEmpty $mainDir
	EnsureDirExistsAndIsEmpty $syrupDir
	EnsureDirExistsAndIsEmpty $syruScriptspDir
	
	$src =  "$margedDir/*"
	$dst = $mainDir
	"Copy main; Src: $src ; Dst: $dst"
	Copy-Item  $src  -Recurse  -Destination $dst


	



	$src = "$scriptsPath/syrup/scripts/*"
	$dst = $syruScriptspDir
	"Copy scripts; Src: $src ; Dst: $dst"
	Copy-Item  $src -Destination $dst -Recurse


	$spacFilePath = Join-Path $scriptsPath "nuget\nuget.nuspec"
	$specFileOutPath = Join-Path $nugetDir "$appName.nuspec"
	
    
    $spec = [xml](get-content $spacFilePath)
    $spec.package.metadata.version = ([string]$spec.package.metadata.version).Replace("{Version}", $BL.BuildVersion.SemVer)
    $spec.Save($specFileOutPath )

	$readyDir =  Join-Path $buildReadyDir  $Dirs.nuget
	EnsureDirExistsAndIsEmpty $readyDir
    exec { &$nuget pack $specFileOutPath -OutputDirectory $readyDir  -NoPackageAnalysis}
	$nugetFile =  ([System.IO.Directory]::GetFiles($readyDir , "*.nupkg"))[0]
	SyrupGenerateInfoFile $nugetFile $appName  $BL.BuildVersion.SemVer "prod" $BL.BuildDateTime
}



task Publish-Local -If (-not   $env:TEAMCITY_VERSION ) {
    "Publish local"
	$devDir = (Join-Path $BL.RepoRoot ".dev")
	$syrupDir = (Join-Path $devDir ".app.syrup")
	$appVs = (Join-Path $devDir  ".app.vs")
	$workDir = (Join-Path $devDir  "work-dir")
		
	


	"Make .app.vs"
	

	"# Make .app.standolone"
	$appAlone = (Join-Path $devDir  ".app.standolone")
	
	$configDir = (Join-Path $appAlone "config")
	$appDir = (Join-Path $appAlone "app")
	$currentAppDir = (Join-Path $appDir $appName)
	
	$syrupMainDir = (Join-Path $appAlone ".syrup")
	
	EnsureDirExists  $appAlone
	EnsureDirExistsAndIsEmpty  $appDir
	EnsureDirExistsAndIsEmpty  $currentAppDir
	EnsureDirExists  $syrupMainDir
	EnsureDirExists  $configDir

	"Main program"
	$margedDir = (Join-Path $buildTmpDir $Dirs.marge  )
	$src = "$margedDir/*"
	$dst = $currentAppDir 
	"Copy main; Src: $src ; Dst: $dst"
	Copy-Item  $src  -Recurse  -Destination $dst
	
	
	"Global config"
	foreach ($p in $projects) {
	
		$appDir = [System.IO.Path]::Combine( $currentAppDir , $p.dir )
		$dst = (Join-Path $appAlone "$($p.name).lnk")
		$src = [System.IO.Path]::Combine( $appDir, $p.exe )
		$ws = New-Object -ComObject WScript.Shell; 
		$s = $ws.CreateShortcut($dst)
		$s.TargetPath = $src 
		$s.WorkingDirectory = $appDir
		$s.Save()
	}



	"# Make .app.syrup"
	$syrupDir = (Join-Path $devDir ".app.syrup")
	$syrupConfigDir = (Join-Path $syrupDir "config")
	$syrupAppDir = (Join-Path $syrupDir "app")
	$syrupMainDir = (Join-Path $syrupDir ".syrup")
	$syrupNugetDir = (Join-Path $syrupMainDir "nuget")
	$nugetSrcDir =  Join-Path $buildReadyDir  $Dirs.nuget
	
	



	$currentNugetDir = "$appName.$($BL.BuildVersion.SemVer)"
	$nugetDstDir =  Join-Path $syrupNugetDir  $currentNugetDir

	$nugetDstDir
	EnsureDirExistsAndIsEmpty $syrupAppDir
	EnsureDirExistsAndIsEmpty $syrupNugetDir
	EnsureDirExistsAndIsEmpty $nugetDstDir

	$src = "$nugetSrcDir/*"
	$dst = $nugetDstDir
	"Copy to dev syrup version; Src: $src ; Dst: $dst"
	Copy-Item $src  -Destination  $dst

	"Remove lnk"
	Set-Location  $syrupDir
	remove-item *.lnk  


}

task Publish-TeamCity -If ($env:TEAMCITY_VERSION ) {
	"Publish teamcity"
	

	$brnach = & git rev-parse --abbrev-ref HEAD
	$serverAppDir = Join-Path $serverDir $appName 
	
	EnsureDirExists $serverAppDir

	$prodPath = Join-Path $serverAppDir "syrup-production"
	$devPath = Join-Path $serverAppDir "syrup-develop"
	$readyDir =  Join-Path $buildReadyDir  $Dirs.nuget
	$src = "$readyDir/*"

	EnsureDirExists $prodPath
	EnsureDirExists $devPath 
	

	if($brnach -eq "production"){

		$dst = $prodPath
		"Copy to syrup production; Src:$src ; Dst: $dst"
		Copy-Item $src  -Destination  $dst

		$dst = $devPath
		"Copy to syrup develop; Src:$src ; Dst: $dst"
		Copy-Item $src  -Destination  $dst

	} else {

		$dst = $devPath
		"Copy to syrup develop; Src:$src ; Dst: $dst"
		Copy-Item $src  -Destination  $dst

	}

}


# Synopsis: Remove temp files.
task Clean {
 #   Remove-Item bin, obj -Recurse -Force -ErrorAction 0
}


function DownloadIfNotExists($src , $dst){

	If (-not (Test-Path $dst)){
		$dir = [System.IO.Path]::GetDirectoryName($dst)
		If (-not (Test-Path $dir)){
			New-Item -ItemType directory -Path $dir
		}
	 	Invoke-WebRequest $src -OutFile $dst
	} 
}


function GetFileFromDropbox ($apiKey, $filePath) {

		
		$headers = @{}
		$headers.Add("Authorization",("Bearer {0}" -f $apiKey))
		$headers.Add('Dropbox-API-Arg', ('{"path":"' + $filePath + '"}'))
		$apiUrl = "https://content.dropboxapi.com/2/files/download";
		$r = Invoke-RestMethod  -Uri $apiUrl `
								-Headers $headers `
								-ContentType "" `
								-Method POST 
		
		# remove BOM from string
        $r = $r -replace "\xEF\xBB\xBF", ""

		return $r 
}




# Synopsis: Build and clean.

task Startup  Startup-TeamCity, CheckTools
task BuildTask RestorePackage,  Build
task Publish Publish-Local, Publish-TeamCity
task . Startup, BuildTask, Marge, Make-Nuget,  Publish

