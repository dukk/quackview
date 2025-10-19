#!/usr/bin/sh
set -e

LOG_DIR="${QUACKVIEW_DIR}/logs"
LOG_FILE="${LOG_DIR}/docker-entrypoint.log"

mkdir -p "$LOG_DIR"

if [ -d /docker-entrypoint.d ]; then
    ran_any=0
    for f in /docker-entrypoint.d/*; do
        [ -f "$f" ] || continue
        [ -x "$f" ] || continue
        ran_any=1
        printf '%s START %s\n' "$(date '+%Y-%m-%d %H:%M:%S%z')" "$f" >> "$LOG_FILE"
        "$f" >>"$LOG_FILE" 2>&1
        rc=$?
        if [ "$rc" -ne 0 ]; then
            printf '%s ERROR %s exit %s\n' "$(date '+%Y-%m-%d %H:%M:%S%z')" "$f" "$rc" >> "$LOG_FILE"
            exit "$rc"
        fi
        printf '%s DONE %s\n' "$(date '+%Y-%m-%d %H:%M:%S%z')" "$f" >> "$LOG_FILE"
    done
    if [ "$ran_any" -eq 0 ]; then
        printf '%s No executable scripts found in /docker-entrypoint.d; skipping initialization steps.\n' "$(date '+%Y-%m-%d %H:%M:%S%z')" >> "$LOG_FILE"
    fi
else
    printf '%s Directory /docker-entrypoint.d not found\n' "$(date '+%Y-%m-%d %H:%M:%S%z')" >> "$LOG_FILE"
fi