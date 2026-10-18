param([Parameter(Mandatory=$true)][string]$MP1Directory)
$ErrorActionPreference='Stop'
$runtime=(Resolve-Path -LiteralPath $MP1Directory).Path
if(-not (Test-Path -LiteralPath (Join-Path $runtime 'Core.dll'))){throw 'Original MP1 Core.dll is missing.'}
if(-not (Test-Path -LiteralPath (Join-Path $runtime 'MediaPortalDirs.xml'))){throw 'Isolated MP1 directory configuration is missing.'}
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$source=Join-Path $PSScriptRoot 'NativeGridProbe.cs'
$output=Join-Path $runtime 'NativeGridProbe.exe'
& (Join-Path $framework 'csc.exe') /nologo /target:exe /platform:x64 ('/out:'+$output) ('/r:'+(Join-Path $runtime 'Core.dll')) ('/r:'+(Join-Path $runtime 'Utils.dll')) ('/r:'+(Join-Path $framework 'WPF\WindowsBase.dll')) ('/r:'+(Join-Path $framework 'WPF\PresentationFramework.dll')) ('/r:'+(Join-Path $framework 'WPF\PresentationCore.dll')) /r:System.Drawing.dll /r:System.Windows.Forms.dll $source
if($LASTEXITCODE -ne 0){throw 'Compilation failed.'}
Copy-Item -LiteralPath (Join-Path $runtime 'MediaPortal.exe.config') -Destination ($output+'.config') -Force
$report=Join-Path $PSScriptRoot 'Native-Tests.txt'
$process=Start-Process -FilePath $output -ArgumentList ('"'+$report+'"') -WorkingDirectory $runtime -WindowStyle Hidden -PassThru -Wait
Get-Content -LiteralPath $report
if($process.ExitCode -ne 0){throw 'Native grid validation failed.'}
