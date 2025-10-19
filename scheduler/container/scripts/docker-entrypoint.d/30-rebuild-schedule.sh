#!/usr/bin/sh
set -e

/usr/bin/quackjob rebuild-schedule
systemctl reload cron