param(
    [Parameter(Mandatory=$true)][string]$SkinDirectory,
    [Parameter(Mandatory=$true)][string]$MP1Core,
    [string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$probeRepository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not $OutputDirectory){$OutputDirectory=Join-Path $probeRepository 'work/Grid-Navigation'}
$probeOutput=[IO.Path]::GetFullPath($OutputDirectory)
$probeSkin=(Resolve-Path -LiteralPath $SkinDirectory).Path
$probeCore=(Resolve-Path -LiteralPath $MP1Core).Path
[IO.Directory]::CreateDirectory($probeOutput) | Out-Null
$probeEditor=Join-Path $probeOutput 'MP1SkinStudio.exe'
Copy-Item -LiteralPath (Join-Path $probeRepository 'MP1SkinStudio.exe') -Destination $probeEditor -Force
$probeCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
foreach($probeName in @('Probe','NativeMathProbe','FormProbe')){
    $probeExe=Join-Path $probeOutput ($probeName+'.exe')
    & $probeCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/r:'+$probeEditor) ('/out:'+$probeExe) (Join-Path $PSScriptRoot ($probeName+'.cs'))
    if($LASTEXITCODE -ne 0){throw ('Compilation failed: '+$probeName)}
}
& (Join-Path $probeOutput 'Probe.exe') $probeSkin $probeOutput
if($LASTEXITCODE -ne 0){throw 'Titan grid workflows failed.'}
& (Join-Path $probeOutput 'NativeMathProbe.exe') $probeCore (Join-Path $probeOutput 'Native-Results.txt')
if($LASTEXITCODE -ne 0){throw 'Original navigation math comparison failed.'}
$probeDemo=Join-Path $probeRepository 'Samples/Titan-Sample'
$probeForm=Start-Process -FilePath (Join-Path $probeOutput 'FormProbe.exe') -ArgumentList @(('"'+$probeDemo+'"'),('"'+$probeOutput+'"')) -WorkingDirectory $probeOutput -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $probeOutput 'Form-stdout.txt') -RedirectStandardError (Join-Path $probeOutput 'Form-stderr.txt')
if($probeForm.ExitCode -ne 0){throw ('Synthetic form test failed; see '+(Join-Path $probeOutput 'Form-stderr.txt'))}
Get-Content -LiteralPath (Join-Path $probeOutput 'Form-Results.txt')
