#!/usr/bin/sh
set -e

target="/etc/cron.d/quackview"
link="/quackview/config/crontab"

if [ ! -f "$target" ]; then
    echo "Missing cron.d file symlink: $target" >&2
    exit 1
fi

if [ ! -L "$link" ] || [ "$(readlink "$link" 2>/dev/null || true)" != "$target" ]; then
    rm -f "$link"
    ln -s "$target" "$link"
fi

crontab -u quackjob "$target"
