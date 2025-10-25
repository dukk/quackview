#!/bin/sh

echo "Ensuring crontab is set up for quackjob user"
crontab -u quackjob -l > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "No crontab found for quackjob user; creating default crontab"
    echo "#*0 3 1 * * /usr/bin/quackjob --job=\"build-image-file-list.json\" >> /quackview/logs/cron-run.log 2>&1" | crontab -u quackjob -
    (crontab -l; echo "#*0 /1 * * * /usr/bin/quackjob --job=\"calendar-events.json\" >> /quackview/logs/cron-run.log 2>&1") | crontab -u quackjob -
    (crontab -l; echo "#0 4 /7 * * /usr/bin/quackjob --job=\"dad-jokes.json\" >> /quackview/logs/cron-run.log 2>&1") | crontab -u quackjob -
    ln -sf /etc/crontabs/quackjob /quackview/config/crontab
else
    echo "Crontab already set up for quackjob user; skipping"
fi