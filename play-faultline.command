#!/usr/bin/env bash
#
#  Double-click this file to play Faultline on macOS, or run it from a terminal on Linux.
#
#  The .command extension is what makes macOS treat it as double-clickable. On Linux it works the
#  same way from a terminal, or from a file manager once it is marked executable:
#
#      chmod +x play-faultline.command
#
#  Everything below exists so that somebody who has never opened a terminal can get to the game: it
#  checks for the one thing that has to be installed, says exactly what to do when it is missing,
#  and otherwise starts the game and opens it in the browser.

set -u
cd "$(dirname "$0")" || exit 1

echo
echo "  FAULTLINE"
echo "  ---------"
echo

if ! command -v dotnet >/dev/null 2>&1; then
    echo "  Faultline needs the .NET SDK, and it is not installed on this computer."
    echo
    echo "  1. Go to:  https://dotnet.microsoft.com/download"
    echo "  2. Download the SDK for your machine and run the installer."
    echo "  3. Close this window, then open this file again."
    echo
    echo "  It is a normal, free Microsoft installer. Nothing else is needed."
    echo
    read -r -p "  Press Return to close. " _
    exit 1
fi

echo "  Building. The first run takes a minute or two; after that it is quick."
echo

if ! dotnet build src/Faultline.Web -c Release -v quiet --nologo; then
    echo
    echo "  The build did not finish, so the game did not start."
    echo
    echo "  This normally means the .NET SDK is older than the game needs. Installing the latest"
    echo "  from https://dotnet.microsoft.com/download and trying again fixes almost every case."
    echo
    echo "  If it keeps happening, send whatever is printed above this line to whoever gave you"
    echo "  this folder — that text is the whole diagnosis."
    echo
    read -r -p "  Press Return to close. " _
    exit 1
fi

echo
echo "  Starting. Your browser will open at http://localhost:5137"
echo
echo "  >> Leave this window open while you play. Closing it stops the game. <<"
echo

# macOS does not always honour the project's launchBrowser setting, so the page is opened here once
# the server has had a moment to bind. Backgrounded, because the run command below never returns.
( sleep 4; command -v open >/dev/null 2>&1 && open http://localhost:5137 ) &

exec dotnet run --project src/Faultline.Web -c Release --no-build
