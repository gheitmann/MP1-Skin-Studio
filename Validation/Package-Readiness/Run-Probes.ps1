param(
    [Parameter(Mandatory=$true)][string]$PackageZip,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [switch]$SkipFullUi,
    [switch]$IncludeCore
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$reviewPackage=(Resolve-Path -LiteralPath $PackageZip).Path
$reviewOutput=[IO.Path]::GetFullPath($OutputDirectory)
$reviewBin=Join-Path $reviewOutput 'package'
if(Test-Path -LiteralPath $reviewBin){throw 'Choose a fresh output directory; the extracted package is preserved.'}
[IO.Directory]::CreateDirectory($reviewOutput) | Out-Null
[IO.Compression.ZipFile]::ExtractToDirectory($reviewPackage,$reviewBin)
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
Get-Content -LiteralPath (Join-Path $reviewOutput 'ReadinessProbe/Results.txt')
if(-not $SkipFullUi){Get-Content -LiteralPath (Join-Path $reviewOutput 'UiHost/ui-test-results.txt') | Select-Object -Last 3}
Write-Output ('Completed package acceptance. Full UI: '+(-not $SkipFullUi)+'; core: '+[bool]$IncludeCore)
