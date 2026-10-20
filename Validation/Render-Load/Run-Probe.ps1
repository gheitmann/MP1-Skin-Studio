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
$probeHashes=@{}
Get-ChildItem -LiteralPath $SkinDirectory -Filter '*.xml' -Recurse -File | ForEach-Object {$probeHashes[$_.FullName]=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
$probeCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $probeCompiler /nologo /target:exe /optimize+ /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ('/r:'+(Join-Path $probeOutput 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $probeOutput 'Probe.exe')) (Join-Path $PSScriptRoot 'Probe.cs')
if($LASTEXITCODE -ne 0){throw 'Render/load probe compilation failed.'}
$probeProcess=Start-Process -FilePath (Join-Path $probeOutput 'Probe.exe') -ArgumentList @(('"'+(Resolve-Path -LiteralPath $SkinDirectory).Path+'"'),('"'+$probeOutput+'"')) -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput (Join-Path $probeOutput 'Results.txt') -RedirectStandardError (Join-Path $probeOutput 'stderr.txt')
if($probeProcess.ExitCode -ne 0){throw ('Render/load probe failed; inspect '+$probeOutput)}
foreach($file in $probeHashes.Keys){if((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $probeHashes[$file]){throw ('Installed XML changed: '+$file)}}
Add-Content -LiteralPath (Join-Path $probeOutput 'Results.txt') -Value ('PASS '+$probeHashes.Count+' installed XML hashes unchanged.')
Get-Content -LiteralPath (Join-Path $probeOutput 'Results.txt')
