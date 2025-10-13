# Use a minimal base image with cron
FROM ubuntu:22.04

RUN apt-get update && \
    apt-get install -y \
        cron \
        python3 \
        python3-pip \
        nodejs \
        npm \
        golang-go \
        dotnet-sdk-6.0 \
        ca-certificates \
        curl && \
    rm -rf /var/lib/apt/lists/*

ENV DATA_DIR=/data
ENV JOBS_DIR=/jobs

RUN useradd -m cronuser
RUN mkdir -p /data && chown cronuser:cronuser /data
RUN mkdir -p /jobs && chown -R cronuser:cronuser /jobs

COPY crontab /etc/cron.d/jobs

RUN chmod 0644 /etc/cron.d/jobs
RUN crontab /etc/cron.d/jobs

VOLUME ["/data", "/jobs"]

USER cronuser

CMD ["cron", "-f"]