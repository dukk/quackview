# Use official nginx image as base
FROM nginx:alpine

ENV QUACKVIEW_DATA_DIR=/quackview/data

COPY src/ /usr/share/nginx/html/
COPY container/nginx.conf /etc/nginx/conf.d/default.conf

RUN rm -rf /usr/share/nginx/html/data
RUN mkdir -p /quackview/data && chown -R nginx:nginx /quackview/data

VOLUME ["/quackview/data"]

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]