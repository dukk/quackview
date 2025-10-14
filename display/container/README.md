This container serves the site from /usr/share/nginx/html and exposes a data directory at /data via nginx.

Build-time options

- To build the image and include files from a directory in the build context as `/data` inside the image:

  docker build -t my-dashboard . --build-arg SRC_DIR=client/src --build-arg DATA_SRC=path/to/data

  Note: `DATA_SRC` must be a path relative to the build context (the directory you run `docker build` in).

Runtime mount (recommended)

- It's often better to mount a host directory into the container at runtime. For example:

  docker run -d -p 8001:80 -v "C:\\path\\to\\host\\data":/data:ro --name dashboard my-dashboard

  This will make the host directory available at http://localhost:8001/data/

Ports

- The container listens on port 80. The Dockerfile exposes 80; map it to a host port (e.g., 8001) using `-p HOSTPORT:80`.

Notes

- The nginx config enables `autoindex` on `/data` for convenience; remove or disable it if you don't want directory listings.
- If you used build-time copying of data, the files are copied into the image at build time and won't reflect later changes on the host unless you mount at runtime.
