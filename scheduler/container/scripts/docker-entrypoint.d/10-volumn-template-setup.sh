#!/bin/sh

# echo "Setting up volume directories and templates"
# mkdir -p /quackview/config/ /quackview/jobs/ /quackview/data/ /quackview/secrets/ /quackview/logs/

if [ -d /quackview-template ]; then
    echo "Copying template files to /quackview"
    rsync -av --backup --suffix=".new" /quackview-template/ /quackview
    #cp -rnv /quackview-template/* /quackview
else
    echo "No /quackview-template directory found; skipping template copy"
fi

if [ ! -e /quackview/logs/cron.log ] && [ ! -L /quackview/logs/cron.log ]; then
    echo "Missing cron log file symlink; creating"
    ln -s /var/log/cron.log /quackview/logs/cron.log
fi

echo "Setting ownership of /quackview/ to quackjob user"
chown -R quackjob:quackjob /quackview/