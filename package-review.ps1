param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'work\Review-Build'),
    [string]$Revision = 'working-tree',
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (-not $SkipBuild) { & (Join-Path $PSScriptRoot 'build.ps1'); & (Join-Path $PSScriptRoot 'LiveBridge\build-plugin.ps1') }
$reviewOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($reviewOutput) | Out-Null
$reviewTarget = Join-Path $reviewOutput 'MP1-Skin-Studio-Review.zip'
$reviewTemp = $reviewTarget + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
$reviewFiles = @('MP1SkinStudio.exe', 'LICENSE', 'docs/USER_GUIDE.md', 'docs/QUICK_START.md', 'docs/RELEASE_READINESS.md', 'docs/RELEASE_NOTES.md')
$reviewFiles += @('LiveBridge/bin/MP1SkinStudio.LiveBridge.dll','LiveBridge/Install-LiveBridge.ps1','docs/LIVE_PREVIEW.md')
$reviewFiles += @('docs/SAVE_SKIN_COPY.md')
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

Save skin … beside Live saves applied inputs to a complete separate output skin.
Create its first copy once; later saves update reviewed XML with conflict checks,
transaction rollback and automatic ZIP backups. The current source, live preview
and editor Undo history continue. Save Project also remembers the output folder.
See docs/SAVE_SKIN_COPY.md for persistence, recovery and normal MP skin selection.

Live preview: install LiveBridge/bin/MP1SkinStudio.LiveBridge.dll into your MP1
1.39 plugins/process folder, or use LiveBridge/Install-LiveBridge.ps1 with admin
rights for Program Files. Start MP once, open the same skin/theme/page in Studio,
enable Live in the toolbar and Connect. Valid input fields and canvas drags
update native MP controls without Apply, XML export, reload or restart.
Disconnect restores preview overrides. Read docs/LIVE_PREVIEW.md for setup,
runtime property limits and the distinction between live preview and saved XML.

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

Properties are grouped under collapsible headings; hidden inputs retain their
draft values. Focus a field or hover its label for help beneath the panel.
Font name / ... opens a searchable sample-text preview with actual font family,
pixel size and substitution details. Use font creates a draft; Apply commits it.
Toolbox opens a side panel for dragging text, buttons and skin Media images onto
the preview. Drops target the current page XML and undo in one step. External
images must first be placed in Media. Snap spacing is selectable; Element offers
separate position/size protection. Project format 5 retains these settings and
reads formats 1-4; older editors cannot open newly saved format-5 projects.

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

Copy skin beside SKIN creates a complete independent skin with a new name and
location, optional applied XML and optional opening. Source files stay unchanged.
Copying runs in the background with Cancel and never replaces existing folders.

Select different Skin Folder at the bottom of SKIN opens the existing folder
browser, retaining pending-input and unsaved-session protections.

Dropdown backgrounds and selections now match the dark scrollbar palette,
including SKIN, THEME, View, Example and dialog selectors.

Fresh focused checks: 25 skin/sidebar/menu/scroll and ten readable-preview
scenarios passed, with all 488 installed XML and 26 demo hashes unchanged.
Earlier acceptance passed 430 core, eleven
copy-dialog scenarios, twelve demo and twelve clean-session workflows. A full
DefaultWideHD copy passed all 3,399 original/copy file hashes. Prior readability,
integrated-page and broad
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



