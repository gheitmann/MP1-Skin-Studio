param([Parameter(Mandatory=$true)][string]$ClientDirectory,[Parameter(Mandatory=$true)][string]$SkinDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'));$client=(Resolve-Path -LiteralPath $ClientDirectory).Path;$skin=(Resolve-Path -LiteralPath $SkinDirectory).Path
foreach($target in @($client,$skin)){if(-not $target.StartsWith((Join-Path $root 'work\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Use an isolated MP client and skin beneath this repository work directory.'}}
foreach($process in @(Get-Process MediaPortal -ErrorAction SilentlyContinue)){if($process.Path -and $process.Path.Equals((Join-Path $client 'MediaPortal.exe'),[StringComparison]::OrdinalIgnoreCase)){throw 'Close the isolated MP before preparing its fixture.'}}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Scene.xml') -Destination (Join-Path $skin 'studio.live.special.xml')
Copy-Item -LiteralPath (Join-Path $root 'MP1SkinStudio.exe') -Destination $client
$dll=Join-Path $root 'LiveBridge\bin\MP1SkinStudio.LiveBridge.dll';Copy-Item -LiteralPath $dll -Destination $client;Copy-Item -LiteralPath $dll -Destination (Join-Path $client 'plugins\process')
$compiler=$env:WINDIR+'\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$basic=@('/nologo','/r:System.dll','/r:System.Core.dll','/r:System.Xml.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll')
$sdk=@(('/r:'+(Join-Path $client 'Core.dll')),('/r:'+(Join-Path $client 'Utils.dll')),('/r:'+(Join-Path $client 'Common.Utils.dll')))
& $compiler @basic @sdk /target:library ('/out:'+(Join-Path $client 'plugins\process\SkinStudio.SpecialFixture.dll')) (Join-Path $PSScriptRoot 'ScenePlugin.cs');if($LASTEXITCODE -ne 0){throw 'Scene plugin compilation failed.'}
& $compiler @basic @sdk ('/r:'+(Join-Path $client 'MP1SkinStudio.LiveBridge.dll')) ('/out:'+(Join-Path $client 'SpecialNativeProbe.exe')) (Join-Path $PSScriptRoot 'NativeProbe.cs');if($LASTEXITCODE -ne 0){throw 'Native probe compilation failed.'}
& $compiler @basic ('/out:'+(Join-Path $client 'SpecialSceneProbe.exe')) (Join-Path $PSScriptRoot 'SceneProbe.cs') (Join-Path $root 'src\LiveProtocol.cs');if($LASTEXITCODE -ne 0){throw 'Scene probe compilation failed.'}
& $compiler @basic ('/r:'+(Join-Path $client 'MP1SkinStudio.exe')) ('/out:'+(Join-Path $client 'SpecialStudioProbe.exe')) (Join-Path $PSScriptRoot 'StudioProbe.cs');if($LASTEXITCODE -ne 0){throw 'Studio probe compilation failed.'}
Write-Output 'Isolated fixture prepared; no owner installation was modified.'
