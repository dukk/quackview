# Use a minimal Alpine base image
FROM alpine:3.20

RUN apk update && apk upgrade
RUN apk add --no-cache \
        ca-certificates \
        curl \
        cronie

RUN mkdir -p /quackview/{config,jobs,bin,data,secrets,logs}/

# Set environment variables for QuackView directories
ENV QUACKVIEW_DIR=/quackview/

RUN adduser -D -h /home/quackjob -s /bin/sh quackjob

COPY container/jobs/* /quackview/jobs/
COPY container/secrets/* /quackview/secrets/
COPY container/data/* /quackview/data/

COPY dist/bin/appsettings.json /quackview/config/quackjob.json
COPY dist/bin/quackjob /usr/bin/quackjob
RUN chmod +x /usr/bin/quackjob

COPY container/startup.sh /quackview/config/startup.sh
RUN chmod +x /quackview/config/startup.sh

COPY container/crontab /etc/cron.d/quackview
RUN chmod 0644 /etc/cron.d/quackview

RUN chown -R quackjob:quackjob /quackview

VOLUME ["/quackview/"]

CMD ["sh", "-c", "/quackview/config/startup.sh"]