const express = require('express');
const fs = require('fs');
const path = require('path');
const cors = require('cors');
const chokidar = require('chokidar');

const app = express();
app.use(cors());

const PORT = process.env.PORT || 3000;
const WATCH_DIR = process.env.WATCH_DIR || path.join(__dirname, 'watched');
const USE_POLLING = /^(1|true)$/i.test(process.env.CHOKIDAR_USEPOLLING || '');
const POLL_INTERVAL = Number.parseInt(process.env.CHOKIDAR_INTERVAL || '500', 10);
const AWAIT_WRITE = /^(1|true)$/i.test(process.env.CHOKIDAR_AWAIT_WRITE_FINISH || '');
const STABILITY = Number.parseInt(process.env.CHOKIDAR_STABILITY || '500', 10);
const POLLWRITE = Number.parseInt(process.env.CHOKIDAR_POLLWRITE || '100', 10);

if (!fs.existsSync(WATCH_DIR)) {
  fs.mkdirSync(WATCH_DIR, { recursive: true });
}

app.get('/health', (req, res) => res.json({ ok: true, watchDir: WATCH_DIR }));

app.get('/events', (req, res) => {
  res.set({
    'Content-Type': 'text/event-stream',
    'Cache-Control': 'no-cache',
    Connection: 'keep-alive',
  });
  res.flushHeaders && res.flushHeaders();

  const stamp = () => { const d = new Date(); return { ts: d.getTime() }; };
  res.write(`event: connected\ndata: ${JSON.stringify({ watchPath: '/data/', ...stamp() })}\n\n`);

  const watcher = chokidar.watch(WATCH_DIR, {
    ignoreInitial: false,
    persistent: true,
    usePolling: USE_POLLING,
    interval: POLL_INTERVAL,
    awaitWriteFinish: AWAIT_WRITE ? { stabilityThreshold: STABILITY, pollInterval: POLLWRITE } : false,
  });

  const send = (type, filePath) => {
    if (!filePath || path.extname(filePath).toLowerCase() !== '.json') return;
    const payload = { eventType: type, path: filePath, rel: path.relative(WATCH_DIR, filePath), ...stamp() };
    try { res.write(`event: file-change\ndata: ${JSON.stringify(payload)}\n\n`); } catch (_) {}
  };

  watcher
    .on('add', p => send('add', p))
    .on('change', p => send('change', p))
    .on('unlink', p => send('unlink', p))
    .on('addDir', p => send('addDir', p))
    .on('unlinkDir', p => send('unlinkDir', p))
    .on('error', err => {
      try { res.write(`event: error\ndata: ${JSON.stringify({ message: String(err) })}\n\n`); } catch (_) {}
    })
    .on('ready', () => {
      try { res.write(`event: ready\ndata: ${JSON.stringify(stamp())}\n\n`); } catch (_) {}
    });

  const ping = setInterval(() => {
    try {
      res.write(`event: ping\ndata: ${JSON.stringify(stamp())}\n\n`);
    } catch (err) {}
  }, 20_000);

  req.on('close', () => {
    clearInterval(ping);
    try { watcher.close(); } catch (e) {}
  });
});

app.listen(PORT, () => {
  console.log(`Data watcher SSE listening on http://0.0.0.0:${PORT}/ events -> /events`);
  console.log(`Watching directory: ${WATCH_DIR}`);
});
