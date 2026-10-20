param([Parameter(Mandatory=$true)][string]$PackageDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$copyPackage=(Resolve-Path -LiteralPath $PackageDirectory).Path
$copyOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $copyOutput){throw 'Choose a fresh output directory.'}
New-Item -ItemType Directory -Path $copyOutput | Out-Null
Copy-Item -LiteralPath (Join-Path $copyPackage 'MP1SkinStudio.exe') -Destination $copyOutput
Copy-Item -LiteralPath (Join-Path $copyPackage 'Samples') -Destination $copyOutput -Recurse
$copyCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $copyCompiler /nologo /target:exe /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ('/r:'+(Join-Path $copyOutput 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $copyOutput 'Probe.exe')) (Join-Path $PSScriptRoot 'Probe.cs')
if($LASTEXITCODE -ne 0){throw 'Probe compilation failed.'}
$copyProcess=Start-Process -FilePath (Join-Path $copyOutput 'Probe.exe') -ArgumentList @(('"'+(Join-Path $copyOutput 'Samples/Titan-Sample')+'"'),('"'+(Join-Path $copyOutput 'Workflows')+'"')) -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $copyOutput 'stdout.txt') -RedirectStandardError (Join-Path $copyOutput 'stderr.txt')
if($copyProcess.ExitCode -ne 0){throw ('Copy probe failed; inspect '+$copyOutput)}
Get-Content -LiteralPath (Join-Path $copyOutput 'Workflows/Results.txt')
