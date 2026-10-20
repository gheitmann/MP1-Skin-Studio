param(
    [Parameter(Mandatory=$true)][string]$SkinDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$findingRepository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$findingOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $findingOutput){throw 'Choose a fresh output directory.'}
[IO.Directory]::CreateDirectory($findingOutput) | Out-Null
$findingEditor=Join-Path $findingOutput 'MP1SkinStudio.exe'
Copy-Item -LiteralPath (Join-Path $findingRepository 'MP1SkinStudio.exe') -Destination $findingEditor
$findingCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $findingCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/r:'+$findingEditor) ('/out:'+(Join-Path $findingOutput 'ReportProbe.exe')) (Join-Path $PSScriptRoot 'ReportProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Report probe compilation failed.'}
$findingSkin=(Resolve-Path -LiteralPath $SkinDirectory).Path
$findingProcess=Start-Process -FilePath (Join-Path $findingOutput 'ReportProbe.exe') -ArgumentList @(('"'+$findingSkin+'"'),('"'+(Join-Path $findingOutput 'Reports')+'"')) -WorkingDirectory $findingOutput -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $findingOutput 'stdout.txt') -RedirectStandardError (Join-Path $findingOutput 'stderr.txt')
if($findingProcess.ExitCode -ne 0){throw ('Report workflows failed; inspect '+(Join-Path $findingOutput 'stderr.txt'))}
Get-Content -LiteralPath (Join-Path $findingOutput 'Reports/Results.txt')
