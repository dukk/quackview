# Use a minimal base image with cron
FROM ubuntu:22.04

RUN add-apt-repository ppa:dotnet/backports && \
    apt-get update && \
    apt-get install -y \
        cron \
        dotnet-sdk-9.0 \
        ca-certificates \
        curl && \
    rm -rf /var/lib/apt/lists/*

ENV DATA_DIR=/quackview/data
ENV JOBS_DIR=/quackview/jobs

COPY crontab /etc/cron.d/quackview-jobs
COPY jobs/ /quackview/jobs

RUN useradd -m quackjob && \
    chown -R quackjob:quackjob /quackview

RUN mkdir -p /quackview/data && chown quackjob:quackjob /quackview/data
RUN mkdir -p /quackview/jobs && chown -R quackjob:quackjob /quackview/jobs
RUN ln -s /etc/cron.d/quackview-jobs /quackview/jobs/crontab
RUN chmod 0644 /etc/cron.d/quackview-jobs
RUN crontab /etc/cron.d/quackview-jobs

VOLUME ["/quackview/data", "/quackview/jobs"]

USER quackjob

CMD ["cron", "-f"]