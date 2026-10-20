param(
    [Parameter(Mandatory=$true)][string]$PackageDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$startupPackage=(Resolve-Path -LiteralPath $PackageDirectory).Path
$startupOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $startupOutput){throw 'Choose a fresh output directory.'}
[IO.Directory]::CreateDirectory($startupOutput) | Out-Null
Copy-Item -LiteralPath (Join-Path $startupPackage 'MP1SkinStudio.exe') -Destination $startupOutput
$startupCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $startupCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/r:'+(Join-Path $startupOutput 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $startupOutput 'StartupProbe.exe')) (Join-Path $PSScriptRoot 'StartupProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Startup probe compilation failed.'}
$startupArguments=@(('"'+(Join-Path $startupPackage 'Samples\Titan-Sample')+'"'),('"'+(Join-Path $startupOutput 'Workflows')+'"'))
$startupProcess=Start-Process -FilePath (Join-Path $startupOutput 'StartupProbe.exe') -ArgumentList $startupArguments -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput (Join-Path $startupOutput 'stdout.txt') -RedirectStandardError (Join-Path $startupOutput 'stderr.txt')
if($startupProcess.ExitCode -ne 0){throw ('Startup workflows failed; inspect '+(Join-Path $startupOutput 'stderr.txt'))}
Get-Content -LiteralPath (Join-Path $startupOutput 'Workflows\Results.txt')
