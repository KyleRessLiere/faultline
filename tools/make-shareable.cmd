@echo off
setlocal

rem  Builds a copy of Faultline that somebody with nothing installed can play.
rem
rem  Output: dist\Faultline\  and  dist\Faultline-windows.zip
rem
rem  Inside is one executable and the game's files. The .NET runtime is published *inside* the
rem  executable, so the person you send it to installs nothing, opens nothing and reads nothing —
rem  they double-click Faultline.exe and the game opens in their browser.
rem
rem  Run this from the repo root:   tools\make-shareable.cmd

cd /d "%~dp0.."

set RUNTIME=%1
if "%RUNTIME%"=="" set RUNTIME=win-x64

echo.
echo   Building a shareable Faultline for %RUNTIME%.
echo   This takes a few minutes and produces roughly 80 MB.
echo.

if exist dist\Faultline rmdir /s /q dist\Faultline
mkdir dist\Faultline 2>nul

echo   [1/3] Publishing the game...
dotnet publish src\Faultline.Web -c Release -o dist\_web --nologo -v quiet
if errorlevel 1 goto :failed

echo   [2/3] Publishing the launcher with the runtime inside it...
dotnet publish tools\Faultline.Launcher -c Release -r %RUNTIME% --self-contained true ^
    -p:PublishSingleFile=true -o dist\Faultline --nologo -v quiet
if errorlevel 1 goto :failed

echo   [3/3] Assembling...

rem  The launcher serves whatever sits in wwwroot beside it, which is exactly what publishing a
rem  Blazor app produces.
xcopy /e /i /q /y dist\_web\wwwroot dist\Faultline\wwwroot >nul
if errorlevel 1 goto :failed

rem  Debug symbols are most of what is left after the runtime, and nobody playing needs them.
del /q dist\Faultline\*.pdb 2>nul

copy /y "docs\SHARING.md" "dist\Faultline\READ ME FIRST.txt" >nul 2>nul

powershell -NoProfile -Command ^
    "Compress-Archive -Path 'dist\Faultline\*' -DestinationPath 'dist\Faultline-windows.zip' -Force"
if errorlevel 1 goto :failed

rmdir /s /q dist\_web

echo.
echo   Done.
echo.
echo     Folder:  dist\Faultline\
echo     Zip:     dist\Faultline-windows.zip
echo.
echo   Send the zip. Tell them: unzip it, then double-click Faultline.
echo.
goto :end

:failed
echo.
echo   Something above did not finish, so there is no shareable build.
echo   The last lines printed are the reason.
echo.

:end
endlocal
