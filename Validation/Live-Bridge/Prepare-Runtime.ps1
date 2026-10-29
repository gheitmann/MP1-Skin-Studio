param([string]$MediaPortalDirectory='C:\Program Files\Team MediaPortal\MediaPortal',[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$runtime=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $runtime){throw 'Choose a fresh isolated runtime directory.'}
$client=Join-Path $runtime 'client';$data=Join-Path $runtime 'data';$skins=Join-Path $data 'skin';$skin=Join-Path $skins 'SkinStudio-Live-Test'
foreach($folder in @($client,$data,$skins,(Join-Path $client 'plugins\process'),(Join-Path $client 'plugins\Windows'),(Join-Path $client 'plugins\ExternalPlayers'),(Join-Path $client 'musicplayer\plugins\audio decoders'))){[IO.Directory]::CreateDirectory($folder) | Out-Null}
Get-ChildItem -LiteralPath $MediaPortalDirectory -File | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $client}
foreach($name in @('Shaders','defaults','runtimes')){if(Test-Path -LiteralPath (Join-Path $MediaPortalDirectory $name)){Copy-Item -LiteralPath (Join-Path $MediaPortalDirectory $name) -Destination $client -Recurse}}
$standard=@('Common.GUIPlugins.dll','Dialogs.dll','GUIHome.dll','GUINotifier.dll','GUISettings.dll','GUITopbar.dll','GUIMusic.dll','GUIPictures.dll','GUIVideos.dll')
Get-ChildItem -LiteralPath (Join-Path $MediaPortalDirectory 'plugins\Windows') -File | Where-Object {$_.Name -in $standard} | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $client 'plugins\Windows')}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\..\LiveBridge\bin\MP1SkinStudio.LiveBridge.dll') -Destination (Join-Path $client 'plugins\process')
Copy-Item -LiteralPath 'C:\ProgramData\Team MediaPortal\MediaPortal\skin\DefaultWideHD' -Destination $skin -Recurse
$language=Join-Path $data 'language';Copy-Item -LiteralPath 'C:\ProgramData\Team MediaPortal\MediaPortal\language' -Destination $language -Recurse
$dirs=New-Object Xml.XmlDocument;$dirs.LoadXml('<Config />')
foreach($name in @('Config','Plugins','Log','Skin','Language','Database','Thumbs','Cache')){
 $path=switch($name){'Config'{$data};'Plugins'{Join-Path $client 'plugins'};'Skin'{$skins};default{Join-Path $data $name.ToLowerInvariant()}}
 [IO.Directory]::CreateDirectory($path) | Out-Null
 $entry=$dirs.CreateElement('Dir');$entry.SetAttribute('id',$name);$child=$dirs.CreateElement('Path');$child.InnerText=$path.TrimEnd('\')+'\';$entry.AppendChild($child) | Out-Null;$dirs.DocumentElement.AppendChild($entry) | Out-Null
}
$dirs.Save((Join-Path $client 'MediaPortalDirs.xml'))
$settings=New-Object Xml.XmlDocument;$settings.LoadXml('<profile />')
function Set-LiveSetting([string]$section,[string]$key,[string]$value){$s=$settings.SelectSingleNode('/profile/section[@name="'+$section+'"]');if(-not $s){$s=$settings.CreateElement('section');$s.SetAttribute('name',$section);$settings.DocumentElement.AppendChild($s) | Out-Null};$e=$settings.CreateElement('entry');$e.SetAttribute('name',$key);$e.InnerText=$value;$s.AppendChild($e) | Out-Null}
Set-LiveSetting 'skin' 'name' 'SkinStudio-Live-Test';Set-LiveSetting 'skin' 'theme' 'default';Set-LiveSetting 'skin' 'language' 'English';Set-LiveSetting 'skin' 'obsoleteWarning' 'no'
Set-LiveSetting 'general' 'startbasichome' 'no';Set-LiveSetting 'general' 'startfullscreen' 'no';Set-LiveSetting 'general' 'startscreen' '0';Set-LiveSetting 'general' 'screenselector' 'no';Set-LiveSetting 'general' 'autostart' 'no';Set-LiveSetting 'general' 'loglevel' '3'
Set-LiveSetting 'plugins' 'MP1 Skin Studio Live' 'yes';Set-LiveSetting 'plugins' 'MP1SkinStudio.LiveBridge.dll' 'yes'
foreach($plugin in Get-ChildItem -LiteralPath (Join-Path $client 'plugins\Windows') -Filter '*.dll'){
 $enabled=if($plugin.Name -in @('GUIHome.dll','GUISettings.dll','GUINotifier.dll','GUIMusic.dll','GUIVideoFiles.dll','GUIPictures.dll')){'yes'}else{'no'}
 Set-LiveSetting 'plugins' $plugin.Name $enabled
}
# MP checks for a minimum 10 KB configuration before starting its GUI.
$settings.DocumentElement.AppendChild($settings.CreateComment((' Isolated Skin Studio test profile. ' * 400))) | Out-Null
$settings.Save((Join-Path $data 'MediaPortal.xml'))
$page=Join-Path $skin 'myHome.xml';$text=[IO.File]::ReadAllText($page);$at=$text.LastIndexOf('</controls>');if($at -lt 0){throw 'Test page lacks controls.'}
$testControls=@'
<control><description>Studio live test text</description><type>label</type><id>21001</id><posX>100</posX><posY>110</posY><width>900</width><height>70</height><font>font18</font><label>Live bridge: original text</label><textcolor>ffffffff</textcolor></control>
<control><description>Studio live test button</description><type>button</type><id>21002</id><posX>100</posX><posY>200</posY><width>520</width><height>85</height><font>font18</font><label>Original button</label><textcolor>ffffffff</textcolor></control>
<control><description>Studio duplicate zero A</description><type>label</type><id>0</id><posX>100</posX><posY>310</posY><width>500</width><height>50</height><font>font18</font><label>Zero A</label></control>
<control><description>Studio duplicate zero B</description><type>label</type><id>0</id><posX>100</posX><posY>370</posY><width>500</width><height>50</height><font>font18</font><label>Zero B</label></control>
'@
[IO.File]::WriteAllText($page,$text.Substring(0,$at)+$testControls+$text.Substring($at),(New-Object Text.UTF8Encoding($false)))
[IO.Directory]::CreateDirectory((Join-Path $runtime 'ipc')) | Out-Null
[ordered]@{Client=$client;Data=$data;Skin=$skin;Ipc=(Join-Path $runtime 'ipc')} | ConvertTo-Json | Set-Content (Join-Path $runtime 'Runtime.json')
Write-Output ('Prepared isolated MediaPortal runtime: '+$runtime)
