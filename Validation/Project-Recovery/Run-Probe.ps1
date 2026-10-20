param(
    [Parameter(Mandatory=$true)][string]$SkinDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$recoveryRepository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$recoveryOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $recoveryOutput){throw 'Choose a fresh output directory.'}
[IO.Directory]::CreateDirectory($recoveryOutput) | Out-Null
Copy-Item -LiteralPath (Join-Path $recoveryRepository 'MP1SkinStudio.exe') -Destination $recoveryOutput
$recoveryCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $recoveryCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/r:'+(Join-Path $recoveryOutput 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $recoveryOutput 'RecoveryProbe.exe')) (Join-Path $PSScriptRoot 'RecoveryProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Recovery probe compilation failed.'}
$recoveryArguments=@(('"'+(Resolve-Path -LiteralPath $SkinDirectory).Path+'"'),('"'+(Join-Path $recoveryOutput 'Workflows')+'"'),('"'+(Join-Path $recoveryRepository 'Samples/Titan-Sample')+'"'))
$recoveryProcess=Start-Process -FilePath (Join-Path $recoveryOutput 'RecoveryProbe.exe') -ArgumentList $recoveryArguments -WorkingDirectory $recoveryOutput -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $recoveryOutput 'stdout.txt') -RedirectStandardError (Join-Path $recoveryOutput 'stderr.txt')
if($recoveryProcess.ExitCode -ne 0){throw ('Recovery workflows failed; inspect '+(Join-Path $recoveryOutput 'stderr.txt'))}
Get-Content -LiteralPath (Join-Path $recoveryOutput 'Workflows/Results.txt')
