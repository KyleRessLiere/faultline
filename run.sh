#!/usr/bin/env bash
# Run the Faultline dev server.
#
#   ./run.sh              build and serve on http://localhost:5199
#   ./run.sh -w           hot-reload mode; edits to .razor/.cs reload the browser
#   ./run.sh -p 5300      serve on a different port
#   ./run.sh -o           open a browser once it is listening
#   ./run.sh -t           run the test suite first, refuse to serve if it is red
#   ./run.sh -s           stop whatever is holding the port and exit
#
# Flags combine: ./run.sh -w -o

set -euo pipefail

PROJECT="src/Faultline.Web"
PORT=5199
WATCH=0
OPEN=0
TEST=0
STOP=0

usage() {
  # Print the header comment block, stopping at the first line that is not a comment.
  awk 'NR>1 && /^#/ { sub(/^# ?/, ""); print; next } NR>1 { exit }' "$0"
  exit 0
}

while [ $# -gt 0 ]; do
  case "$1" in
    -p|--port)   PORT="${2:?--port needs a number}"; shift 2 ;;
    -w|--watch)  WATCH=1; shift ;;
    -o|--open)   OPEN=1; shift ;;
    -t|--test)   TEST=1; shift ;;
    -s|--stop)   STOP=1; shift ;;
    -h|--help)   usage ;;
    *)           echo "Unknown option: $1" >&2; echo "Try: $0 --help" >&2; exit 2 ;;
  esac
done

cd "$(dirname "$0")"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "error: dotnet not found on PATH. Install the .NET 10 SDK: https://dotnet.microsoft.com/download" >&2
  exit 1
fi

if [ ! -d "$PROJECT" ]; then
  echo "error: $PROJECT not found — run this from the repo, not a copy of the script." >&2
  exit 1
fi

url="http://localhost:$PORT"

# Find the process listening on a port, on Windows (netstat) or unix (lsof).
listener_pid() {
  local p="$1"
  case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*)
      netstat -ano 2>/dev/null | awk -v port=":$p" '$0 ~ port && /LISTENING/ { print $NF; exit }'
      ;;
    *)
      lsof -ti "tcp:$p" -sTCP:LISTEN 2>/dev/null | head -1
      ;;
  esac
}

kill_listener() {
  local p="$1"
  local target
  target=$(listener_pid "$p")

  if [ -z "$target" ]; then
    echo "nothing listening on port $p"
    return 0
  fi

  echo "stopping PID $target on port $p"
  case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) taskkill //PID "$target" //F >/dev/null 2>&1 || true ;;
    *)                    kill -9 "$target" >/dev/null 2>&1 || true ;;
  esac
  sleep 1
}

if [ "$STOP" -eq 1 ]; then
  kill_listener "$PORT"
  exit 0
fi

# A stale server on the same port keeps serving its own old build output. Because the build writes
# to that same directory, the assets it serves drift out of sync and the app dies on boot with an
# unhelpful "unhandled error". Refuse to add a second one.
if curl -fsS -o /dev/null --max-time 2 "$url" 2>/dev/null; then
  echo "error: something is already serving $url" >&2
  echo "       stop it:        $0 -s -p $PORT" >&2
  echo "       or use another: $0 -p $((PORT + 1))" >&2
  exit 1
fi

if [ "$TEST" -eq 1 ]; then
  echo "==> running tests"
  if ! dotnet test --nologo -v q; then
    echo "error: tests are red — not serving. Fix them first." >&2
    exit 1
  fi
  echo
fi

if [ "$OPEN" -eq 1 ]; then
  # Wait for the port to answer, then open the default browser for this platform.
  (
    for _ in $(seq 1 60); do
      if curl -fsS -o /dev/null --max-time 2 "$url" 2>/dev/null; then
        case "$(uname -s)" in
          MINGW*|MSYS*|CYGWIN*) start "" "$url" ;;
          Darwin)               open "$url" ;;
          *)                    xdg-open "$url" >/dev/null 2>&1 || true ;;
        esac
        exit 0
      fi
      sleep 1
    done
  ) &
fi

# Every sitting lands in docs/playtest/<date>/ without anybody switching anything on. The page
# cannot write a path, so it posts to a local host — and the Blazor dev server is not ours to add an
# endpoint to, so the log host runs beside it on a fixed port. Best effort: if it will not start,
# the game still runs and the log simply stays in the browser.
if ! curl -fsS -o /dev/null --max-time 2 "http://127.0.0.1:5178/playtest/log/ping" 2>/dev/null; then
  ( dotnet run --project tools/Faultline.Launcher -- --log-only >/dev/null 2>&1 & ) || true
  echo "    session log -> docs/playtest/<date>/<time>.log"
fi

echo "==> Faultline on $url   (ctrl-c to stop)"
if [ "$WATCH" -eq 1 ]; then
  echo "    hot reload on — edits to .razor and .cs reload the page"
  exec dotnet watch --project "$PROJECT" -- --urls "$url"
else
  exec dotnet run --project "$PROJECT" --urls "$url"
fi
