# Quack View Setup

## Docker Compose

1. Paste the following configuration in a new `docker-compose.yml` file using your preferred editor. Configuration:

    ```docker
    services:
    display:
        image: ghcr.io/dukk/quackview-display:latest
        volumes:
        - ./data:/quackview/data
        ports:
        - "8008:80"
        depends_on:
        - scheduler

    scheduler:
        image: ghcr.io/dukk/quackview-scheduler:latest
        volumes:
        - ./data:/quackview/data
        - ./jobs:/quackview/jobs
    ```

2. Run `docker-compose up -d` to start the containers.
3. Browse to <http://localhost:8008/> to view the dashboard.
