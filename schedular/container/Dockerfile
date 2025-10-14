# Use a minimal Alpine base image
FROM alpine:3.20

# Install cron daemon and tools
RUN apk add --no-cache \
        ca-certificates \
        curl \
        cronie

ENV DATA_DIR=/quackview/data
ENV JOBS_DIR=/quackview/jobs

COPY container/crontab /etc/cron.d/quackview-jobs
COPY container/jobs/ /quackview/jobs

# Create application user (system account without password)
RUN adduser -D -h /home/quackjob -s /bin/sh quackjob && \
    chown -R quackjob:quackjob /quackview

RUN mkdir -p /quackview/data && chown quackjob:quackjob /quackview/data && \
    mkdir -p /quackview/jobs && chown -R quackjob:quackjob /quackview/jobs && \
    ln -s /etc/cron.d/quackview-jobs /quackview/jobs/crontab && \
    chmod 0644 /etc/cron.d/quackview-jobs

VOLUME ["/quackview/data", "/quackview/jobs"]

# Run cron in the foreground (as root so it can switch to the target user defined in /etc/cron.d)
CMD ["crond", "-f", "-l", "2"]