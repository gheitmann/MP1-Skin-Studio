param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'));$out=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $out){throw 'Choose a fresh report directory.'}
[IO.Directory]::CreateDirectory($out) | Out-Null;Copy-Item -LiteralPath (Join-Path $root 'MP1SkinStudio.exe') -Destination $out
& ($env:WINDIR+'\Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /r:System.dll /r:System.Core.dll /r:System.Xml.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ('/r:'+(Join-Path $out 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $out 'Probe.exe')) (Join-Path $PSScriptRoot 'Probe.cs')
if($LASTEXITCODE -ne 0){throw 'Save dialog probe compilation failed.'}
$p=Start-Process -FilePath (Join-Path $out 'Probe.exe') -ArgumentList ('"'+(Join-Path $out 'Report')+'"') -WindowStyle Hidden -Wait -PassThru
Get-Content -LiteralPath (Join-Path $out 'Report\Results.txt')
if($p.ExitCode -ne 0){throw 'Save dialog probe failed.'}
