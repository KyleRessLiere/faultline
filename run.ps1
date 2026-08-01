<#
.SYNOPSIS
    Run the Faultline dev server.

.DESCRIPTION
    PowerShell twin of run.sh, so the app spins up without needing Git Bash.

.EXAMPLE
    .\run.ps1
    Build and serve on http://localhost:5199

.EXAMPLE
    .\run.ps1 -Watch -Open
    Hot reload, and open a browser once it is listening

.EXAMPLE
    .\run.ps1 -Port 5300 -Test
    Run the tests first, then serve on another port
#>
[CmdletBinding()]
param(
    [int]$Port = 5199,
    [switch]$Watch,
    [switch]$Open,
    [switch]$Test
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

$project = 'src/Faultline.Web'
$url = "http://localhost:$Port"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'dotnet not found on PATH. Install the .NET 10 SDK: https://dotnet.microsoft.com/download'
    exit 1
}

if (-not (Test-Path $project)) {
    Write-Error "$project not found - run this from the repo, not a copy of the script."
    exit 1
}

function Test-Listening {
    param([int]$OnPort)
    try {
        $c = New-Object System.Net.Sockets.TcpClient
        $c.Connect('127.0.0.1', $OnPort)
        $c.Close()
        return $true
    }
    catch {
        return $false
    }
}

# A stale server on the same port silently serves old code, which is a confusing way to lose an hour.
if (Test-Listening -OnPort $Port) {
    Write-Error "Something is already serving $url. Stop it, or pick another port:  .\run.ps1 -Port $($Port + 1)"
    exit 1
}

if ($Test) {
    Write-Host '==> running tests' -ForegroundColor Cyan
    dotnet test --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Error 'Tests are red - not serving. Fix them first.'
        exit 1
    }
    Write-Host ''
}

if ($Open) {
    # Wait for the port to answer, then open the default browser.
    $waiter = Start-Job -ScriptBlock {
        param($p, $u)
        for ($i = 0; $i -lt 60; $i++) {
            try {
                $c = New-Object System.Net.Sockets.TcpClient
                $c.Connect('127.0.0.1', $p)
                $c.Close()
                Start-Process $u
                return
            }
            catch {
                Start-Sleep -Seconds 1
            }
        }
    } -ArgumentList $Port, $url
    $null = $waiter
}

Write-Host "==> Faultline on $url   (ctrl-c to stop)" -ForegroundColor Green

if ($Watch) {
    Write-Host '    hot reload on - edits to .razor and .cs reload the page' -ForegroundColor DarkGray
    dotnet watch --project $project -- --urls $url
}
else {
    dotnet run --project $project --urls $url
}
