param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'work\Review-Build'),
    [string]$Revision = 'working-tree',
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (-not $SkipBuild) { & (Join-Path $PSScriptRoot 'build.ps1') }
$reviewOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($reviewOutput) | Out-Null
$reviewTarget = Join-Path $reviewOutput 'MP1-Skin-Studio-Review.zip'
$reviewTemp = $reviewTarget + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
$reviewFiles = @('MP1SkinStudio.exe', 'LICENSE', 'docs/USER_GUIDE.md')
$reviewSampleRoot = Join-Path $PSScriptRoot 'Samples\Titan-Sample'
$reviewFiles += @(Get-ChildItem -LiteralPath $reviewSampleRoot -Recurse -File |
    Where-Object { $_.Extension -in @('.xml', '.png', '.jpg', '.jpeg') } |
    ForEach-Object { $_.FullName.Substring($PSScriptRoot.Length + 1).Replace('\', '/') })
$reviewHashes = [ordered]@{}
foreach ($reviewFile in $reviewFiles) {
    $reviewHashes[$reviewFile] = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $reviewFile) -Algorithm SHA256).Hash.ToLowerInvariant()
}
$reviewNotes = @"
MP1 Skin Studio - manual review build
Source revision: $Revision

Extract this complete ZIP to a separate directory. To test manually, run
MP1SkinStudio.exe from that directory. It opens the included demo skin.
Requires Windows and .NET Framework 4.x. Packaging does not launch the editor.
Try demo.filmstrip.xml from PAGE / XML, enable Navigation and use Left/Right.
Its upper image follows the selected thumbnail; no installed skin is required.

This package contains the editor, repository demo XML/images, license and guide.
It contains no installed Titan skin, MediaPortal executable, client configuration,
saved projects, desktop captures or private runtime logs.

Core workflows: select a demo element, change properties and Apply Properties;
use Revert Inputs to discard unapplied fields; test Undo/Redo; save an editor
project, reopen it and export changes as ZIP. Use separate skin copies for real
editing and retain original files. The preview remains approximate.

Fresh acceptance covers attended Windows PIN Unlocked/protected fields/arrow
navigation/save/reopen, eight bundled demo page loads, and the self-contained
filmstrip save/export/undo workflow. All 22 bundled source/assets stayed unchanged
by that workflow. The engine retains its preceding 322-core-check/six-Titan-
workflow validation; those unchanged suites were not repeated.
No new installed MediaPortal client run was performed. See the user guide for
supported preview navigation and remaining rendering limitations.
"@
function Add-ReviewText($Archive, $Name, $Text) {
    $reviewEntry = $Archive.CreateEntry($Name)
    $reviewWriter = New-Object System.IO.StreamWriter($reviewEntry.Open(), (New-Object System.Text.UTF8Encoding($false)))
    try { $reviewWriter.Write($Text) } finally { $reviewWriter.Dispose() }
}
try {
    $reviewArchive = [System.IO.Compression.ZipFile]::Open($reviewTemp, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($reviewFile in $reviewFiles) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($reviewArchive, (Join-Path $PSScriptRoot $reviewFile), $reviewFile) | Out-Null
        }
        Add-ReviewText $reviewArchive 'README-Review.txt' $reviewNotes
        Add-ReviewText $reviewArchive 'MANIFEST.sha256' (($reviewHashes.GetEnumerator() | ForEach-Object { $_.Value + '  ' + $_.Key }) -join "`n")
    } finally { $reviewArchive.Dispose() }
    $reviewArchive = [System.IO.Compression.ZipFile]::OpenRead($reviewTemp)
    try {
        if ($reviewArchive.Entries.Count -ne $reviewFiles.Count + 2) { throw 'Unexpected review archive entries.' }
        foreach ($reviewFile in $reviewFiles) {
            $reviewInput = $reviewArchive.GetEntry($reviewFile).Open()
            $reviewHasher = [System.Security.Cryptography.SHA256]::Create()
            try { $reviewDigest = [BitConverter]::ToString($reviewHasher.ComputeHash($reviewInput)).Replace('-', '').ToLowerInvariant() }
            finally { $reviewInput.Dispose(); $reviewHasher.Dispose() }
            if ($reviewDigest -ne $reviewHashes[$reviewFile]) { throw "Review archive hash mismatch: $reviewFile" }
        }
    } finally { $reviewArchive.Dispose() }
    if ([System.IO.File]::Exists($reviewTarget)) { [System.IO.File]::Replace($reviewTemp, $reviewTarget, [System.Management.Automation.Language.NullString]::Value) }
    else { [System.IO.File]::Move($reviewTemp, $reviewTarget) }
    Write-Output "Verified review files: $($reviewFiles.Count) plus README and SHA256 manifest"
    Write-Output "Archive: $reviewTarget"
    Write-Output "SHA256: $((Get-FileHash -LiteralPath $reviewTarget -Algorithm SHA256).Hash.ToLowerInvariant())"
} finally {
    if ([System.IO.File]::Exists($reviewTemp)) { [System.IO.File]::Delete($reviewTemp) }
}



