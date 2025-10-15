# Use official nginx image as base
FROM nginx:alpine

# Everything is relative to the <repo>/display/ directory
ARG CONTEXT_ROOT=./

ENV QUACKVIEW_DATA_DIR=/quackview/data

COPY ${CONTEXT_ROOT}src/ /usr/share/nginx/html/
COPY ${CONTEXT_ROOT}container/nginx.conf /etc/nginx/conf.d/default.conf

RUN rm -rf /usr/share/nginx/html/data
RUN mkdir -p /quackview/data && chown -R nginx:nginx /quackview/data

# TODO: Install jobs once there is a package to install them from...

VOLUME ["/quackview/data"]

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]