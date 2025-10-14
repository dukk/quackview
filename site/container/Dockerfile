# Use official nginx image as base
FROM nginx:alpine

ARG SRC_DIR=../src

ENV DATA_DIR=/dashboard/data

COPY ${SRC_DIR}/ /usr/share/nginx/html/
RUN rm -rf /usr/share/nginx/html/data

RUN mkdir -p /dashboard/data && chown -R nginx:nginx /dashboard/data
COPY default.conf /etc/nginx/conf.d/default.conf

VOLUME ["/dashboard/data"]

EXPOSE 8008

CMD ["nginx", "-g", "daemon off;"]