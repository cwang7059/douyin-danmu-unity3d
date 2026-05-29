const state = {
  quantity: 1,
  userSerial: 1
};

const gatewayInput = document.querySelector('#gatewayUrl');
const statusPill = document.querySelector('.status-pill');
const statusText = document.querySelector('#statusText');
const logList = document.querySelector('#logList');
const giftValueInput = document.querySelector('#giftValue');

document.querySelector('#healthButton').addEventListener('click', checkHealth);
document.querySelector('#clearLog').addEventListener('click', () => {
  logList.replaceChildren();
});

document.querySelectorAll('.quantity').forEach(button => {
  button.addEventListener('click', () => {
    state.quantity = Number(button.dataset.quantity || 1);
    document.querySelectorAll('.quantity').forEach(item => item.classList.toggle('active', item === button));
  });
});

document.querySelectorAll('.action').forEach(button => {
  button.addEventListener('click', () => sendCommandFromButton(button));
});

document.querySelectorAll('.gift-button').forEach(button => {
  button.addEventListener('click', () => sendGift(button.dataset.gift));
});

window.addEventListener('DOMContentLoaded', checkHealth);

async function checkHealth() {
  try {
    await request('/health', 'GET');
    setStatus(true, '在线');
    pushLog('网关连接正常', true);
  } catch (error) {
    setStatus(false, '离线');
    pushLog(formatError(error), false);
  }
}

async function sendCommandFromButton(button) {
  const quantity = state.quantity;
  const createPayload = () => ({
    eventType: 'command',
    userId: nextUserId(),
    userName: 'Mobile Panel',
    team: button.dataset.team,
    commandType: button.dataset.type,
    key: button.dataset.key,
    value: Number(button.dataset.value || 1)
  });

  await repeatSend(quantity, () => request('/command', 'POST', createPayload()), button.innerText.replace(/\s+/g, ' '));
}

async function sendGift(team) {
  const giftValue = clampNumber(Number(giftValueInput.value || 1), 1, 9999);
  giftValueInput.value = giftValue;

  const createPayload = () => ({
    eventType: 'gift',
    userId: nextUserId(),
    userName: 'Mobile Gift',
    giftName: `${team} mobile panel gift`,
    giftValue
  });

  await repeatSend(state.quantity, () => request('/gift', 'POST', createPayload()), team === 'human' ? '人族礼物' : '怪物礼物');
}

async function repeatSend(quantity, sender, label) {
  let sent = 0;
  for (let index = 0; index < quantity; index += 1) {
    try {
      await sender();
      sent += 1;
      setStatus(true, '在线');
    } catch (error) {
      setStatus(false, '离线');
      pushLog(`${label} ${sent}/${quantity}，${formatError(error)}`, false);
      return;
    }
  }

  pushLog(`${label} ×${quantity}`, true);
}

function request(path, method, body) {
  return window.controlApi.request({
    baseUrl: gatewayInput.value,
    path,
    method,
    body
  });
}

function setStatus(online, text) {
  statusPill.dataset.status = online ? 'online' : 'offline';
  statusText.textContent = text;
}

function pushLog(message, ok) {
  const item = document.createElement('li');
  const time = new Date().toLocaleTimeString('zh-CN', { hour12: false });
  item.className = ok ? 'ok' : 'error';
  item.textContent = `${time}  ${message}`;
  logList.prepend(item);

  while (logList.children.length > 24) {
    logList.lastElementChild.remove();
  }
}

function nextUserId() {
  const id = `mobile-panel-${state.userSerial}`;
  state.userSerial += 1;
  return id;
}

function clampNumber(value, min, max) {
  if (!Number.isFinite(value)) {
    return min;
  }

  return Math.max(min, Math.min(max, Math.round(value)));
}

function formatError(error) {
  return error && error.message ? error.message : String(error);
}
