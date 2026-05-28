const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('node:path');
const http = require('node:http');
const https = require('node:https');

function createWindow() {
  const window = new BrowserWindow({
    width: 430,
    height: 820,
    minWidth: 360,
    minHeight: 680,
    title: '弹幕战手机控制台',
    autoHideMenuBar: true,
    backgroundColor: '#10151f',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  });

  window.loadFile(path.join(__dirname, 'index.html'));
}

app.whenReady().then(() => {
  ipcMain.handle('gateway-request', async (_event, payload) => requestGateway(payload));
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

function requestGateway(payload = {}) {
  const baseUrl = normalizeBaseUrl(payload.baseUrl);
  const requestPath = normalizePath(payload.path);
  const method = String(payload.method || 'GET').toUpperCase();
  const body = payload.body == null ? null : JSON.stringify(payload.body);
  const target = new URL(requestPath, `${baseUrl}/`);
  const client = target.protocol === 'https:' ? https : http;

  if (target.protocol !== 'http:' && target.protocol !== 'https:') {
    throw new Error('Only http/https gateway URLs are supported.');
  }

  const options = {
    method,
    hostname: target.hostname,
    port: target.port || undefined,
    path: `${target.pathname}${target.search}`,
    headers: {
      Accept: 'application/json'
    }
  };

  if (body != null) {
    options.headers['Content-Type'] = 'application/json; charset=utf-8';
    options.headers['Content-Length'] = Buffer.byteLength(body);
  }

  return new Promise((resolve, reject) => {
    const request = client.request(options, response => {
      const chunks = [];
      response.on('data', chunk => chunks.push(chunk));
      response.on('end', () => {
        const text = Buffer.concat(chunks).toString('utf8');
        let data = text;
        try {
          data = text ? JSON.parse(text) : null;
        } catch (_error) {
          data = text;
        }

        if (response.statusCode >= 200 && response.statusCode < 300) {
          resolve({ ok: true, status: response.statusCode, data });
          return;
        }

        reject(new Error(`HTTP ${response.statusCode}: ${text || response.statusMessage}`));
      });
    });

    request.setTimeout(2600, () => request.destroy(new Error('Gateway request timeout.')));
    request.on('error', reject);

    if (body != null) {
      request.write(body);
    }

    request.end();
  });
}

function normalizeBaseUrl(value) {
  const baseUrl = String(value || 'http://127.0.0.1:8765').trim().replace(/\/+$/, '');
  if (!baseUrl) {
    return 'http://127.0.0.1:8765';
  }

  return baseUrl;
}

function normalizePath(value) {
  const requestPath = String(value || '/health').trim();
  return requestPath.startsWith('/') ? requestPath : `/${requestPath}`;
}
