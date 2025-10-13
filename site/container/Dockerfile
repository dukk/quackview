# Use official nginx image as base
FROM nginx:alpine

ARG SRC_DIR=../src
ARG DATA_SRC=../mock-data

ENV DATA_DIR=/data

COPY ${SRC_DIR}/ /usr/share/nginx/html/

RUN mkdir -p ${DATA_DIR} && chown -R nginx:nginx ${DATA_DIR}
COPY ${DATA_SRC}/ ${DATA_DIR}/
COPY default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]