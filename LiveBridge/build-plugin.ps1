param([string]$MediaPortalDirectory='',[string]$OutputDirectory=(Join-Path $PSScriptRoot 'bin'))
$ErrorActionPreference='Stop'
if(-not $MediaPortalDirectory){
    foreach($candidate in @((Join-Path $env:ProgramFiles 'Team MediaPortal\MediaPortal'),(Join-Path ${env:ProgramFiles(x86)} 'Team MediaPortal\MediaPortal'))){if(Test-Path -LiteralPath (Join-Path $candidate 'Core.dll')){$MediaPortalDirectory=$candidate;break}}
}
if(-not(Test-Path -LiteralPath (Join-Path $MediaPortalDirectory 'Core.dll'))){throw 'Choose an installed MediaPortal 1 SDK directory containing Core.dll.'}
[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($OutputDirectory)) | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$files=@(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | Select-Object -ExpandProperty FullName)
$files+=Join-Path $PSScriptRoot '..\src\LiveProtocol.cs'
& $compiler /nologo /target:library /platform:anycpu /optimize+ /codepage:65001 ('/out:'+(Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) 'MP1SkinStudio.LiveBridge.dll')) /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll ('/r:'+(Join-Path $MediaPortalDirectory 'Core.dll')) ('/r:'+(Join-Path $MediaPortalDirectory 'Utils.dll')) ('/r:'+(Join-Path $MediaPortalDirectory 'Common.Utils.dll')) $files
if($LASTEXITCODE -ne 0){throw 'Live bridge compilation failed.'}
Write-Output 'Built MP1SkinStudio.LiveBridge.dll against the installed MediaPortal SDK.'
