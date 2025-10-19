# Use a minimal Alpine base image
FROM alpine:3.20

RUN apk update && apk upgrade
RUN apk add --no-cache \
        ca-certificates \
        curl \
        cronie \
        dos2unix

ENV QUACKVIEW_DIR=/quackview/

RUN adduser -D -h /home/quackjob -s /bin/sh quackjob

COPY container/scripts/docker-entrypoint.sh /docker-entrypoint.sh
RUN dos2unix /docker-entrypoint.sh && chmod +x /docker-entrypoint.sh

RUN mkdir -p /etc/docker-entrypoint.d
COPY container/scripts/docker-entrypoint.d/* /etc/docker-entrypoint.d/
RUN dos2unix /etc/docker-entrypoint.d/*.sh && chmod +x /etc/docker-entrypoint.d/*.sh

COPY container/template/* /quackview-template/
COPY dist/bin/appsettings.json /quackview-template/config/quackjob.json
COPY dist/bin/quackjob /usr/bin/quackjob
RUN chown -R quackjob:quackjob /quackview-template
RUN chmod +x /usr/bin/quackjob

VOLUME ["/quackview/"]

ENTRYPOINT [ "/docker-entrypoint.sh"]

USER quackjob

CMD [ "touch /var/log/cron.log && tail -f /var/log/cron.log &" ]