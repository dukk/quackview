#!/bin/sh
set -e

LOG_DIR="${QUACKVIEW_DIR}/logs"
LOG_FILE="${LOG_DIR}/docker-entrypoint.log"
mkdir -p "$LOG_DIR"

ep_log() {
    ts="$(date '+%Y-%m-%d %H:%M:%S')"
    printf '%s %s\n' "$ts" "$*" | tee -a "$LOG_FILE"
}
ep_log "\nStarting Docker entrypoint"
ep_log "Running as: $(whoami)"

if [ -d /docker-entrypoint.d ]; then
    ran_any=0
    for f in /docker-entrypoint.d/*; do
        [ -f "$f" ] || continue
        [ -x "$f" ] || continue
        ran_any=1
        ep_log "START $f"
        output=$("$f" 2>&1) || rc=$?
        rc=${rc:-0}
        if [ -n "$output" ]; then
            f_short=${f#/docker-entrypoint.d/}
            echo "$output" | while IFS= read -r line; do
                ep_log "[$f_short] $line"
            done
        fi
        if [ "$rc" -ne 0 ]; then
            ep_log "ERROR $f exit $rc"
            exit "$rc"
        fi
        ep_log "DONE $f"
    done
    if [ "$ran_any" -eq 0 ]; then
        ep_log "No executable scripts found in /docker-entrypoint.d; skipping initialization steps."
    fi
else
    ep_log "Directory /docker-entrypoint.d not found"
fi
