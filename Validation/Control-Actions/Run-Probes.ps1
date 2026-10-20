param([Parameter(Mandatory=$true)][string]$SkinDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$probeRepository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not $OutputDirectory){$OutputDirectory=Join-Path $probeRepository 'work/Control-Actions'}
$probeOutput=[IO.Path]::GetFullPath($OutputDirectory)
$probeSkin=(Resolve-Path -LiteralPath $SkinDirectory).Path
[IO.Directory]::CreateDirectory($probeOutput) | Out-Null
$probeEditor=Join-Path $probeOutput 'MP1SkinStudio.exe'
Copy-Item -LiteralPath (Join-Path $probeRepository 'MP1SkinStudio.exe') -Destination $probeEditor -Force
$probeCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
foreach($probeName in @('TitanProbe','FormProbe')){
 & $probeCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/r:'+$probeEditor) ('/out:'+(Join-Path $probeOutput ($probeName+'.exe'))) (Join-Path $PSScriptRoot ($probeName+'.cs'))
 if($LASTEXITCODE -ne 0){throw ('Compilation failed: '+$probeName)}
}
& (Join-Path $probeOutput 'TitanProbe.exe') $probeSkin (Join-Path $probeOutput 'Titan')
if($LASTEXITCODE -ne 0){throw 'Titan control-action workflows failed.'}
$probeForm=Join-Path $probeOutput 'Form'
[IO.Directory]::CreateDirectory($probeForm) | Out-Null
$probeProcess=Start-Process -FilePath (Join-Path $probeOutput 'FormProbe.exe') -ArgumentList ('"'+$probeForm+'"') -WorkingDirectory $probeOutput -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $probeForm 'stdout.txt') -RedirectStandardError (Join-Path $probeForm 'stderr.txt')
if($probeProcess.ExitCode -ne 0){throw ('Form event test failed; see '+(Join-Path $probeForm 'stderr.txt'))}
Get-Content -LiteralPath (Join-Path $probeForm 'Form-Results.txt')
