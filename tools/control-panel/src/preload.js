const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('controlApi', {
  request(payload) {
    return ipcRenderer.invoke('gateway-request', payload);
  }
});
