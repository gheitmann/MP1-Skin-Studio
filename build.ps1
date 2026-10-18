$ErrorActionPreference = 'Stop'
$studioRoot = $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw '.NET Framework 4.x C# compiler was not found.' }
$sourcePaths = Get-ChildItem -LiteralPath (Join-Path $studioRoot 'src') -Filter '*.cs' | Select-Object -ExpandProperty FullName
$exePath = Join-Path $studioRoot 'MP1SkinStudio.exe'
& $compilerPath /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 "/out:$exePath" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll $sourcePaths
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output 'Built: MP1SkinStudio.exe'
