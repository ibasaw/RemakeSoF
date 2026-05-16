#!/usr/bin/env bash
#
# Launch the RemakeSoF Linux dedicated server.
#
# Usage:
#   ./Tools/start-server.sh                 # foreground, default port 7777
#   ./Tools/start-server.sh --bg            # background (detached), logs to Logs/server.log
#   ./Tools/start-server.sh --port 7778     # custom port
#   ./Tools/start-server.sh --kill          # stop any running instance
#   ./Tools/start-server.sh --status        # show running instance + last log lines
#
# Combine: ./Tools/start-server.sh --bg --port 7778

set -euo pipefail

# Resolve paths relative to the repo root (parent of this script's dir).
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$( cd "${SCRIPT_DIR}/.." && pwd )"
SERVER_DIR="${REPO_ROOT}/Builds/Linux-Server"
BINARY="${SERVER_DIR}/RemakeSoF.x86_64"
LOG_FILE="${REPO_ROOT}/Logs/server.log"
mkdir -p "${REPO_ROOT}/Logs"

PORT=7777
FRAMERATE=30
MODE_BG=0
ACTION="start"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --bg)         MODE_BG=1; shift ;;
    --port)       PORT="$2"; shift 2 ;;
    --framerate)  FRAMERATE="$2"; shift 2 ;;
    --kill)       ACTION="kill"; shift ;;
    --status)     ACTION="status"; shift ;;
    -h|--help)
      sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      exit 2
      ;;
  esac
done

running_pids() {
  # Match only processes whose actual exe is the RemakeSoF binary
  # (avoids matching unrelated shells/editors that happen to mention the path).
  local pid
  for pid in $(pgrep -f RemakeSoF.x86_64 2>/dev/null); do
    if [[ "$(readlink -f /proc/${pid}/exe 2>/dev/null)" == "${BINARY}" ]]; then
      echo "${pid}"
    fi
  done
}

kill_running() {
  local pids
  pids="$(running_pids)"
  if [[ -z "${pids}" ]]; then
    echo "No running server."
    return 0
  fi
  echo "Stopping PIDs: ${pids}"
  kill ${pids} 2>/dev/null || true
  sleep 2
  pids="$(running_pids)"
  if [[ -n "${pids}" ]]; then
    echo "Force-killing PIDs: ${pids}"
    kill -9 ${pids} 2>/dev/null || true
  fi
}

status() {
  local pids
  pids="$(running_pids)"
  if [[ -z "${pids}" ]]; then
    echo "Server: NOT RUNNING"
  else
    echo "Server: RUNNING (PIDs: ${pids})"
    for pid in ${pids}; do
      ps -o pid=,etime=,args= -p "${pid}" 2>/dev/null
    done
  fi
  if [[ -f "${LOG_FILE}" ]]; then
    echo "--- last 15 log lines (${LOG_FILE}) ---"
    tail -n 15 "${LOG_FILE}"
  fi
}

ensure_binary() {
  if [[ ! -x "${BINARY}" ]]; then
    echo "Binary not found or not executable: ${BINARY}" >&2
    echo "Build it from Unity: target=linux64 subtarget=server." >&2
    exit 1
  fi
}

start_server() {
  ensure_binary
  kill_running >/dev/null
  local args=(--port "${PORT}" --target-framerate "${FRAMERATE}" -batchmode -nographics -logFile "${LOG_FILE}")
  echo "Starting RemakeSoF server"
  echo "  binary:    ${BINARY}"
  echo "  port:      ${PORT}"
  echo "  framerate: ${FRAMERATE}"
  echo "  log:       ${LOG_FILE}"
  cd "${SERVER_DIR}"
  if [[ "${MODE_BG}" -eq 1 ]]; then
    nohup "${BINARY}" "${args[@]}" >/dev/null 2>&1 &
    disown
    sleep 1
    local pid
    pid="$(pgrep -f "Builds/Linux-Server/RemakeSoF.x86_64" || true)"
    echo "Started in background (PID ${pid})"
  else
    echo "Running in foreground — Ctrl-C to stop."
    exec "${BINARY}" "${args[@]}"
  fi
}

case "${ACTION}" in
  start)  start_server ;;
  kill)   kill_running ;;
  status) status ;;
esac
