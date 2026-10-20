param(
    [Parameter(Mandatory=$true)][string]$PackageDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [Parameter(Mandatory=$true)][string]$SkinDirectory
)
$ErrorActionPreference='Stop'
$probePackage=(Resolve-Path -LiteralPath $PackageDirectory).Path
$probeOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $probeOutput){throw 'Choose a fresh probe directory.'}
[IO.Directory]::CreateDirectory($probeOutput) | Out-Null
Copy-Item -LiteralPath (Join-Path $probePackage 'MP1SkinStudio.exe') -Destination $probeOutput
$probeBefore=@{}
Get-ChildItem -LiteralPath $SkinDirectory -Filter '*.xml' -Recurse -File | ForEach-Object {$probeBefore[$_.FullName]=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
$probeCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $probeCompiler /nologo /target:exe /optimize+ /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ('/r:'+(Join-Path $probeOutput 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $probeOutput 'PerformanceProbe.exe')) (Join-Path $PSScriptRoot 'PerformanceProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Performance probe compilation failed.'}
$probeProcess=Start-Process -FilePath (Join-Path $probeOutput 'PerformanceProbe.exe') -ArgumentList ('"'+(Resolve-Path -LiteralPath $SkinDirectory).Path+'"') -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput (Join-Path $probeOutput 'Results.txt') -RedirectStandardError (Join-Path $probeOutput 'stderr.txt')
if($probeProcess.ExitCode -ne 0){throw ('Performance probe failed; inspect '+$probeOutput)}
foreach($probeFile in $probeBefore.Keys){if((Get-FileHash -LiteralPath $probeFile -Algorithm SHA256).Hash -ne $probeBefore[$probeFile]){throw ('Installed XML changed: '+$probeFile)}}
Add-Content -LiteralPath (Join-Path $probeOutput 'Results.txt') -Value ('PASS '+$probeBefore.Count+' installed XML hashes unchanged.')
Get-Content -LiteralPath (Join-Path $probeOutput 'Results.txt')
