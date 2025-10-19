#!/usr/bin/sh

mkdir -p /quackview/{config,jobs,data,secrets,logs}/

[ -d /quackview-template ] && cp -rn /quackview-template/. /quackview/

if [ ! -e /quackview/logs/cron.log ] && [ ! -L /quackview/logs/cron.log ]; then
    ln -s /var/log/cron.log /quackview/logs/cron.log
fi

chown -R quackjob:quackjob /quackview/