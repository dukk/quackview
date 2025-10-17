# Use a minimal Alpine base image
FROM alpine:3.20

RUN apk update && apk upgrade
RUN apk add --no-cache \
        ca-certificates \
        curl \
        cronie

ENV QUACKVIEW_DATA_DIR=/quackview/data
ENV QUACKVIEW_JOBS_DIR=/quackview/jobs

RUN mkdir -p /quackview/{jobs,bin,data}

RUN adduser -D -h /home/quackjob -s /bin/sh quackjob
RUN chown -R quackjob:quackjob /quackview

COPY dist/bin /quackview/bin
COPY container/crontab /etc/cron.d/quackview
COPY container/jobs/* /quackview/jobs

RUN chmod +x /quackview/bin/quackjob
RUN chmod 0644 /etc/cron.d/quackview

VOLUME ["/quackview/data", "/quackview/jobs"]

# Run cron in the foreground (as root so it can switch to the target user defined in /etc/cron.d)
CMD ["crond", "-f", "-l", "2"]