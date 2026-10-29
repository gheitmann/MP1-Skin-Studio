param([string]$MediaPortalDirectory='',[switch]$Uninstall,[switch]$RequestElevation)
$ErrorActionPreference='Stop'
if(-not $MediaPortalDirectory){foreach($candidate in @((Join-Path $env:ProgramFiles 'Team MediaPortal\MediaPortal'),(Join-Path ${env:ProgramFiles(x86)} 'Team MediaPortal\MediaPortal'))){if(Test-Path -LiteralPath (Join-Path $candidate 'Core.dll')){$MediaPortalDirectory=$candidate;break}}}
if(-not $MediaPortalDirectory){throw 'Specify -MediaPortalDirectory with your MediaPortal installation folder.'}
$client=(Resolve-Path -LiteralPath $MediaPortalDirectory).Path
$exe=Join-Path $client 'MediaPortal.exe'
if(-not(Test-Path -LiteralPath $exe)){throw 'This folder does not contain MediaPortal.exe.'}
$version=[Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
if($version.FileMajorPart -ne 1 -or $version.FileMinorPart -ne 39){throw 'This bridge is built and checked for MediaPortal 1.39. Rebuild against other MP1 SDK versions before using it.'}
if($RequestElevation){
 $principal=New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
 if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){
  $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$PSCommandPath+'"'),'-MediaPortalDirectory',('"'+$client.TrimEnd('\')+'"'))
  if($Uninstall){$arguments+='-Uninstall'}
  $shell=Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
  $elevated=Start-Process -FilePath $shell -ArgumentList $arguments -Verb RunAs -WindowStyle Hidden -Wait -PassThru
  if($elevated.ExitCode -ne 0){throw 'The administrator installation did not complete.'}
  Write-Output 'Administrator installation completed.';return
 }
}
foreach($process in @(Get-Process MediaPortal -ErrorAction SilentlyContinue)){if($process.Path -and [IO.Path]::GetFullPath($process.Path).Equals($exe,[StringComparison]::OrdinalIgnoreCase)){throw 'Close MediaPortal before installing or removing its plugin. Skin editing itself will not require a restart.'}}
$destination=Join-Path $client 'plugins\process\MP1SkinStudio.LiveBridge.dll'
if($Uninstall){if(Test-Path -LiteralPath $destination){Remove-Item -LiteralPath $destination};Write-Output 'LiveBridge removed. Skin and configuration files were not changed.';return}
$source=Join-Path $PSScriptRoot 'bin\MP1SkinStudio.LiveBridge.dll'
if(-not(Test-Path -LiteralPath $source)){throw 'The package is incomplete: LiveBridge/bin/MP1SkinStudio.LiveBridge.dll is missing.'}
[IO.Directory]::CreateDirectory((Split-Path $destination)) | Out-Null
if(Test-Path -LiteralPath $destination){$backup=$destination+'.backup-'+(Get-Date -Format 'yyyyMMddHHmmssfff');Copy-Item -LiteralPath $destination -Destination $backup}
Copy-Item -LiteralPath $source -Destination $destination
if((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash){throw 'Plugin verification failed.'}
Write-Output 'LiveBridge installed. Enable MP1 Skin Studio Live under MediaPortal process plugins if needed, then start MediaPortal once. Connect Studio to the same skin, theme and page. No restart is needed for supported live edits.'
