#!/bin/sh
set -e

# Defaults
: "${PORT:=3000}"
: "${WATCH_DIR:=/quackview/data}"

echo "Starting data-watcher-sse on port ${PORT}, watching ${WATCH_DIR}"
NODE_ENV=production WATCH_DIR="${WATCH_DIR}" PORT="${PORT}" \
  node /opt/data-watcher-sse/index.js &

echo "Starting nginx"
exec nginx -g 'daemon off;'
