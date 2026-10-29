param([Parameter(Mandatory=$true)][string]$ClientDirectory,[Parameter(Mandatory=$true)][string]$SkinDirectory)
$ErrorActionPreference='Stop';$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'));$client=(Resolve-Path -LiteralPath $ClientDirectory).Path;$skin=(Resolve-Path -LiteralPath $SkinDirectory).Path
if(-not $client.StartsWith((Join-Path $root 'work\'),[StringComparison]::OrdinalIgnoreCase) -or -not $skin.StartsWith((Join-Path $root 'work\'),[StringComparison]::OrdinalIgnoreCase)){throw 'The coverage fixture is restricted to an isolated client/skin under this repository work directory.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Scene.xml') -Destination (Join-Path $skin 'studio.live.coverage.xml')
Copy-Item -LiteralPath (Join-Path $root 'LiveBridge\bin\MP1SkinStudio.LiveBridge.dll') -Destination (Join-Path $client 'plugins\process\MP1SkinStudio.LiveBridge.dll')
& ($env:WINDIR+'\Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:library /r:System.dll /r:System.Core.dll /r:System.Windows.Forms.dll ('/r:'+(Join-Path $client 'Core.dll')) ('/r:'+(Join-Path $client 'Utils.dll')) ('/r:'+(Join-Path $client 'Common.Utils.dll')) ('/out:'+(Join-Path $client 'plugins\process\SkinStudio.CoverageFixture.dll')) (Join-Path $PSScriptRoot 'ScenePlugin.cs')
if($LASTEXITCODE -ne 0){throw 'Coverage scene plugin compilation failed.'}
& ($env:WINDIR+'\Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:exe /r:System.dll /r:System.Core.dll /r:System.Xml.dll ('/out:'+(Join-Path $client 'SceneProbe.exe')) (Join-Path $PSScriptRoot 'SceneProbe.cs') (Join-Path $root 'src\LiveProtocol.cs')
if($LASTEXITCODE -ne 0){throw 'Coverage scene probe compilation failed.'}
