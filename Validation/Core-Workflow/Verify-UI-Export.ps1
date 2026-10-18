param([Parameter(Mandatory=$true)][string]$ProjectPath,
      [Parameter(Mandatory=$true)][string]$ExportPath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Read-ZipText($Archive, $Name) {
    $entry = $Archive.GetEntry($Name)
    if ($null -eq $entry) { throw "Missing ZIP entry: $Name" }
    $reader = New-Object System.IO.StreamReader($entry.Open())
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
$project = [System.IO.Compression.ZipFile]::OpenRead($ProjectPath)
$export = [System.IO.Compression.ZipFile]::OpenRead($ExportPath)
try {
    [xml]$manifest = Read-ZipText $project 'project.xml'
    $files = @($manifest.skinStudioProject.files.file)
    if ($files.Count -ne 1 -or $files[0].path -ne 'settings.xml') { throw 'Unexpected modified file set' }
    'PASS: saved UI project modifies only settings.xml'
    $saved = Read-ZipText $project $files[0].edited
    $exported = Read-ZipText $export 'settings.xml'
    if ($saved -cne $exported) { throw 'Export differs from saved snapshot' }
    'PASS: UI ZIP export equals saved snapshot exactly as decoded XML text'
    [xml]$xml = $exported
    $heading = @($xml.SelectNodes('//control[label="EDITOR TEST"]'))
    if ($heading.Count -ne 1 -or $heading[0].posX -ne '192' -or $heading[0].posY -ne '235' -or $heading[0].width -ne '440') { throw 'UI heading changes missing' }
    'PASS: EDITOR TEST heading retains X=192, Y=235, width=440'
    if ($export.Entries.Count -ne 2) { throw 'Unexpected ZIP entry count' }
    'PASS: export has two entries: settings.xml and installation instructions'
    $original = Join-Path $manifest.skinStudioProject.root 'settings.xml'
    $digest = (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($digest -ne '78b2b21deff4355a588cd0830865b8d3aab2d9185d6cc9fde58373aaf7e5bf31') { throw 'Original settings.xml changed' }
    'PASS: installed original settings.xml SHA256 unchanged'
    "Original SHA256: $digest"
    'ZIP entries: ' + (($export.Entries | ForEach-Object FullName) -join ', ')
} finally { $project.Dispose(); $export.Dispose() }
