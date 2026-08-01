<#
.SYNOPSIS
    Start Faultline and open it. No arguments, no decisions.

.DESCRIPTION
    Finds a free port by itself, starts the dev server and opens a browser at it. If something is
    already using the usual port it steps to the next one rather than failing, so running this twice
    gives you two working servers instead of one error.

    For anything more deliberate — hot reload, stopping a server, a specific port — use run.ps1.

.EXAMPLE
    .\play.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host 'dotnet is not installed, or is not on PATH.' -ForegroundColor Red
    Write-Host 'Get the .NET 10 SDK from https://dotnet.microsoft.com/download and run this again.'
    exit 1
}

function Test-PortFree {
    param([int]$Port)
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $client.Connect('127.0.0.1', $Port)
        $client.Close()
        return $false
    }
    catch {
        return $true
    }
}

# Walk forward until something is free. A stale server on the usual port keeps serving its own old
# build, so stepping past it is safer than reusing it.
$port = 5199
while ($port -lt 5260) {
    if (Test-PortFree -Port $port) { break }
    Write-Host "  port $port is busy, trying $($port + 1)" -ForegroundColor DarkGray
    $port++
}

if (-not (Test-PortFree -Port $port)) {
    Write-Host 'No free port between 5199 and 5260. Close some servers and try again.' -ForegroundColor Red
    Write-Host 'To stop one:  .\run.ps1 -Stop -Port 5199'
    exit 1
}

$url = "http://localhost:$port"

Write-Host ''
Write-Host "  Faultline  ->  $url" -ForegroundColor Green
Write-Host '  A browser will open once it is ready. Ctrl-C here stops the server.' -ForegroundColor DarkGray
Write-Host ''

# Open the browser only once the port actually answers, otherwise it lands on a connection error
# while the first build is still running.
$opener = Start-Job -ScriptBlock {
    param($p, $u)
    for ($i = 0; $i -lt 120; $i++) {
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
} -ArgumentList $port, $url

try {
    dotnet run --project 'src/Faultline.Web' --urls $url
}
finally {
    Stop-Job $opener -ErrorAction SilentlyContinue | Out-Null
    Remove-Job $opener -Force -ErrorAction SilentlyContinue | Out-Null
}
