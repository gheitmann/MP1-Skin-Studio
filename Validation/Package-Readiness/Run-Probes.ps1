param(
    [Parameter(Mandatory=$true)][string]$PackageZip,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [switch]$SkipFullUi,
    [switch]$IncludeCore,
    [switch]$IncludeStartup,
    [switch]$IncludeTransitions,
    [switch]$IncludeChanges
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$reviewPackage=(Resolve-Path -LiteralPath $PackageZip).Path
$reviewOutput=[IO.Path]::GetFullPath($OutputDirectory)
$reviewBin=Join-Path $reviewOutput 'package'
if(Test-Path -LiteralPath $reviewBin){throw 'Choose a fresh output directory; the extracted package is preserved.'}
[IO.Directory]::CreateDirectory($reviewOutput) | Out-Null
[IO.Compression.ZipFile]::ExtractToDirectory($reviewPackage,$reviewBin)
$reviewManifest=@(Get-Content -LiteralPath (Join-Path $reviewBin 'MANIFEST.sha256'))
$reviewCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$reviewHosts=@('MatrixProbe','ReadinessProbe')
if(-not $SkipFullUi){$reviewHosts+='UiHost'}
foreach($reviewProbe in $reviewHosts){
    & $reviewCompiler /nologo /target:exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/r:'+(Join-Path $reviewBin 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $reviewBin ($reviewProbe+'.exe'))) (Join-Path $PSScriptRoot ($reviewProbe+'.cs'))
    if($LASTEXITCODE -ne 0){throw ('Compilation failed: '+$reviewProbe)}
}
& (Join-Path $reviewBin 'MatrixProbe.exe') $reviewBin (Join-Path $reviewOutput 'Matrix')
if($LASTEXITCODE -ne 0){throw 'Package matrix failed.'}
$reviewFormHosts=@('ReadinessProbe')
if(-not $SkipFullUi){$reviewFormHosts+='UiHost'}
foreach($reviewProbe in $reviewFormHosts){
    $reviewReport=Join-Path $reviewOutput $reviewProbe
    [IO.Directory]::CreateDirectory($reviewReport) | Out-Null
    $reviewProcess=Start-Process -FilePath (Join-Path $reviewBin ($reviewProbe+'.exe')) -ArgumentList @(('"'+(Join-Path $reviewBin 'Samples/Titan-Sample')+'"'),('"'+$reviewReport+'"')) -WorkingDirectory $reviewBin -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $reviewReport 'stdout.txt') -RedirectStandardError (Join-Path $reviewReport 'stderr.txt')
    if($reviewProcess.ExitCode -ne 0){throw ($reviewProbe+' failed; inspect its local report directory: '+$reviewReport)}
}
if($IncludeCore){
    $reviewCore=Join-Path $reviewOutput 'Core'
    $reviewCoreProcess=Start-Process -FilePath (Join-Path $reviewBin 'MP1SkinStudio.exe') -ArgumentList @('--selftest',('"'+$reviewCore+'"')) -WorkingDirectory $reviewBin -WindowStyle Hidden -Wait -PassThru
    if($reviewCoreProcess.ExitCode -ne 0){throw ('Core checks failed; inspect '+$reviewCore)}
}
if($IncludeStartup){
    & (Join-Path $PSScriptRoot '../Startup-Acceptance/Run-Probe.ps1') -PackageDirectory $reviewBin -OutputDirectory (Join-Path $reviewOutput 'StartupProbe')
}
if($IncludeTransitions){
    & (Join-Path $PSScriptRoot '../Session-Transitions/Run-Probe.ps1') -PackageDirectory $reviewBin -OutputDirectory (Join-Path $reviewOutput 'TransitionProbe')
}
if($IncludeChanges){
    & (Join-Path $PSScriptRoot '../Change-Review/Run-Probe.ps1') -PackageDirectory $reviewBin -OutputDirectory (Join-Path $reviewOutput 'ChangeReviewProbe')
}
$reviewVerified=0
foreach($reviewLine in $reviewManifest){
    if($reviewLine -notmatch '^([a-f0-9]{64})  (.+)$'){throw 'Invalid package manifest record.'}
    $reviewExpected=$Matches[1];$reviewRelative=$Matches[2]
    $reviewActual=(Get-FileHash -LiteralPath (Join-Path $reviewBin $reviewRelative) -Algorithm SHA256).Hash.ToLowerInvariant()
    if($reviewActual -cne $reviewExpected){throw ('Package payload changed during acceptance: '+$reviewRelative)}
    $reviewVerified++
}
$reviewPreservation='PASS all '+$reviewVerified+' manifest payload hashes unchanged after the complete acceptance run.'
Set-Content -LiteralPath (Join-Path $reviewOutput 'Payload-Results.txt') -Value $reviewPreservation -Encoding utf8
Write-Output $reviewPreservation
Get-Content -LiteralPath (Join-Path $reviewOutput 'ReadinessProbe/Results.txt')
if(-not $SkipFullUi){Get-Content -LiteralPath (Join-Path $reviewOutput 'UiHost/ui-test-results.txt') | Select-Object -Last 3}
Write-Output ('Completed package acceptance. Full UI: '+(-not $SkipFullUi)+'; core: '+[bool]$IncludeCore+'; startup: '+[bool]$IncludeStartup+'; transitions: '+[bool]$IncludeTransitions+'; changes: '+[bool]$IncludeChanges)
