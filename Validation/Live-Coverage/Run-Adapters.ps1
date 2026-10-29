param([Parameter(Mandatory=$true)][string]$ClientDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'));$client=(Resolve-Path -LiteralPath $ClientDirectory).Path;$out=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $out){throw 'Choose a fresh output directory.'}
Copy-Item -LiteralPath (Join-Path $root 'LiveBridge\bin\MP1SkinStudio.LiveBridge.dll') -Destination $client
& ($env:WINDIR+'\Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /r:System.dll /r:System.Core.dll /r:System.Xml.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ('/r:'+(Join-Path $client 'Core.dll')) ('/r:'+(Join-Path $client 'Utils.dll')) ('/r:'+(Join-Path $client 'Common.Utils.dll')) ('/r:'+(Join-Path $client 'MP1SkinStudio.LiveBridge.dll')) ('/out:'+(Join-Path $client 'AdapterProbe.exe')) (Join-Path $PSScriptRoot 'AdapterProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Adapter probe compilation failed.'}
& (Join-Path $client 'AdapterProbe.exe') $out
if($LASTEXITCODE -ne 0){throw 'Extended adapter checks failed.'}
