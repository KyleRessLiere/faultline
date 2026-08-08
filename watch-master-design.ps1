<#
.SYNOPSIS
Watches a Downloads folder for new MASTER_DESIGN and prompt- Markdown files.

.DESCRIPTION
Only files created after this script starts are processed.

MASTER_DESIGN files:
  - Archive the existing authoritative design document.
  - Install the downloaded file as docs\MASTER_DESIGN.md.

Prompt files:
  - Match any .md filename beginning with "prompt-".
  - Create an Eastern-date folder under docs\prompts if it does not exist.
  - Copy the prompt into that day's folder.
  - Add a numeric suffix instead of overwriting an existing file.

.PARAMETER ProjectRoot
Root directory of the repository.

.PARAMETER DownloadsDirectory
Directory to watch for new downloads.

.PARAMETER DestinationRelativePath
Authoritative master-design path relative to ProjectRoot.

.PARAMETER HistoryRelativePath
Master-design history directory relative to ProjectRoot.

.PARAMETER PromptsRelativePath
Prompt archive root relative to ProjectRoot.

.PARAMETER KeepDownloadedFile
Leaves processed files in Downloads instead of deleting them.

.EXAMPLE
.\watch-master-design.ps1

.EXAMPLE
.\watch-master-design.ps1 `
  -ProjectRoot "D:\git\omarkylegame" `
  -DownloadsDirectory "$env:USERPROFILE\Downloads"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectRoot = "C:\Users\ressl\Documents\git\omarkylegame",

    [Parameter()]
    [string]$DownloadsDirectory = (Join-Path $env:USERPROFILE "Downloads"),

    [Parameter()]
    [string]$DestinationRelativePath = "docs\MASTER_DESIGN.md",

    [Parameter()]
    [string]$HistoryRelativePath = "docs\design-history",

    [Parameter()]
    [string]$PromptsRelativePath = "docs\prompts",

    [Parameter()]
    [switch]$KeepDownloadedFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectDirectory = [System.IO.Path]::GetFullPath($ProjectRoot)
$downloadsDirectoryResolved = [System.IO.Path]::GetFullPath($DownloadsDirectory)

$destinationFile = Join-Path $projectDirectory $DestinationRelativePath
$docsDirectory = Split-Path -Parent $destinationFile
$historyDirectory = Join-Path $projectDirectory $HistoryRelativePath
$promptsDirectory = Join-Path $projectDirectory $PromptsRelativePath

# Matches:
#   MASTER_DESIGN.md
#   MASTER_DESIGN (1).md
#   Master design.md
#   Master-design (2).md
$masterDesignPattern = '(?i)^MASTER[ _-]DESIGN(?: \(\d+\))?\.md$'

# Matches any Markdown filename beginning with prompt-, including:
#   prompt-warrens-v2-index-2026-08-05-2140.md
#   prompt-warrens-v2-index-2026-08-05-2140 (1).md
$promptPattern = '(?i)^prompt-.*\.md$'

if (-not (Test-Path -LiteralPath $downloadsDirectoryResolved -PathType Container)) {
    throw "Downloads directory does not exist: $downloadsDirectoryResolved"
}

if (-not (Test-Path -LiteralPath $projectDirectory -PathType Container)) {
    throw "Project root does not exist: $projectDirectory"
}

New-Item -ItemType Directory -Path $docsDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $historyDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $promptsDirectory -Force | Out-Null

$recentlyProcessed = @{}

function Get-EasternDateTime {
    $easternZone = [System.TimeZoneInfo]::FindSystemTimeZoneById(
        "Eastern Standard Time"
    )

    $easternTime = [System.TimeZoneInfo]::ConvertTimeFromUtc(
        [DateTime]::UtcNow,
        $easternZone
    )

    $zoneAbbreviation = if (
        $easternZone.IsDaylightSavingTime($easternTime)
    ) {
        "EDT"
    }
    else {
        "EST"
    }

    return @{
        Value         = $easternTime
        DateFolder    = $easternTime.ToString("yyyy-MM-dd")
        FileTimestamp = $easternTime.ToString("yyyy-MM-dd_hh-mm-ss_tt")
        DisplayTime   = $easternTime.ToString("MMMM d, yyyy h:mm:ss tt")
        Zone          = $zoneAbbreviation
    }
}

function Wait-ForCompletedDownload {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath
    )

    $previousLength = -1
    $stableChecks = 0

    # Wait for up to approximately 60 seconds.
    for ($attempt = 0; $attempt -lt 120; $attempt++) {
        if (-not (Test-Path -LiteralPath $FilePath)) {
            Start-Sleep -Milliseconds 500
            continue
        }

        try {
            $file = Get-Item -LiteralPath $FilePath

            $stream = [System.IO.File]::Open(
                $FilePath,
                [System.IO.FileMode]::Open,
                [System.IO.FileAccess]::Read,
                [System.IO.FileShare]::None
            )

            $stream.Close()

            if (
                $file.Length -gt 0 -and
                $file.Length -eq $previousLength
            ) {
                $stableChecks++
            }
            else {
                $stableChecks = 0
                $previousLength = $file.Length
            }

            if ($stableChecks -ge 2) {
                return $true
            }
        }
        catch {
            $stableChecks = 0
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Get-UniqueFilePath {
    param(
        [Parameter(Mandatory)]
        [string]$Directory,

        [Parameter(Mandatory)]
        [string]$FileName
    )

    $candidate = Join-Path $Directory $FileName

    if (-not (Test-Path -LiteralPath $candidate)) {
        return $candidate
    }

    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
    $extension = [System.IO.Path]::GetExtension($FileName)
    $counter = 2

    do {
        $candidate = Join-Path $Directory (
            "{0}_{1}{2}" -f $baseName, $counter, $extension
        )

        $counter++
    }
    while (Test-Path -LiteralPath $candidate)

    return $candidate
}

function Get-UniqueArchivePath {
    param(
        [Parameter(Mandatory)]
        [hashtable]$EasternDateTime
    )

    $archiveName = "MASTER_DESIGN_{0}_{1}.md" -f `
        $EasternDateTime.FileTimestamp,
        $EasternDateTime.Zone

    return Get-UniqueFilePath `
        -Directory $historyDirectory `
        -FileName $archiveName
}

function Install-MasterDesign {
    param(
        [Parameter(Mandatory)]
        [string]$DownloadedFile
    )

    $easternDateTime = Get-EasternDateTime
    $temporaryFile = Join-Path $docsDirectory ".MASTER_DESIGN.pending.md"
    $archiveFile = $null
    $existingFileArchived = $false

    try {
        Copy-Item `
            -LiteralPath $DownloadedFile `
            -Destination $temporaryFile `
            -Force

        $temporaryFileInfo = Get-Item -LiteralPath $temporaryFile

        if ($temporaryFileInfo.Length -eq 0) {
            throw "The downloaded design document is empty."
        }

        if (Test-Path -LiteralPath $destinationFile) {
            $archiveFile = Get-UniqueArchivePath `
                -EasternDateTime $easternDateTime

            Move-Item `
                -LiteralPath $destinationFile `
                -Destination $archiveFile

            $existingFileArchived = $true

            Write-Host "Archived previous master design:"
            Write-Host "  $archiveFile"
        }
        else {
            Write-Host "No existing MASTER_DESIGN.md was found to archive."
        }

        Move-Item `
            -LiteralPath $temporaryFile `
            -Destination $destinationFile `
            -Force

        if (-not $KeepDownloadedFile) {
            Remove-Item `
                -LiteralPath $DownloadedFile `
                -Force
        }

        Write-Host "Master design updated successfully:"
        Write-Host "  $destinationFile"
        Write-Host "  $($easternDateTime.DisplayTime) $($easternDateTime.Zone)"
    }
    catch {
        Write-Error "Master design update failed: $($_.Exception.Message)"

        if (Test-Path -LiteralPath $temporaryFile) {
            Remove-Item `
                -LiteralPath $temporaryFile `
                -Force `
                -ErrorAction SilentlyContinue
        }

        if (
            $existingFileArchived -and
            $archiveFile -and
            (Test-Path -LiteralPath $archiveFile) -and
            -not (Test-Path -LiteralPath $destinationFile)
        ) {
            try {
                Move-Item `
                    -LiteralPath $archiveFile `
                    -Destination $destinationFile

                Write-Warning "The previous MASTER_DESIGN.md was restored."
            }
            catch {
                Write-Error "The previous design could not be restored."
                Write-Error "Archived copy remains at: $archiveFile"
            }
        }

        throw
    }
}

function Install-PromptFile {
    param(
        [Parameter(Mandatory)]
        [string]$DownloadedFile
    )

    $easternDateTime = Get-EasternDateTime
    $dateDirectory = Join-Path `
        $promptsDirectory `
        $easternDateTime.DateFolder

    New-Item `
        -ItemType Directory `
        -Path $dateDirectory `
        -Force | Out-Null

    $originalName = [System.IO.Path]::GetFileName($DownloadedFile)

    $destinationPrompt = Get-UniqueFilePath `
        -Directory $dateDirectory `
        -FileName $originalName

    Copy-Item `
        -LiteralPath $DownloadedFile `
        -Destination $destinationPrompt

    if (-not $KeepDownloadedFile) {
        Remove-Item `
            -LiteralPath $DownloadedFile `
            -Force
    }

    Write-Host "Prompt archived successfully:"
    Write-Host "  $destinationPrompt"
    Write-Host "  $($easternDateTime.DisplayTime) $($easternDateTime.Zone)"
}

function Process-DownloadedFile {
    param(
        [Parameter(Mandatory)]
        [string]$DownloadedFile
    )

    $fileName = [System.IO.Path]::GetFileName($DownloadedFile)

    if (
        $fileName -notmatch $masterDesignPattern -and
        $fileName -notmatch $promptPattern
    ) {
        return
    }

    $eventKey = $DownloadedFile.ToLowerInvariant()
    $now = Get-Date

    if ($recentlyProcessed.ContainsKey($eventKey)) {
        $lastProcessed = $recentlyProcessed[$eventKey]

        if (($now - $lastProcessed).TotalSeconds -lt 15) {
            return
        }
    }

    $recentlyProcessed[$eventKey] = $now

    Write-Host ""
    Write-Host "Detected new download:"
    Write-Host "  $DownloadedFile"
    Write-Host "Waiting for download completion..."

    if (-not (Wait-ForCompletedDownload -FilePath $DownloadedFile)) {
        Write-Warning "The file did not become ready within the timeout."
        $recentlyProcessed.Remove($eventKey)
        return
    }

    try {
        if ($fileName -match $masterDesignPattern) {
            Install-MasterDesign -DownloadedFile $DownloadedFile
        }
        elseif ($fileName -match $promptPattern) {
            Install-PromptFile -DownloadedFile $DownloadedFile
        }
    }
    catch {
        $recentlyProcessed.Remove($eventKey)
    }
}

$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $downloadsDirectoryResolved
$watcher.Filter = "*.md"
$watcher.IncludeSubdirectories = $false
$watcher.NotifyFilter = `
    [System.IO.NotifyFilters]::FileName -bor `
    [System.IO.NotifyFilters]::CreationTime -bor `
    [System.IO.NotifyFilters]::LastWrite
$watcher.EnableRaisingEvents = $true

Register-ObjectEvent `
    -InputObject $watcher `
    -EventName Created `
    -SourceIdentifier "DesignAutomation.Created" | Out-Null

Register-ObjectEvent `
    -InputObject $watcher `
    -EventName Renamed `
    -SourceIdentifier "DesignAutomation.Renamed" | Out-Null

Write-Host "Design download watcher is running."
Write-Host ""
Write-Host "Only files downloaded after this script starts will be processed."
Write-Host ""
Write-Host "Watching:"
Write-Host "  $downloadsDirectoryResolved"
Write-Host ""
Write-Host "Accepted master-design filenames include:"
Write-Host "  MASTER_DESIGN.md"
Write-Host "  MASTER_DESIGN (1).md"
Write-Host ""
Write-Host "Accepted prompt filenames:"
Write-Host "  Any .md file beginning with prompt-"
Write-Host ""
Write-Host "Master design destination:"
Write-Host "  $destinationFile"
Write-Host ""
Write-Host "Master design history:"
Write-Host "  $historyDirectory"
Write-Host ""
Write-Host "Prompt date folders:"
Write-Host "  $promptsDirectory\yyyy-MM-dd"
Write-Host ""
Write-Host "Press Ctrl+C to stop."

try {
    while ($true) {
        $event = Wait-Event -Timeout 2

        if ($event) {
            $downloadedPath = $event.SourceEventArgs.FullPath

            Remove-Event `
                -EventIdentifier $event.EventIdentifier `
                -ErrorAction SilentlyContinue

            if ($downloadedPath) {
                Process-DownloadedFile `
                    -DownloadedFile $downloadedPath
            }
        }

        $cutoff = (Get-Date).AddMinutes(-5)

        foreach ($key in @($recentlyProcessed.Keys)) {
            if ($recentlyProcessed[$key] -lt $cutoff) {
                $recentlyProcessed.Remove($key)
            }
        }
    }
}
finally {
    Unregister-Event `
        -SourceIdentifier "DesignAutomation.Created" `
        -ErrorAction SilentlyContinue

    Unregister-Event `
        -SourceIdentifier "DesignAutomation.Renamed" `
        -ErrorAction SilentlyContinue

    Get-Event |
        Where-Object {
            $_.SourceIdentifier -in @(
                "DesignAutomation.Created",
                "DesignAutomation.Renamed"
            )
        } |
        Remove-Event `
            -ErrorAction SilentlyContinue

    $watcher.EnableRaisingEvents = $false
    $watcher.Dispose()

    Write-Host ""
    Write-Host "Design download watcher stopped."
}