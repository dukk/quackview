# Use official nginx image as base
FROM nginx:alpine

ENV DATA_DIR=/quackview/data

COPY src/ /usr/share/nginx/html/
RUN rm -rf /usr/share/nginx/html/data

RUN mkdir -p /quackview/data && chown -R nginx:nginx /quackview/data
COPY container/nginx.conf /etc/nginx/conf.d/default.conf

VOLUME ["/quackview/data"]

EXPOSE 8008

CMD ["nginx", "-g", "daemon off;"]