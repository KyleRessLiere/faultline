# Master Design Download Automation

This repository includes a PowerShell watcher that automatically installs a newly downloaded `MASTER_DESIGN.md` file into the project and archives the previous version.

## What it does

After the watcher starts, it monitors the configured Downloads folder for newly created Markdown files named like:

```text
MASTER_DESIGN.md
MASTER_DESIGN (1).md
Master design.md
Master-design (2).md
```

When a matching file appears, the script:

1. Waits for the browser to finish downloading it.
2. Moves the current design document into `docs/design-history`.
3. Adds a readable Eastern Time timestamp to the archived filename.
4. Installs the new file as `docs/MASTER_DESIGN.md`.
5. Removes the downloaded duplicate unless `-KeepDownloadedFile` is used.

Existing files already present in Downloads when the script starts are ignored.

## Requirements

- Windows
- Windows PowerShell 5.1 or PowerShell 7
- A local clone of this repository

## Default configuration

The repository owner's defaults are built into the script:

```text
Project root:
C:\Users\ressl\Documents\git\omarkylegame

Downloads:
C:\Users\ressl\Downloads

Destination:
docs\MASTER_DESIGN.md

History:
docs\design-history
```

Other users should supply their own paths with parameters.

## Run manually

From the repository root:

```powershell
.\watch-master-design.ps1
```

Run it with custom paths:

```powershell
.\watch-master-design.ps1 `
  -ProjectRoot "D:\git\omarkylegame" `
  -DownloadsDirectory "$env:USERPROFILE\Downloads"
```

Keep the downloaded browser copy after installation:

```powershell
.\watch-master-design.ps1 -KeepDownloadedFile
```

## Parameters

| Parameter | Purpose | Default |
|---|---|---|
| `ProjectRoot` | Root directory of the cloned repository | `C:\Users\ressl\Documents\git\omarkylegame` |
| `DownloadsDirectory` | Folder monitored for new downloads | `%USERPROFILE%\Downloads` |
| `DestinationRelativePath` | Destination file relative to the repository root | `docs\MASTER_DESIGN.md` |
| `HistoryRelativePath` | Archive directory relative to the repository root | `docs\design-history` |
| `KeepDownloadedFile` | Leaves the downloaded copy in Downloads | Disabled |

## Run automatically at Windows login

Open PowerShell and create a scheduled task.

### Repository owner command

```powershell
schtasks /Create `
  /TN "Master Design Watcher" `
  /SC ONLOGON `
  /RL LIMITED `
  /TR "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File ""C:\Users\ressl\Documents\git\omarkylegame\watch-master-design.ps1""" `
  /F
```

### Portable command for another user

Update the repository path before running:

```powershell
$repo = "D:\git\omarkylegame"

$taskCommand = "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$repo\watch-master-design.ps1`" -ProjectRoot `"$repo`""

schtasks /Create `
  /TN "Master Design Watcher" `
  /SC ONLOGON `
  /RL LIMITED `
  /TR $taskCommand `
  /F
```

If the Downloads folder is nonstandard:

```powershell
$repo = "D:\git\omarkylegame"
$downloads = "D:\Browser Downloads"

$taskCommand = "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$repo\watch-master-design.ps1`" -ProjectRoot `"$repo`" -DownloadsDirectory `"$downloads`""

schtasks /Create `
  /TN "Master Design Watcher" `
  /SC ONLOGON `
  /RL LIMITED `
  /TR $taskCommand `
  /F
```

## Manage the scheduled task
schtasks /Create `
  /TN "Master Design Watcher" `
  /SC ONLOGON `
  /RL LIMITED `
  /TR "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File ""C:\Users\ressl\Documents\git\omarkylegame\watch-master-design.ps1""" `
  /F
Run it immediately:

```powershell
schtasks /Run /TN "Master Design Watcher"
```

Stop it:

```powershell
schtasks /End /TN "Master Design Watcher"
```

Confirm it exists:

```powershell
schtasks /Query /TN "Master Design Watcher" /V /FO LIST
```

Delete it:

```powershell
schtasks /Delete /TN "Master Design Watcher" /F
```

## Testing

1. Start the watcher.
2. Download a fresh file from Claude named `MASTER_DESIGN.md`.
3. Confirm the new file appears at:

```text
docs\MASTER_DESIGN.md
```

4. Confirm the previous version appears in:

```text
docs\design-history
```

An archived filename will look like:

```text
MASTER_DESIGN_2026-08-03_05-30-12_PM_EDT.md
```

## Troubleshooting

### Nothing happens after downloading

Confirm the filename matches one of the accepted forms. The script accepts a space, underscore, or hyphen between `MASTER` and `DESIGN`, plus browser duplicate suffixes such as `(1)`.

Confirm the watcher was already running before the file was downloaded. Files already present when the watcher starts are intentionally ignored.

### The scheduled task runs but no PowerShell window appears

That is expected. The scheduled command uses:

```text
-WindowStyle Hidden
```

Temporarily remove that argument while troubleshooting.

### PowerShell blocks the script

Run it manually with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\watch-master-design.ps1"
```

### The wrong repository is updated

Pass the intended repository explicitly:

```powershell
.\watch-master-design.ps1 -ProjectRoot "D:\git\correct-repository"
```

## Resource usage

The watcher uses `FileSystemWatcher`, so it receives filesystem events from Windows instead of repeatedly scanning the Downloads folder. While idle, CPU usage should remain near zero, with one lightweight PowerShell process remaining in memory.
