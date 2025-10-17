#!/usr/bin/sh

crontab -u quackjob /etc/cron.d/quackview 

/usr/bin/quackjob rebuild-schedule
systemctl reload cron

touch /var/log/cron.log 
tail -f /var/log/cron.log &
