@echo off
setlocal

rem  Double-click this file to play Faultline.
rem
rem  Everything below is here so that somebody who has never opened a terminal can get to the game:
rem  it checks for the one thing that has to be installed, says exactly what to do when it is
rem  missing, and otherwise starts the game and opens it in the browser. The window it opens IS the
rem  game server — closing that window stops the game, which is the only thing a player has to know.

title Faultline

echo.
echo   FAULTLINE
echo   ---------
echo.

cd /d "%~dp0"

rem  The one prerequisite. A missing runtime is by far the most likely reason this file does nothing
rem  useful, so it is checked first and answered with a link rather than an error code.
where dotnet >nul 2>nul
if errorlevel 1 goto :nodotnet

echo   Building. The first run takes a minute or two; after that it is quick.
echo.

dotnet build src\Faultline.Web -c Release -v quiet --nologo
if errorlevel 1 goto :buildfailed

echo.
echo   Starting. Your browser will open at http://localhost:5137
echo.
echo   ^>^> Leave this window open while you play. Closing it stops the game. ^<^<
echo.

dotnet run --project src\Faultline.Web -c Release --no-build
goto :end

:nodotnet
echo   Faultline needs the .NET SDK, and it is not installed on this computer.
echo.
echo   1. Go to:  https://dotnet.microsoft.com/download
echo   2. Download the SDK for Windows and run the installer.
echo   3. Close this window, then double-click "Play Faultline" again.
echo.
echo   It is a normal, free Microsoft installer. Nothing else is needed.
echo.
pause
goto :end

:buildfailed
echo.
echo   The build did not finish, so the game did not start.
echo.
echo   This normally means the .NET SDK is older than the game needs. Installing the latest
echo   from https://dotnet.microsoft.com/download and trying again fixes almost every case.
echo.
echo   If it keeps happening, send whatever is printed above this line to whoever gave you
echo   this folder — that text is the whole diagnosis.
echo.
pause

:end
endlocal
