/**
 * thinking-board.js
 * مساحة التفكير اللانهائية — Infinite Thinking Space
 *
 * الميزات:
 *  - لوحة لانهائية 8000×8000 مع تحريك (Pan) وتكبير (Zoom)
 *  - إضافة بطاقات من الشريط الجانبي أو بالنقر المزدوج على اللوحة
 *  - تحرير العنوان والمحتوى مباشرةً على البطاقة (contenteditable)
 *  - سحب البطاقات وتغيير حجمها مع حفظ تلقائي في قاعدة البيانات
 *  - أنواع بطاقات: ملاحظة، فكرة، مهمة، سؤال
 */

const ThinkingBoard = (() => {

  // ── إعدادات أنواع البطاقات ──────────────────────────────────────────────────
  const CARD_TYPES = {
    0: { icon: '📝', label: 'ملاحظة', bg: '#1e293b', border: '#475569' },
    1: { icon: '💡', label: 'فكرة',   bg: '#7c2d12', border: '#c2410c' },
    2: { icon: '✅', label: 'مهمة',   bg: '#0c4a6e', border: '#0369a1' },
    3: { icon: '❓', label: 'سؤال',   bg: '#2e1065', border: '#7c3aed' }
  };

  // ── الحالة ─────────────────────────────────────────────────────────────────
  const s = {
    boardId:       null,
    selectedType:  0,
    pan:           { x: 500, y: 300 },
    scale:         1,
    isPanning:     false,
    panAnchor:     { x: 0, y: 0 },      // clientXY at mousedown + current pan
    dragging:      null,                 // { el, cardId, offX, offY }
    resizing:      null,                 // { el, cardId, startX, startY, startW, startH }
    saveTimers:    {},                   // cardId → timer
  };

  // ── مراجع DOM ──────────────────────────────────────────────────────────────
  let viewport, canvas, saveEl, zoomEl, titleEl;

  // ── API ────────────────────────────────────────────────────────────────────
  const getToken  = ()  => localStorage.getItem('mdar_token');
  const saveToken = (t) => localStorage.setItem('mdar_token', t);

  async function api(path, opts = {}) {
    const res = await fetch('/api' + path, {
      ...opts,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${getToken()}`,
        ...(opts.headers || {})
      }
    });
    if (res.status === 401)  { showAuth(); return null; }
    if (res.status === 204 || res.status === 404) return null;
    return res.ok ? res.json() : null;
  }

  // ── التحويل ────────────────────────────────────────────────────────────────
  /** تطبيق الإزاحة والتكبير على اللوحة */
  function applyTransform() {
    canvas.style.transform =
      `translate(${-s.pan.x * s.scale}px, ${-s.pan.y * s.scale}px) scale(${s.scale})`;
    zoomEl.textContent = Math.round(s.scale * 100) + '%';
  }

  /** تحويل إحداثيات المؤشر (clientXY) إلى إحداثيات اللوحة */
  function clientToCanvas(cx, cy) {
    const r = viewport.getBoundingClientRect();
    return {
      x: (cx - r.left)  / s.scale + s.pan.x,
      y: (cy - r.top)   / s.scale + s.pan.y
    };
  }

  /** مركز منطقة العرض الحالية بإحداثيات اللوحة */
  function viewportCenter() {
    const r = viewport.getBoundingClientRect();
    return clientToCanvas(r.left + r.width / 2, r.top + r.height / 2);
  }

  // ── Auth ───────────────────────────────────────────────────────────────────
  function showAuth() {
    document.getElementById('auth-overlay').classList.remove('hidden');
  }
  function hideAuth() {
    document.getElementById('auth-overlay').classList.add('hidden');
  }

  // ── شاشة اختيار اللوحات ──────────────────────────────────────────────────
  async function showBoardsScreen() {
    const screen = document.getElementById('boards-screen');
    screen.classList.remove('hidden');

    const boards = await api('/thinking-boards');
    const list   = document.getElementById('boards-list');
    list.innerHTML = '';

    if (!boards || boards.length === 0) {
      list.innerHTML = '<p style="color:#64748b;font-size:13px;text-align:center">لا توجد لوحات بعد — أنشئ واحدة!</p>';
      return;
    }

    boards.forEach(b => {
      const el = document.createElement('div');
      el.className = 'board-item';
      el.innerHTML = `
        <span class="board-item-title">${esc(b.title)}</span>
        <span class="board-item-meta">${b.cardCount} بطاقة</span>
      `;
      el.onclick = () => {
        screen.classList.add('hidden');
        loadBoard(b.id);
      };
      list.appendChild(el);
    });
  }

  async function createBoard() {
    const input = document.getElementById('new-board-title');
    const title = input.value.trim() || 'لوحتي';
    const board  = await api('/thinking-boards', {
      method: 'POST',
      body: JSON.stringify({ title })
    });
    if (board) {
      document.getElementById('boards-screen').classList.add('hidden');
      loadBoard(board.id);
    }
  }

  // ── تحميل اللوحة ──────────────────────────────────────────────────────────
  async function loadBoard(boardId) {
    showLoading();
    s.boardId = boardId;

    const board = await api(`/thinking-boards/${boardId}`);
    if (!board) { hideLoading(); showBoardsScreen(); return; }

    titleEl.textContent = board.title;
    document.title = `${board.title} — مساحة التفكير`;
    window.history.replaceState({}, '', `?boardId=${boardId}`);

    canvas.innerHTML = '';
    (board.cards || []).forEach(renderCard);

    hideLoading();
  }

  // ── رسم البطاقات ──────────────────────────────────────────────────────────
  function renderCard(data) {
    const type = CARD_TYPES[data.cardType] ?? CARD_TYPES[0];

    const el = document.createElement('div');
    el.className     = 'thinking-card';
    el.dataset.cardId = data.id;
    el.style.cssText  = `
      left:       ${data.positionX}px;
      top:        ${data.positionY}px;
      width:      ${data.width}px;
      min-height: ${data.height}px;
      background: ${type.bg};
      border-color: ${type.border};
    `;

    el.innerHTML = `
      <div class="card-header">
        <span class="card-type-icon">${type.icon}</span>
        <button class="card-del-btn" title="حذف البطاقة">✕</button>
      </div>
      <div class="card-title"   contenteditable="true" spellcheck="false">${esc(data.title)}</div>
      <div class="card-content" contenteditable="true" spellcheck="false">${esc(data.content)}</div>
      <div class="card-resize"></div>
    `;

    bindCardEvents(el, data.id);
    canvas.appendChild(el);
    return el;
  }

  function bindCardEvents(el, cardId) {
    // ── سحب البطاقة ───────────────────────────────────────────────────
    el.addEventListener('mousedown', (e) => {
      const tag = e.target;
      if (tag.isContentEditable)               return;
      if (tag.classList.contains('card-del-btn'))  return;
      if (tag.classList.contains('card-resize'))   return;
      e.stopPropagation(); e.preventDefault();
      selectCard(el);
      const r = el.getBoundingClientRect();
      s.dragging = {
        el, cardId,
        offX: (e.clientX - r.left)  / s.scale,
        offY: (e.clientY - r.top)   / s.scale
      };
    });

    // ── حذف ───────────────────────────────────────────────────────────
    el.querySelector('.card-del-btn').addEventListener('click', (e) => {
      e.stopPropagation();
      deleteCard(cardId, el);
    });

    // ── تحرير محتوى — حفظ عند فقدان التركيز ──────────────────────────
    const titleDiv   = el.querySelector('.card-title');
    const contentDiv = el.querySelector('.card-content');

    titleDiv.addEventListener('blur',    () => scheduleContentSave(cardId, el));
    contentDiv.addEventListener('blur',  () => scheduleContentSave(cardId, el));
    titleDiv.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') { e.preventDefault(); contentDiv.focus(); }
    });

    // ── تغيير الحجم ───────────────────────────────────────────────────
    el.querySelector('.card-resize').addEventListener('mousedown', (e) => {
      e.stopPropagation(); e.preventDefault();
      selectCard(el);
      const r = el.getBoundingClientRect();
      s.resizing = {
        el, cardId,
        startX: e.clientX, startY: e.clientY,
        startW: r.width  / s.scale,
        startH: parseFloat(el.style.minHeight) || r.height / s.scale
      };
    });

    // منع فقاعة النقر للوحة
    el.addEventListener('click', (e) => e.stopPropagation());
  }

  function selectCard(el) {
    document.querySelectorAll('.thinking-card.selected')
      .forEach(c => c.classList.remove('selected'));
    el.classList.add('selected');
  }

  // ── إنشاء بطاقة ──────────────────────────────────────────────────────────
  async function addCard(clientX, clientY) {
    if (!s.boardId) return;

    let pos;
    if (clientX !== undefined && clientY !== undefined) {
      pos = clientToCanvas(clientX, clientY);
      pos.x -= 110; pos.y -= 80;
    } else {
      const c = viewportCenter();
      const offset = canvas.childElementCount * 18 % 200;
      pos = { x: c.x - 110 + offset, y: c.y - 80 + offset };
    }

    const req = {
      title:     'فكرة جديدة',
      content:   '',
      cardType:  s.selectedType,
      positionX: Math.max(0, pos.x),
      positionY: Math.max(0, pos.y),
      width:     220,
      height:    160
    };

    showSaving();
    const data = await api(`/thinking-boards/${s.boardId}/cards`, {
      method: 'POST', body: JSON.stringify(req)
    });

    if (data) {
      showSaved();
      const el = renderCard(data);
      selectCard(el);
      setTimeout(() => {
        const t = el.querySelector('.card-title');
        t.focus();
        const range = document.createRange();
        range.selectNodeContents(t);
        window.getSelection().removeAllRanges();
        window.getSelection().addRange(range);
      }, 40);
    }
  }

  // ── حذف بطاقة ────────────────────────────────────────────────────────────
  async function deleteCard(cardId, el) {
    el.style.transition = 'opacity 0.2s, transform 0.2s';
    el.style.opacity    = '0';
    el.style.transform  = 'scale(0.9)';
    await api(`/thinking-boards/${s.boardId}/cards/${cardId}`, { method: 'DELETE' });
    el.remove();
    toast('تم حذف البطاقة');
  }

  // ── حفظ تلقائي ───────────────────────────────────────────────────────────
  function scheduleContentSave(cardId, el) {
    clearTimeout(s.saveTimers[cardId]);
    s.saveTimers[cardId] = setTimeout(() => saveContent(cardId, el), 700);
  }

  async function saveContent(cardId, el) {
    const title   = el.querySelector('.card-title').textContent.trim();
    const content = el.querySelector('.card-content').textContent.trim();
    showSaving();
    await api(`/thinking-boards/${s.boardId}/cards/${cardId}`, {
      method: 'PATCH', body: JSON.stringify({ title, content })
    });
    showSaved();
  }

  async function savePosition(cardId, el) {
    showSaving();
    await api(`/thinking-boards/${s.boardId}/cards/${cardId}`, {
      method: 'PATCH', body: JSON.stringify({
        positionX: parseFloat(el.style.left)      || 0,
        positionY: parseFloat(el.style.top)       || 0,
        width:     parseFloat(el.style.width)     || 220,
        height:    parseFloat(el.style.minHeight) || 160,
      })
    });
    showSaved();
  }

  // ── مؤشر الحفظ ───────────────────────────────────────────────────────────
  let savedTimer;
  function showSaving() { saveEl.textContent = 'جاري الحفظ...'; saveEl.className = 'save-indicator saving'; }
  function showSaved()  {
    saveEl.textContent = '● تم الحفظ'; saveEl.className = 'save-indicator saved';
    clearTimeout(savedTimer);
    savedTimer = setTimeout(() => { saveEl.textContent = ''; saveEl.className = 'save-indicator'; }, 2500);
  }

  // ── Toast ─────────────────────────────────────────────────────────────────
  let toastTimer;
  function toast(msg) {
    const el = document.getElementById('toast');
    el.textContent = msg; el.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => el.classList.remove('show'), 2500);
  }

  // ── Loading ───────────────────────────────────────────────────────────────
  function showLoading() { document.getElementById('loading-overlay').classList.remove('hidden'); }
  function hideLoading() { document.getElementById('loading-overlay').classList.add('hidden'); }

  // ── ربط الأحداث ──────────────────────────────────────────────────────────
  function bindEvents() {

    // ─ تحريك اللوحة (Pan) ─────────────────────────────────────────────
    viewport.addEventListener('mousedown', (e) => {
      if (e.button !== 0) return;
      s.isPanning  = true;
      s.panAnchor  = { x: e.clientX + s.pan.x * s.scale, y: e.clientY + s.pan.y * s.scale };
      viewport.classList.add('panning');
    });

    document.addEventListener('mousemove', (e) => {
      // ── سحب بطاقة ──
      if (s.dragging) {
        const p = clientToCanvas(e.clientX, e.clientY);
        s.dragging.el.style.left = `${Math.max(0, p.x - s.dragging.offX)}px`;
        s.dragging.el.style.top  = `${Math.max(0, p.y - s.dragging.offY)}px`;
        return;
      }
      // ── تغيير حجم بطاقة ──
      if (s.resizing) {
        const dx  = (e.clientX - s.resizing.startX) / s.scale;
        const dy  = (e.clientY - s.resizing.startY) / s.scale;
        const nW  = Math.max(180, s.resizing.startW + dx);
        const nH  = Math.max(120, s.resizing.startH + dy);
        s.resizing.el.style.width      = `${nW}px`;
        s.resizing.el.style.minHeight  = `${nH}px`;
        return;
      }
      // ── تحريك اللوحة ──
      if (s.isPanning) {
        s.pan.x = (s.panAnchor.x - e.clientX) / s.scale;
        s.pan.y = (s.panAnchor.y - e.clientY) / s.scale;
        applyTransform();
      }
    });

    document.addEventListener('mouseup', () => {
      if (s.dragging) {
        savePosition(s.dragging.cardId, s.dragging.el);
        s.dragging = null;
      }
      if (s.resizing) {
        savePosition(s.resizing.cardId, s.resizing.el);
        s.resizing = null;
      }
      if (s.isPanning) {
        s.isPanning = false;
        viewport.classList.remove('panning');
      }
    });

    // ─ تكبير/تصغير بعجلة الماوس ──────────────────────────────────────
    viewport.addEventListener('wheel', (e) => {
      e.preventDefault();
      const factor   = e.deltaY > 0 ? 0.92 : 1.08;
      const newScale = Math.min(3, Math.max(0.15, s.scale * factor));
      const r        = viewport.getBoundingClientRect();
      const mx = e.clientX - r.left, my = e.clientY - r.top;
      s.pan.x += mx / s.scale - mx / newScale;
      s.pan.y += my / s.scale - my / newScale;
      s.scale  = newScale;
      applyTransform();
    }, { passive: false });

    // ─ نقرة مزدوجة على اللوحة الفارغة → إضافة بطاقة ─────────────────
    viewport.addEventListener('dblclick', (e) => {
      if (e.target !== viewport && e.target !== canvas) return;
      addCard(e.clientX, e.clientY);
    });

    // ─ إلغاء التحديد عند النقر على الفراغ ────────────────────────────
    viewport.addEventListener('click', (e) => {
      if (e.target === viewport || e.target === canvas)
        document.querySelectorAll('.thinking-card.selected')
          .forEach(c => c.classList.remove('selected'));
    });

    // ─ حذف بالـ Delete / Backspace ────────────────────────────────────
    document.addEventListener('keydown', (e) => {
      if (document.activeElement?.isContentEditable) return;
      if (e.key !== 'Delete' && e.key !== 'Backspace')  return;
      const sel = document.querySelector('.thinking-card.selected');
      if (sel) deleteCard(sel.dataset.cardId, sel);
    });

    // ─ أزرار الشريط الجانبي ───────────────────────────────────────────
    document.getElementById('add-card-btn').addEventListener('click', () => addCard());

    document.getElementById('zoom-in-btn').addEventListener('click', () => {
      s.scale = Math.min(3, s.scale * 1.25); applyTransform();
    });
    document.getElementById('zoom-out-btn').addEventListener('click', () => {
      s.scale = Math.max(0.15, s.scale / 1.25); applyTransform();
    });
    document.getElementById('zoom-reset-btn').addEventListener('click', () => {
      s.scale = 1; s.pan = { x: 500, y: 300 }; applyTransform();
    });

    document.querySelectorAll('.type-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('.type-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        s.selectedType = parseInt(btn.dataset.type, 10);
      });
    });

    // ─ Auth ───────────────────────────────────────────────────────────
    document.getElementById('login-btn').addEventListener('click', () => {
      const token = document.getElementById('token-input').value.trim();
      if (token) { saveToken(token); hideAuth(); startApp(); }
    });
    document.getElementById('token-input').addEventListener('keydown', (e) => {
      if (e.key === 'Enter') document.getElementById('login-btn').click();
    });

    // ─ إنشاء لوحة جديدة ──────────────────────────────────────────────
    document.getElementById('create-board-btn').addEventListener('click', createBoard);
    document.getElementById('new-board-title').addEventListener('keydown', (e) => {
      if (e.key === 'Enter') createBoard();
    });
  }

  // ── بدء التطبيق ──────────────────────────────────────────────────────────
  async function startApp() {
    applyTransform();

    const params  = new URLSearchParams(window.location.search);
    const boardId = params.get('boardId');

    if (boardId) {
      await loadBoard(boardId);
      return;
    }

    // لا يوجد boardId في الرابط — حمّل أول لوحة أو اعرض شاشة الاختيار
    const boards = await api('/thinking-boards');
    if (boards && boards.length > 0) {
      await loadBoard(boards[0].id);
    } else {
      // إنشاء اللوحة الأولى تلقائياً
      const newBoard = await api('/thinking-boards', {
        method: 'POST', body: JSON.stringify({ title: 'لوحتي الأولى' })
      });
      if (newBoard) await loadBoard(newBoard.id);
      else          await showBoardsScreen();
    }
  }

  // ── Init ──────────────────────────────────────────────────────────────────
  async function init() {
    viewport = document.getElementById('board-viewport');
    canvas   = document.getElementById('board-canvas');
    saveEl   = document.getElementById('save-indicator');
    zoomEl   = document.getElementById('zoom-level');
    titleEl  = document.getElementById('board-title-display');

    bindEvents();

    if (!getToken()) { showAuth(); return; }
    await startApp();
  }

  // ── مساعد HTML Escape ─────────────────────────────────────────────────────
  function esc(str) {
    if (!str) return '';
    return str
      .replace(/&/g,  '&amp;')
      .replace(/</g,  '&lt;')
      .replace(/>/g,  '&gt;')
      .replace(/"/g,  '&quot;');
  }

  return { init };
})();

document.addEventListener('DOMContentLoaded', ThinkingBoard.init);
