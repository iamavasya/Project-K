// Serves the PWA build in www/ the way the web image's nginx does: under /m/, real files first,
// else the app shell, plus the web's /env.js. Used by the PWA e2e run (ng serve does not serve the
// service worker).
import { createReadStream, statSync } from 'node:fs';
import { createServer } from 'node:http';
import { extname, join, normalize } from 'node:path';

const root = new URL('../www/', import.meta.url).pathname;
const base = '/m/';
const port = Number(process.env.PORT ?? 4300);
// Stand-in for the env.js nginx renders from the container environment.
const envJs = `window.__PROJECTK_CONFIG__ = { apiUrl: ${JSON.stringify(process.env.PROJECTK_API_URL ?? 'https://api.example.test/api')} };\n`;
const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json',
  '.webmanifest': 'application/manifest+json',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.txt': 'text/plain; charset=utf-8',
};

function resolve(appPath) {
  const file = join(root, normalize(decodeURIComponent(appPath)).replace(/^(\.\.[/\\])+/, ''));
  try {
    if (file.startsWith(root) && statSync(file).isFile()) {
      return file;
    }
  } catch {
    // Not a file: fall through to the app shell.
  }
  return join(root, 'index.html');
}

createServer((req, res) => {
  const path = new URL(req.url ?? '/', 'http://localhost').pathname;
  if (path === '/env.js') {
    res.writeHead(200, { 'Content-Type': types['.js'], 'Cache-Control': 'no-store' });
    res.end(envJs);
    return;
  }
  if (path === '/' || path === '/m') {
    res.writeHead(302, { Location: base });
    res.end();
    return;
  }
  if (!path.startsWith(base)) {
    res.writeHead(404);
    res.end();
    return;
  }
  const file = resolve(path.slice(base.length));
  res.writeHead(200, {
    'Content-Type': types[extname(file)] ?? 'application/octet-stream',
    'Cache-Control': 'no-cache',
  });
  createReadStream(file).pipe(res);
}).listen(port, '127.0.0.1', () => console.log(`www on http://127.0.0.1:${port}${base}`));
