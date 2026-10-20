param(
    [Parameter(Mandatory=$true)][string]$PackageDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$transitionPackage=(Resolve-Path -LiteralPath $PackageDirectory).Path
$transitionOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $transitionOutput){throw 'Choose a fresh output directory.'}
[IO.Directory]::CreateDirectory($transitionOutput) | Out-Null
Copy-Item -LiteralPath (Join-Path $transitionPackage 'MP1SkinStudio.exe') -Destination $transitionOutput
$transitionCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $transitionCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/r:'+(Join-Path $transitionOutput 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $transitionOutput 'TransitionProbe.exe')) (Join-Path $PSScriptRoot 'TransitionProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Session transition probe compilation failed.'}
$transitionProcess=Start-Process -FilePath (Join-Path $transitionOutput 'TransitionProbe.exe') -ArgumentList @(('"'+(Join-Path $transitionPackage 'Samples\Titan-Sample')+'"'),('"'+(Join-Path $transitionOutput 'Workflows')+'"')) -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput (Join-Path $transitionOutput 'stdout.txt') -RedirectStandardError (Join-Path $transitionOutput 'stderr.txt')
if($transitionProcess.ExitCode -ne 0){throw ('Session transition checks failed; inspect '+(Join-Path $transitionOutput 'stderr.txt'))}
Get-Content -LiteralPath (Join-Path $transitionOutput 'Workflows\Results.txt')
