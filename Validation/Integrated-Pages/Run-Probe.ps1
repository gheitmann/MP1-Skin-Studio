param(
    [Parameter(Mandatory=$true)][string]$PackageDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [Parameter(Mandatory=$true)][string]$SkinDirectory
)
$ErrorActionPreference='Stop'
$reviewPackage=(Resolve-Path -LiteralPath $PackageDirectory).Path
$reviewOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $reviewOutput){throw 'Choose a fresh output directory.'}
[IO.Directory]::CreateDirectory($reviewOutput) | Out-Null
Copy-Item -LiteralPath (Join-Path $reviewPackage 'MP1SkinStudio.exe') -Destination $reviewOutput
# Existing menu integration resolves DemoRoot beside the host executable.
Copy-Item -LiteralPath (Join-Path $reviewPackage 'Samples') -Destination $reviewOutput -Recurse
$reviewCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $reviewCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/r:'+(Join-Path $reviewOutput 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $reviewOutput 'PageProbe.exe')) (Join-Path $PSScriptRoot 'PageProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Integrated page probe compilation failed.'}
$reviewProcess=Start-Process -FilePath (Join-Path $reviewOutput 'PageProbe.exe') -ArgumentList @(('"'+(Join-Path $reviewPackage 'Samples\Titan-Sample')+'"'),('"'+(Resolve-Path -LiteralPath $SkinDirectory).Path+'"'),('"'+(Join-Path $reviewOutput 'Workflows')+'"')) -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput (Join-Path $reviewOutput 'stdout.txt') -RedirectStandardError (Join-Path $reviewOutput 'stderr.txt')
if($reviewProcess.ExitCode -ne 0){throw ('Integrated page checks failed; inspect '+(Join-Path $reviewOutput 'stderr.txt'))}
Get-Content -LiteralPath (Join-Path $reviewOutput 'Workflows\Results.txt')
