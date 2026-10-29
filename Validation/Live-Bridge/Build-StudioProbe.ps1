param([Parameter(Mandatory=$true)][string]$ClientDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'));$client=(Resolve-Path -LiteralPath $ClientDirectory).Path
Copy-Item -LiteralPath (Join-Path $root 'MP1SkinStudio.exe') -Destination $client
Copy-Item -LiteralPath (Join-Path $root 'LiveBridge\bin\MP1SkinStudio.LiveBridge.dll') -Destination $client
& ($env:WINDIR+'\Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo ('/out:'+(Join-Path $client 'StudioProbe.exe')) /r:System.dll /r:System.Core.dll /r:System.Xml.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ('/r:'+(Join-Path $client 'Core.dll')) ('/r:'+(Join-Path $client 'Utils.dll')) ('/r:'+(Join-Path $client 'Common.Utils.dll')) ('/r:'+(Join-Path $client 'MP1SkinStudio.exe')) ('/r:'+(Join-Path $client 'MP1SkinStudio.LiveBridge.dll')) (Join-Path $PSScriptRoot 'StudioProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Studio live probe compilation failed.'}
