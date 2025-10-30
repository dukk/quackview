# Data Watcher SSE

Small Node.js service that exposes a Server-Sent Events endpoint which streams file change events from a watched directory.

Features
- SSE at /events
- Health check at /health
- Configurable via env: PORT and WATCH_DIR

Quick start

1. Install dependencies:

```cmd
cd display\container\data-watcher-sse
npm install
```

2. Start service:

```cmd
npm start
```

3. Test SSE stream (PowerShell/curl are fine). Example with curl (cmd.exe):

```cmd
curl -N http://localhost:3000/events
```

4. Trigger file events by creating/modifying files in the `watched` folder inside this directory or set WATCH_DIR to another path.

Environment variables
- PORT - port to listen on (default 3000)
- WATCH_DIR - directory to watch (default ./watched)
