# Use official nginx image as base
FROM nginx:alpine

ENV QUACKVIEW_DATA_DIR=/quackview/data
ENV WATCH_DIR=/quackview/data

COPY src/ /usr/share/nginx/html/
COPY container/nginx.conf /etc/nginx/conf.d/default.conf

# Install Node.js to run the SSE data watcher alongside nginx
RUN apk add --no-cache nodejs npm

# Copy and install the SSE watcher service
COPY container/data-watcher-sse /opt/data-watcher-sse
WORKDIR /opt/data-watcher-sse
RUN npm install --omit=dev
WORKDIR /

# Startup script to run both Node and nginx
COPY container/start.sh /start.sh
RUN chmod +x /start.sh

RUN rm -rf /usr/share/nginx/html/data
RUN mkdir -p /quackview/data && chown -R nginx:nginx /quackview/data

VOLUME ["/quackview/data"]

EXPOSE 80

CMD ["/start.sh"]