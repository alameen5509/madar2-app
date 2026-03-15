/**
 * SyncController.js — مدار 2
 * محرك المزامنة الذكية مع Mdar.API
 *
 * 3 مستويات:
 *   Level 1 — Optimistic Updates: تطبيق فوري + إرسال خلفي + rollback عند الفشل
 *   Level 2 — Delta Sync:         إرسال التغييرات كل 30 ثانية
 *   Level 3 — Full Sync:          جلب كامل عند أول فتح للوحة
 *
 * يعمل بالتكامل مع OfflineStore.js لدعم وضع عدم الاتصال.
 *
 * الاستخدام:
 *   await MdarSync.init({ boardId, token, apiBase, sessionId, onRemoteChange, onConflict });
 *   MdarSync.queueChange({ type: 'node_moved', nodeId, x, y });
 *   MdarSync.disconnect();
 */
const MdarSync = (() => {

  // ── الحالة الداخلية ───────────────────────────────────────────────────────
  let cfg = {
    boardId:        null,
    token:          null,
    apiBase:        'https://hearty-adventure-production-aba1.up.railway.app/api',
    sessionId:      null,
    onRemoteChange: null,  // callback(change) عند استقبال تغيير من جلسة أخرى
    onConflict:     null,  // callback(local, remote) عند تعارض
    onStatusChange: null,  // callback('syncing'|'synced'|'offline'|'error')
  };

  let changeQueue  = [];   // Level 2: طابور التغييرات المنتظرة
  let lastSyncAt   = null; // آخر وقت مزامنة ناجحة
  let syncTimer    = null; // مؤقت الـ 30 ثانية
  let isOnline     = navigator.onLine;
  let hubConn      = null; // SignalR connection

  const SYNC_INTERVAL = 30_000; // 30 ثانية

  // ── التهيئة ───────────────────────────────────────────────────────────────

  async function init(config) {
    Object.assign(cfg, config);

    if (!cfg.sessionId) {
      cfg.sessionId = 'sess-' + Date.now() + '-' + Math.random().toString(36).slice(2, 8);
    }

    // تهيئة قاعدة البيانات المحلية
    await window.OfflineStore?.init();

    // Level 3: Full Sync عند الفتح
    await fullSync();

    // Level 2: Delta Sync كل 30 ثانية
    syncTimer = setInterval(flush, SYNC_INTERVAL);

    // مراقبة الاتصال
    window.addEventListener('online',  handleOnline);
    window.addEventListener('offline', handleOffline);

    // SignalR
    if (window.signalR) {
      await connectSignalR();
    }

    setStatus(isOnline ? 'synced' : 'offline');
  }

  // ── Level 3: Full Sync ────────────────────────────────────────────────────

  async function fullSync() {
    if (!cfg.boardId || !cfg.token || !isOnline) return;

    const since = lastSyncAt
      ? `?boardId=${cfg.boardId}&since=${lastSyncAt.toISOString()}`
      : `?boardId=${cfg.boardId}`;

    try {
      const res = await apiFetch(`/canvas/sync/pull${since}`, 'GET');
      if (!res.ok) return;
      const data = await res.json();

      // تطبيق التغييرات الواردة بترتيب الـ timestamp
      const changes = (data.changes ?? []).sort((a, b) =>
        new Date(a.timestamp) - new Date(b.timestamp)
      );

      for (const event of changes) {
        // تجاهل أحداث الجلسة الحالية
        if (event.sessionId === cfg.sessionId) continue;
        cfg.onRemoteChange?.(event);
      }

      lastSyncAt = new Date(data.serverTime ?? Date.now());

      // حفظ نسخة محلية
      await window.OfflineStore?.saveSnapshot(cfg.boardId, changes);
    } catch { /* offline — لا بأس */ }
  }

  // ── Level 1: Optimistic Update ────────────────────────────────────────────

  /**
   * تسجيل تغيير — يُضاف للطابور + يُحفظ في IndexedDB إذا كان offline
   * @param {object} change — { type, nodeId, ...data }
   * @param {Function} [rollback] — دالة التراجع عند فشل الـ API
   */
  async function queueChange(change, rollback = null) {
    const entry = {
      ...change,
      timestamp: new Date().toISOString(),
      rollback,
    };

    changeQueue.push(entry);

    // حفظ في IndexedDB كـ pending (للـ offline)
    const storedId = await window.OfflineStore?.addPending(entry);
    entry._storeId = storedId;

    // إذا كانت الشبكة متاحة وتراكمت تغييرات كثيرة → flush فوري
    if (isOnline && changeQueue.length >= 10) {
      flush();
    }
  }

  // ── Level 2: Delta Sync (flush) ───────────────────────────────────────────

  async function flush() {
    if (!isOnline || !cfg.boardId || !cfg.token) return;
    if (changeQueue.length === 0) return;

    const batch = [...changeQueue];
    changeQueue  = [];

    setStatus('syncing');

    try {
      const res = await apiFetch('/canvas/sync/push', 'POST', {
        boardId:    cfg.boardId,
        sessionId:  cfg.sessionId,
        lastSyncAt: lastSyncAt?.toISOString(),
        changes:    batch.map(({ rollback: _r, _storeId: _s, ...c }) => c),
      });

      if (!res.ok) throw new Error(`sync failed: ${res.status}`);

      const result = await res.json();
      lastSyncAt = new Date(result.serverTime ?? Date.now());

      // تحديث IndexedDB: ضع علامة synced
      for (const entry of batch) {
        if (entry._storeId != null) {
          await window.OfflineStore?.markSynced(entry._storeId);
        }
      }

      setStatus('synced');

      // SignalR: أرسل التغييرات لباقي الجلسات
      if (hubConn?.state === 'Connected') {
        for (const change of batch) {
          const { rollback: _r, _storeId: _s, ...c } = change;
          await hubConn.invoke('PushChange', cfg.boardId.toString(), c).catch(() => {});
        }
      }

    } catch {
      // Level 1 Rollback: تراجع عن كل تغيير فاشل
      for (const entry of batch) {
        entry.rollback?.();
      }
      // أعد التغييرات للطابور للمحاولة لاحقاً
      changeQueue = [...batch, ...changeQueue];
      setStatus('error');
    }
  }

  // ── Conflict Resolution ───────────────────────────────────────────────────

  /**
   * Last Write Wins — بناءً على timestamp.
   * إذا تعارض نص: يستدعي cfg.onConflict للمراجعة البشرية.
   */
  function resolveConflict(localChange, remoteChange) {
    const lt = new Date(localChange.timestamp);
    const rt = new Date(remoteChange.timestamp);

    // تعارض النصوص يحتاج مراجعة بشرية
    if (localChange.type === 'text_changed' && remoteChange.type === 'text_changed') {
      if (cfg.onConflict) {
        cfg.onConflict(localChange, remoteChange);
        return null; // ينتظر قرار المستخدم
      }
    }

    // Last Write Wins للباقي
    return lt >= rt ? localChange : remoteChange;
  }

  // ── SignalR ──────────────────────────────────────────────────────────────

  async function connectSignalR() {
    try {
      hubConn = new signalR.HubConnectionBuilder()
        .withUrl(`${cfg.apiBase.replace('/api', '')}/hubs/canvas`, {
          accessTokenFactory: () => cfg.token,
        })
        .withAutomaticReconnect([0, 1000, 5000, 10000])
        .build();

      // استقبال تغييرات الآخرين
      hubConn.on('RemoteChange', ({ change, changedBy, sessionId }) => {
        if (sessionId === cfg.sessionId) return; // تجاهل أحداثنا
        cfg.onRemoteChange?.({ ...change, changedBy });
      });

      hubConn.on('UserJoined', ({ userId }) => {
        console.info('[MdarSync] مستخدم انضم:', userId);
      });

      hubConn.onreconnecting(() => setStatus('syncing'));
      hubConn.onreconnected(() => {
        joinSession();
        setStatus('synced');
      });

      await hubConn.start();
      await joinSession();
    } catch (e) {
      console.warn('[MdarSync] SignalR غير متاح:', e.message);
    }
  }

  async function joinSession() {
    if (hubConn?.state === 'Connected' && cfg.boardId) {
      await hubConn.invoke('JoinSession', cfg.boardId.toString()).catch(() => {});
    }
  }

  async function disconnect() {
    clearInterval(syncTimer);
    await flush(); // flush أي تغييرات متبقية
    if (hubConn?.state === 'Connected') {
      await hubConn.invoke('LeaveSession', cfg.boardId?.toString()).catch(() => {});
      await hubConn.stop();
    }
    window.removeEventListener('online',  handleOnline);
    window.removeEventListener('offline', handleOffline);
  }

  // ── Online / Offline ─────────────────────────────────────────────────────

  async function handleOnline() {
    isOnline = true;
    setStatus('syncing');

    // مزامنة الطابور المعلق من IndexedDB
    const pending = await window.OfflineStore?.getPending() ?? [];
    if (pending.length > 0) {
      changeQueue = [
        ...pending.map(p => p.change),
        ...changeQueue,
      ];
    }

    await flush();
    await fullSync();
    await window.OfflineStore?.clearSynced();
  }

  function handleOffline() {
    isOnline = false;
    setStatus('offline');
  }

  // ── Utilities ────────────────────────────────────────────────────────────

  function setStatus(status) {
    cfg.onStatusChange?.(status);
  }

  function apiFetch(path, method, body) {
    return fetch(cfg.apiBase + path, {
      method,
      headers: {
        'Content-Type':  'application/json',
        'Authorization': 'Bearer ' + cfg.token,
      },
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  return {
    init,
    queueChange,
    flush,
    fullSync,
    resolveConflict,
    disconnect,
    get isOnline()   { return isOnline; },
    get lastSyncAt() { return lastSyncAt; },
  };
})();

window.MdarSync = MdarSync;
