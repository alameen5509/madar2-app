/**
 * OfflineStore.js — مدار 2
 * قاعدة بيانات محلية بـ IndexedDB للعمل بدون إنترنت
 *
 * Stores:
 *   pendingChanges  — تغييرات لم تُرفع للـ API بعد
 *   cachedNodes     — آخر نسخة محلية من بطاقات اللوحة
 *   offlineQueue    — طابور العمليات أثناء Offline
 */
const OfflineStore = (() => {
  const DB_NAME    = 'mdar-offline';
  const DB_VERSION = 1;

  let db = null;

  // ── فتح / تهيئة قاعدة البيانات ─────────────────────────────────────────
  async function init() {
    if (db) return db;
    return new Promise((resolve, reject) => {
      const req = indexedDB.open(DB_NAME, DB_VERSION);

      req.onupgradeneeded = e => {
        const d = e.target.result;

        // Store 1: pendingChanges — التغييرات المعلقة
        if (!d.objectStoreNames.contains('pendingChanges')) {
          const s = d.createObjectStore('pendingChanges', { keyPath: 'id', autoIncrement: true });
          s.createIndex('timestamp', 'timestamp', { unique: false });
          s.createIndex('synced',    'synced',    { unique: false });
        }

        // Store 2: cachedNodes — نسخة محلية من البطاقات
        if (!d.objectStoreNames.contains('cachedNodes')) {
          d.createObjectStore('cachedNodes', { keyPath: 'boardId' });
        }

        // Store 3: offlineQueue — طابور العمليات في وضع Offline
        if (!d.objectStoreNames.contains('offlineQueue')) {
          const q = d.createObjectStore('offlineQueue', { keyPath: 'id', autoIncrement: true });
          q.createIndex('timestamp', 'timestamp', { unique: false });
        }
      };

      req.onsuccess = e => { db = e.target.result; resolve(db); };
      req.onerror   = e => reject(e.target.error);
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────────
  function tx(storeName, mode = 'readonly') {
    return db.transaction(storeName, mode).objectStore(storeName);
  }
  const prom = req => new Promise((res, rej) => {
    req.onsuccess = e => res(e.target.result);
    req.onerror   = e => rej(e.target.error);
  });

  // ── pendingChanges ───────────────────────────────────────────────────────

  /** أضف تغييراً للقائمة المعلقة */
  async function addPending(change) {
    await init();
    return prom(tx('pendingChanges', 'readwrite').add({
      change,
      timestamp: Date.now(),
      synced:    0,
      retries:   0,
    }));
  }

  /** احصل على جميع التغييرات غير المُزامَنة */
  async function getPending() {
    await init();
    return new Promise((res, rej) => {
      const results = [];
      const req = tx('pendingChanges').index('synced').openCursor(IDBKeyRange.only(0));
      req.onsuccess = e => {
        const cursor = e.target.result;
        if (cursor) { results.push(cursor.value); cursor.continue(); }
        else res(results.sort((a, b) => a.timestamp - b.timestamp));
      };
      req.onerror = e => rej(e.target.error);
    });
  }

  /** ضع علامة "مُزامَن" على تغيير */
  async function markSynced(id) {
    await init();
    const store = tx('pendingChanges', 'readwrite');
    const item  = await prom(store.get(id));
    if (item) {
      item.synced = 1;
      await prom(store.put(item));
    }
  }

  /** احذف التغييرات المُزامَنة (تنظيف دوري) */
  async function clearSynced() {
    await init();
    return new Promise((res, rej) => {
      const store = tx('pendingChanges', 'readwrite');
      const req   = store.index('synced').openCursor(IDBKeyRange.only(1));
      req.onsuccess = e => {
        const cursor = e.target.result;
        if (cursor) { cursor.delete(); cursor.continue(); }
        else res();
      };
      req.onerror = e => rej(e.target.error);
    });
  }

  // ── cachedNodes ──────────────────────────────────────────────────────────

  /** احفظ نسخة من البطاقات بعد كل sync ناجح */
  async function saveSnapshot(boardId, nodes) {
    await init();
    return prom(tx('cachedNodes', 'readwrite').put({
      boardId,
      nodes,
      savedAt: Date.now(),
    }));
  }

  /** استرجع النسخة المحلية */
  async function getSnapshot(boardId) {
    await init();
    return prom(tx('cachedNodes').get(boardId));
  }

  // ── offlineQueue ─────────────────────────────────────────────────────────

  /** أضف عملية لطابور Offline */
  async function enqueue(operation, payload) {
    await init();
    return prom(tx('offlineQueue', 'readwrite').add({
      operation,
      payload,
      timestamp: Date.now(),
    }));
  }

  /** احصل على كل العمليات في الطابور مرتبة زمنياً */
  async function dequeueAll() {
    await init();
    return new Promise((res, rej) => {
      const results = [];
      const req = tx('offlineQueue').openCursor();
      req.onsuccess = e => {
        const cursor = e.target.result;
        if (cursor) { results.push(cursor.value); cursor.continue(); }
        else res(results.sort((a, b) => a.timestamp - b.timestamp));
      };
      req.onerror = e => rej(e.target.error);
    });
  }

  /** امسح الطابور بعد المزامنة الناجحة */
  async function clearQueue() {
    await init();
    return prom(tx('offlineQueue', 'readwrite').clear());
  }

  return {
    init,
    addPending, getPending, markSynced, clearSynced,
    saveSnapshot, getSnapshot,
    enqueue, dequeueAll, clearQueue,
  };
})();

window.OfflineStore = OfflineStore;
