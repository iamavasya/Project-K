// Serves the production build in www/ like a static host would: real files, else index.html.
// Used by the PWA e2e run (ng serve does not serve the service worker).
import { createReadStream, statSync } from 'node:fs';
import { createServer } from 'node:http';
import { extname, join, normalize } from 'node:path';

const root = new URL('../www/', import.meta.url).pathname;
const port = Number(process.env.PORT ?? 4300);
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

function resolve(urlPath) {
  const file = join(root, normalize(decodeURIComponent(urlPath)).replace(/^(\.\.[/\\])+/, ''));
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
  const file = resolve(new URL(req.url ?? '/', 'http://localhost').pathname);
  res.writeHead(200, {
    'Content-Type': types[extname(file)] ?? 'application/octet-stream',
    'Cache-Control': 'no-cache',
  });
  createReadStream(file).pipe(res);
}).listen(port, '127.0.0.1', () => console.log(`www on http://127.0.0.1:${port}`));
