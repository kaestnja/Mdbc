<#
.Synopsis
	Build script, https://github.com/nightroman/Invoke-Build
#>

param(
	$Configuration = 'Release'
	,
	[ValidateSet('net472', 'netstandard2.0')]
	$TargetFramework = 'net472'
)

Set-StrictMode -Version Latest
$ModuleName = 'Mdbc'

# module root for publish
# netX
$ModuleRoot1 = if ($env:ProgramW6432) {$env:ProgramW6432} else {$env:ProgramFiles}
$ModuleRoot1 = "$ModuleRoot1\WindowsPowerShell\Modules\$ModuleName"
# netstandardX
$ModuleRoot2 = Join-Path ([Environment]::GetFolderPath('MyDocuments')) PowerShell\Modules\$ModuleName
# current
if ($TargetFramework -eq 'net472') {
	$ModuleRoot = $ModuleRoot1
}
else {
	$ModuleRoot = $ModuleRoot2
}

# Get version from release notes.
function Get-Version {
	switch -Regex -File Release-Notes.md {'##\s+v(\d+\.\d+\.\d+)' {return $Matches[1]} }
}

$MetaParam = @{
	Inputs = '.build.ps1', 'Release-Notes.md'
	Outputs = "Module\$ModuleName.psd1", 'Src\AssemblyInfo.cs'
}

# Synopsis: Generate or update meta files.
task meta @MetaParam {
	$Version = Get-Version
	$Project = 'https://github.com/nightroman/Mdbc'
	$Summary = 'Mdbc module - MongoDB Cmdlets for PowerShell'
	$Copyright = 'Copyright (c) Roman Kuzmin'

	Set-Content Module\$ModuleName.psd1 @"
@{
	Author = 'Roman Kuzmin'
	ModuleVersion = '$Version'
	Description = '$Summary'
	CompanyName = 'https://github.com/nightroman'
	Copyright = '$Copyright'

	RootModule = '$ModuleName.dll'
	RequiredAssemblies = 'MongoDB.Bson.dll', 'MongoDB.Driver.Core.dll', 'MongoDB.Driver.dll'

	PowerShellVersion = '3.0'
	GUID = '12c81cd8-bde3-4c91-a292-e6c4f868106a'

	AliasesToExport = @()
	VariablesToExport = @()
	FunctionsToExport = @()
	CmdletsToExport = @(
		'Add-MdbcCollection'
		'Add-MdbcData'
		'Connect-Mdbc'
		'Export-MdbcData'
		'Get-MdbcCollection'
		'Get-MdbcData'
		'Get-MdbcDatabase'
		'Import-MdbcData'
		'Invoke-MdbcAggregate'
		'Invoke-MdbcCommand'
		'New-MdbcData'
		'Register-MdbcClassMap'
		'Remove-MdbcCollection'
		'Remove-MdbcData'
		'Remove-MdbcDatabase'
		'Rename-MdbcCollection'
		'Set-MdbcData'
		'Update-MdbcData'
		'Use-MdbcTransaction'
		'Watch-MdbcChange'
	)

	PrivateData = @{
		PSData = @{
			Tags = 'Mongo', 'MongoDB', 'Database'
			ProjectUri = '$Project'
			LicenseUri = 'http://www.apache.org/licenses/LICENSE-2.0'
			ReleaseNotes = '$Project/blob/main/Release-Notes.md'
		}
	}
}
"@

	Set-Content Src\AssemblyInfo.cs @"
using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyProduct("$ModuleName")]
[assembly: AssemblyVersion("$Version")]
[assembly: AssemblyTitle("$Summary")]
[assembly: AssemblyCompany("$Project")]
[assembly: AssemblyCopyright("$Copyright")]

[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
"@
}

# Synopsis: Build the project (and post-build Publish).
task build meta, {
	exec { dotnet build Src\$ModuleName.csproj -c $Configuration -f $TargetFramework }
},
Help

# Synopsis: Build all frameworks.
task build2 {
	Invoke-Build Build -Configuration $Configuration -TargetFramework net472
	Invoke-Build Build -Configuration $Configuration -TargetFramework netstandard2.0
}

# Synopsis: Publish the module (post-build).
task publish {
	if ($TargetFramework -eq 'net472') {
		exec { robocopy Module $ModuleRoot /s /np /r:0 /xf *-Help.ps1 } (0..3)
		exec { robocopy Src\bin\$Configuration\$TargetFramework $ModuleRoot /s /np /r:0 } (0..3)
	}
	else {
		exec { dotnet publish Src\$ModuleName.csproj -c $Configuration -f $TargetFramework --no-build }
		exec { robocopy Module $ModuleRoot /s /np /r:0 /xf *-Help.ps1 } (0..3)
		exec { robocopy Src\bin\$Configuration\$TargetFramework\publish $ModuleRoot /s /np /r:0 } (0..3)

		# tweak manifest requirements
		Import-Module PsdKit
		$xml = Import-PsdXml $ModuleRoot\Mdbc.psd1
		Set-Psd $xml '5.1' 'Data/Table/Item[@Key="PowerShellVersion"]'
		Export-PsdXml $ModuleRoot\Mdbc.psd1 $xml
	}
}

# Synopsis: Remove temp files.
task clean {
	remove *.nupkg, z, Src\bin, Src\obj, README.htm
}

# Synopsis: Build help by Helps (https://github.com/nightroman/Helps).
task help @{
	Inputs = {Get-Item Src\Commands\*, Module\en-US\$ModuleName.dll-Help.ps1}
	Outputs = {"$ModuleRoot\en-US\$ModuleName.dll-Help.xml"}
	Jobs = {
		. Helps.ps1
		Convert-Helps Module\en-US\$ModuleName.dll-Help.ps1 $Outputs
	}
}

# Synopsis: Test help script examples.
task testHelpExample {
	. Helps.ps1
	Test-Helps Module\en-US\$ModuleName.dll-Help.ps1
}

# Synopsis: Test synopsis of each cmdlet and warn about unexpected.
task testHelpSynopsis {
	Import-Module Mdbc
	Get-Command *-Mdbc* -CommandType cmdlet | Get-Help | .{process{
		if (!$_.Synopsis.EndsWith('.')) {
			Write-Warning "$($_.Name) : unexpected/missing synopsis"
		}
	}}
}

# Synopsis: Update help then run help tests.
task testHelp help, testHelpExample, testHelpSynopsis

# Synopsis: Convert markdown to HTML.
task markdown {
	assert (Test-Path $env:MarkdownCss)
	exec { pandoc.exe @(
		'README.md'
		'--output=README.htm'
		'--from=gfm'
		'--embed-resources'
		'--standalone'
		"--css=$env:MarkdownCss"
		"--metadata=pagetitle=$ModuleName"
	)}
}

# Synopsis: Set $script:Version.
task version {
	($script:Version = Get-Version)
	# manifest version
	$data = & ([scriptblock]::Create([IO.File]::ReadAllText("$ModuleRoot\$ModuleName.psd1")))
	assert ($data.ModuleVersion -eq $script:Version)
	# assembly version
	assert ((Get-Item $ModuleRoot\$ModuleName.dll).VersionInfo.FileVersion -eq ([Version]"$script:Version.0"))
}

# Synopsis: Make the package in z\tools.
task package {equals $Configuration Release}, updateScript, build, testHelp, test5, markdown, {
	remove z
	$null = mkdir z\tools\$ModuleName\Scripts

	Copy-Item -Recurse -Destination z\tools\$ModuleName $(
		'LICENSE'
		'README.htm'
		"$ModuleRoot\*"
	)

	Copy-Item -Destination z\tools\$ModuleName\Scripts $(
		'.\Scripts\Mdbc.ArgumentCompleters.ps1'
		'.\Scripts\Update-MongoFiles.ps1'
	)
}

# Synopsis: Make NuGet package.
task nuget package, version, {
	$text = @'
Mdbc is the PowerShell module based on the official MongoDB C# driver.
Mdbc makes MongoDB data and operations PowerShell friendly.
'@
	# nuspec
	Set-Content z\Package.nuspec @"
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
	<metadata>
		<id>$ModuleName</id>
		<version>$Version</version>
		<authors>Roman Kuzmin</authors>
		<owners>Roman Kuzmin</owners>
		<projectUrl>https://github.com/nightroman/Mdbc</projectUrl>
		<license type="expression">Apache-2.0</license>
		<requireLicenseAcceptance>false</requireLicenseAcceptance>
		<summary>$text</summary>
		<description>$text</description>
		<tags>Mongo MongoDB PowerShell Module Database</tags>
		<releaseNotes>https://github.com/nightroman/Mdbc/blob/main/Release-Notes.md</releaseNotes>
	</metadata>
</package>
"@
	# pack
	exec { NuGet pack z\Package.nuspec -NoPackageAnalysis }
}

# Synopsis: Push to the repository with a version tag.
task pushRelease version, {
	$changes = exec { git status --short }
	assert (!$changes) "Please, commit changes."

	exec { git push }
	exec { git tag -a "v$Version" -m "v$Version" }
	exec { git push origin "v$Version" }
}

# Synopsis: Make and push the NuGet package.
task pushNuGet nuget, {
	assert ($TargetFramework -eq 'net472')
	$ApiKey = Read-Host nuget.org-ApiKey
	exec { NuGet push "$ModuleName.$Version.nupkg" -Source nuget.org -ApiKey $ApiKey }
},
clean

# Synopsis: Make and push the PSGallery package.
task pushPSGallery package, version, {
	equals $TargetFramework netstandard2.0
	$NuGetApiKey = Read-Host NuGetApiKey
	Publish-Module -Path z/tools/$ModuleName -NuGetApiKey $NuGetApiKey
},
clean

# Synopsis: Copy external scripts to the project.
task updateScript @{
	Partial = $true
	Inputs = {
		Get-Command Mdbc.ArgumentCompleters.ps1, Update-MongoFiles.ps1 |
		.{process{ $_.Definition }}
	}
	Outputs = {process{
		$2 = "Scripts\$(Split-Path -Leaf $_)"
		$item1 = Get-Item -LiteralPath $_
		$item2 = Get-Item -LiteralPath $2
		if ($item1.LastWriteTimeUtc -lt $item2.LastWriteTimeUtc) {
			Write-Warning "Input is older: $_ $2"
			Assert-SameFile $_ $2
			Copy-Item $_ $2
		}
		$2
	}}
	Jobs = {process{
		Copy-Item $_ $2
	}}
}

# Synopsis: Remove test.test* collections
task cleanTest {
	Import-Module Mdbc
	foreach($name in Connect-Mdbc . test *) {
		if ($name -like 'test*') {
			Remove-MdbcCollection $name
		}
	}
}

# Synopsis: Test in the current PowerShell.
task test5 {
	Invoke-Build ** Tests
},
cleanTest

# Synopsis: Test in PowerShell Core.
task test7 -If $env:pwsh {
	exec { & $env:pwsh -NoProfile -Command Invoke-Build test5 }
}

# Synopsis: Build, test and clean all.
task . build2, testHelp, test5, test7, clean
