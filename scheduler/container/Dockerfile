FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine

RUN apk update && apk upgrade
RUN apk add --no-cache \
        ca-certificates \
        curl \
        cronie \
        nano \
        rsync

ENV QUACKVIEW_DIR=/quackview/

RUN adduser -D -h /home/quackjob -s /bin/sh quackjob

COPY container/scripts/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

RUN mkdir -p /docker-entrypoint.d/
COPY container/scripts/docker-entrypoint.d/ /docker-entrypoint.d/
RUN chmod +x /docker-entrypoint.d/*.sh

COPY container/template/ /quackview-template/
COPY dist/bin/appsettings.json /quackview-template/config/quackjob.json
COPY dist/bin/* /usr/share/quackjob/
RUN chown -R quackjob:quackjob /quackview-template
RUN echo "dotnet /usr/share/quackjob/quackjob.dll \$@" > /usr/bin/quackjob && \
    chmod +x /usr/bin/quackjob

VOLUME ["/quackview/"]

ENTRYPOINT [ "/docker-entrypoint.sh"]

RUN mkdir -p /quackview/logs && \
    touch /quackview/logs/cron.log && \
    chown -R quackjob:quackjob /quackview/logs