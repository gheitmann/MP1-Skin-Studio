param([Parameter(Mandatory=$true)][string]$ClientDirectory,[Parameter(Mandatory=$true)][string]$SkinDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$client=(Resolve-Path -LiteralPath $ClientDirectory).Path
$files=@((Join-Path $PSScriptRoot 'NativeProbe.cs'))
foreach($name in @('RuntimeProperty.cs','RuntimeSession.cs','SessionContract.cs')){$files+=Join-Path $root ('LiveBridge\'+$name)}
$files+=Join-Path $root 'src\LiveProtocol.cs'
& ($env:WINDIR+'\Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo ('/out:'+(Join-Path $client 'NativeProbe.exe')) /r:System.dll /r:System.Core.dll /r:System.Xml.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ('/r:'+(Join-Path $client 'Core.dll')) ('/r:'+(Join-Path $client 'Utils.dll')) ('/r:'+(Join-Path $client 'Common.Utils.dll')) $files
if($LASTEXITCODE -ne 0){throw 'Native probe compilation failed.'}
& (Join-Path $client 'NativeProbe.exe') (Resolve-Path -LiteralPath $SkinDirectory).Path
if($LASTEXITCODE -ne 0){throw 'Native probe failed.'}
