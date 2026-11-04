(window => {
    'use strict';

    const DB_NAME = 'OpenBibleStore';
    const STORE = 'kv';
    let dbPromise = null;

    function open() {
        if (!dbPromise) {
            dbPromise = new Promise((resolve, reject) => {
                const request = indexedDB.open(DB_NAME, 1);
                request.onupgradeneeded = () => {
                    if (!request.result.objectStoreNames.contains(STORE)) {
                        request.result.createObjectStore(STORE);
                    }
                };
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => { dbPromise = null; reject(request.error); };
            });
        }
        return dbPromise;
    }

    async function run(mode, action) {
        const db = await open();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE, mode);
            const request = action(tx.objectStore(STORE));
            tx.oncomplete = () => resolve(request.result === undefined ? null : request.result);
            tx.onerror = () => reject(tx.error);
            tx.onabort = () => reject(tx.error);
        });
    }

    window.openBibleStore = {
        get: key => run('readonly', s => s.get(key)),
        set: (key, value) => run('readwrite', s => s.put(value, key)).then(() => null),
        remove: key => run('readwrite', s => s.delete(key)).then(() => null)
    };
})(window);
