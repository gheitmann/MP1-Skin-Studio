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
$reviewFiles = @('MP1SkinStudio.exe', 'LICENSE', 'docs/USER_GUIDE.md', 'docs/QUICK_START.md', 'docs/RELEASE_READINESS.md')
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
Start with docs/QUICK_START.md for a complete edit/save/reopen/export workflow
and a table of all twelve demos. No build step is needed for this ZIP.
Try demo.filmstrip.xml from PAGE / XML, enable Navigation and use Left/Right.
Its upper image follows the selected thumbnail; no installed skin is required.
Try demo.images.xml to compare proportional alignment, center crop and top crop.
Try demo.grid.xml with Navigation enabled to test automatic grid arrow routes.
Try demo.navigation.xml: Right, Down, Left, Up cross nested grid/stack branches.
Try demo.free.xml to navigate ordinary groups with absolute child positions.
Try demo.checkmark.xml with Up/Down and its selected/unselected property fields.

This package contains the editor, repository demo XML/images, license and guide.
It contains no installed Titan skin, MediaPortal executable, client configuration,
saved projects, desktop captures or private runtime logs.

Core workflows: select a demo element, change properties and Apply Properties;
use Revert Inputs to discard unapplied fields; test Undo/Redo; save an editor
project, reopen it and export changes as ZIP. Use separate skin copies for real
editing and retain original files. The preview remains approximate.

Review XML changes (Element menu / Ctrl+Shift+E) compares all modified session
files, including shared/theme/earlier-page XML. Read-only original/edited panes
show the first difference; Previous/Next reaches all large-file sections. Exact
byte hashes and refreshable source checks identify drift, missing or locked XML.
Pending fields and preview-only state are excluded. Close review to export.

SKIN above THEME discovers installed Windows skin folders, current siblings and
the bundled demo, refreshing on open. Existing session protections remain.
The left column is 30% wider, with expanded filters and matching dark scrollbars
on the main page/tree/properties/information regions. Preview tools wrap at
minimum width. Other locations remain available through Open skin directory.

Single-line label/fadelabel frames and mouse targets follow visible captions.
Drag to move them; use Properties for Width/Height. Textboxes, buttons and other
surface controls retain full field frames. No XML dimensions are invented.

XML loading uses position-bounded special-block scanning. Canvas visibility
results are reused only within one frame or pointer inspection, then released.
Baseline comparisons preserve all 488 XML byte/span/DOM mappings and nine
rendered images against a baseline replay. Complex pages remain measurable.

Element selection now batches tree updates and skips unchanged labels/colors.
Pointer picking reuses one measured layout per inspection and releases it before
callbacks; focus and XML changes remain current. Diagnostics evaluate visibility
in one pass. Canvas painting remains an area for further profiling.

PAGES / XML is a permanent sidebar list with filename filter and hover-wheel
scrolling. View above the canvas now offers Edit layout, Isolate selection and
All layers. Edit layout uses short example captions while retaining bindings.
For the recognized MediaTipOverlay.xml structure, Example switches Music, Video,
Pictures, Folder and Radio instead of stacking all five text blocks. These are
editing examples, not live plugin values. Isolation is temporary; view/example
settings persist in the editor project. XML/conditions/export bytes stay intact.

Centered list/playlist sample captions now align vertically with their focus
texture and retain complete glyphs. The native title bar uses the dark header
palette on supported Windows, retaining standard window buttons and behavior.

Fresh executable/package checks: 422 core, 23 skin/sidebar/scrolling and ten
readable-preview scenarios, twelve complete demo workflows, twelve clean-session
workflows and 31 unchanged payload hashes. All 488 installed fixture XML files
are unchanged. Prior integrated-page and broad
UI/browser/review/transition/startup and large-menu reports remain historical.
See docs/RELEASE_READINESS.md for recognition limits and practical scope.

The previous attended client checkpoint used source 07e8ef2: a real Titan Video
width edit persisted, exported exactly and appeared in a complete isolated MP1
skin with its shared header and Left/Right menu navigation. Titan was restored;
all 2,050 original hashes stayed unchanged. See docs/RELEASE_READINESS.md for scope and reviewing
actual exports. No installed skin resources are bundled.
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



