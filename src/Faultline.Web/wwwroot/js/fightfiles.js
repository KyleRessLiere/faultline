// Saving a .fight file and remembering custom scenarios. The browser is the only thing here that
// can touch a real directory or persist across a refresh, so this is the whole of that surface —
// everything above it is C#. Nothing in this file knows a rule; it moves text around.

window.faultlineFiles = (function () {
    'use strict';

    // Chromium ships showSaveFilePicker; Firefox and Safari do not. The UI asks first so the
    // "save into a folder" button is never offered when it could only fail.
    function canSaveToDirectory() {
        return typeof window.showSaveFilePicker === 'function';
    }

    // The scenario creator saves .fight; the combat log saves .log. Both are plain text through the
    // same picker, so the extension and its label are arguments rather than two near-identical
    // functions. Omitting them keeps the original .fight behaviour.
    async function saveToDirectory(suggestedName, text, extension, description) {
        if (!canSaveToDirectory()) {
            return 'unsupported';
        }

        const ext = extension || '.fight';
        const label = description || 'PLUCK fight';

        try {
            const handle = await window.showSaveFilePicker({
                suggestedName: suggestedName,
                types: [{ description: label, accept: { 'text/plain': [ext] } }]
            });

            const writable = await handle.createWritable();
            await writable.write(text);
            await writable.close();
            return 'saved:' + (handle.name || suggestedName);
        } catch (err) {
            // A cancelled picker throws AbortError. That is a normal outcome, not a failure.
            if (err && (err.name === 'AbortError' || err.code === 20)) {
                return 'cancelled';
            }

            return 'error:' + ((err && err.message) || 'unknown error');
        }
    }

    function download(fileName, text) {
        try {
            const blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
            const url = URL.createObjectURL(blob);
            const anchor = document.createElement('a');
            anchor.href = url;
            anchor.download = fileName;
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);

            // Revoking immediately can race the download in some builds; a tick is enough.
            setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
            return 'downloaded:' + fileName;
        } catch (err) {
            return 'error:' + ((err && err.message) || 'unknown error');
        }
    }

    // Clipboard first, then a hidden textarea for browsers that refuse navigator.clipboard outside a
    // secure context. Either way this reports a status string; it never throws back into C#.
    async function copyText(text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return 'copied';
            }
        } catch (err) {
            // Fall through to the textarea path rather than failing outright.
        }

        try {
            const area = document.createElement('textarea');
            area.value = text;
            area.setAttribute('readonly', '');
            area.style.position = 'fixed';
            area.style.left = '-9999px';
            document.body.appendChild(area);
            area.select();
            const ok = document.execCommand('copy');
            document.body.removeChild(area);
            return ok ? 'copied' : 'error:the browser refused the copy';
        } catch (err) {
            return 'error:' + ((err && err.message) || 'unknown error');
        }
    }

    // ---- the note folder ---------------------------------------------------------------------
    //
    // A directory the player picks once, which notes are then written into as they are typed. This
    // is the only way a page can put a file somewhere real without asking every time: a directory
    // handle survives in IndexedDB, and the browser re-grants it on request rather than re-picking.
    // localStorage cannot hold one — a handle is a structured-clonable object, not a string.

    const HANDLE_DB = 'faultline';
    const HANDLE_STORE = 'handles';
    const HANDLE_KEY = 'noteFolder';

    function canUseNoteFolder() {
        return typeof window.showDirectoryPicker === 'function';
    }

    function handleStore(mode) {
        return new Promise(function (resolve, reject) {
            const open = window.indexedDB.open(HANDLE_DB, 1);
            open.onupgradeneeded = function () {
                open.result.createObjectStore(HANDLE_STORE);
            };
            open.onerror = function () { reject(open.error); };
            open.onsuccess = function () {
                const tx = open.result.transaction(HANDLE_STORE, mode);
                resolve({ store: tx.objectStore(HANDLE_STORE), db: open.result });
            };
        });
    }

    function idbGet(key) {
        return handleStore('readonly').then(function (ctx) {
            return new Promise(function (resolve) {
                const req = ctx.store.get(key);
                req.onsuccess = function () { resolve(req.result || null); };
                req.onerror = function () { resolve(null); };
            });
        }).catch(function () { return null; });
    }

    function idbPut(key, value) {
        return handleStore('readwrite').then(function (ctx) {
            return new Promise(function (resolve) {
                const req = value === null ? ctx.store.delete(key) : ctx.store.put(value, key);
                req.onsuccess = function () { resolve(true); };
                req.onerror = function () { resolve(false); };
            });
        }).catch(function () { return false; });
    }

    // Chrome drops write permission between page loads. Asking inside a click gesture succeeds
    // silently for a directory the user already chose; asking outside one is what fails, which is
    // why the C# side only calls this from a button and on the first note.
    async function grant(handle, prompt) {
        if (!handle || typeof handle.queryPermission !== 'function') {
            return false;
        }

        const options = { mode: 'readwrite' };
        if (await handle.queryPermission(options) === 'granted') {
            return true;
        }

        return prompt && await handle.requestPermission(options) === 'granted';
    }

    async function pickNoteFolder() {
        if (!canUseNoteFolder()) {
            return 'unsupported';
        }

        try {
            const handle = await window.showDirectoryPicker({ id: 'faultline-notes', mode: 'readwrite' });
            if (!await grant(handle, true)) {
                return 'denied';
            }

            await idbPut(HANDLE_KEY, handle);
            return 'picked:' + (handle.name || 'folder');
        } catch (err) {
            if (err && (err.name === 'AbortError' || err.code === 20)) {
                return 'cancelled';
            }

            return 'error:' + ((err && err.message) || 'unknown error');
        }
    }

    // The remembered folder's name, or '' when there is none or the grant has lapsed. Never prompts:
    // a page that asked for permission on load would be asking before the player did anything.
    async function noteFolderName() {
        try {
            const handle = await idbGet(HANDLE_KEY);
            if (!handle) {
                return '';
            }

            return await grant(handle, false) ? (handle.name || 'folder') : '';
        } catch (err) {
            return '';
        }
    }

    async function forgetNoteFolder() {
        await idbPut(HANDLE_KEY, null);
        return 'forgotten';
    }

    // Writes one file, creating every folder on the way. Segments are folder names in order; the
    // C# side builds them, so nothing here decides what a session is called.
    async function writeNoteFile(segments, fileName, text) {
        try {
            const root = await idbGet(HANDLE_KEY);
            if (!root) {
                return 'nofolder';
            }

            if (!await grant(root, true)) {
                return 'denied';
            }

            let dir = root;
            for (const segment of segments || []) {
                dir = await dir.getDirectoryHandle(segment, { create: true });
            }

            const file = await dir.getFileHandle(fileName, { create: true });
            const writable = await file.createWritable();
            await writable.write(text);
            await writable.close();

            return 'wrote:' + (segments || []).concat([fileName]).join('/');
        } catch (err) {
            return 'error:' + ((err && err.message) || 'unknown error');
        }
    }

    // Wall-clock time in US Eastern, whatever the machine's own zone is, with the abbreviation the
    // date actually falls under — EST in winter, EDT in summer. The browser carries the whole tz
    // database; .NET in WebAssembly may not, so the clock is asked here and formatted in C#.
    function easternNow() {
        const now = new Date();
        const parts = {};
        const format = new Intl.DateTimeFormat('en-US', {
            timeZone: 'America/New_York',
            year: 'numeric', month: '2-digit', day: '2-digit',
            hour: '2-digit', minute: '2-digit', second: '2-digit',
            hour12: false, timeZoneName: 'short'
        });

        for (const part of format.formatToParts(now)) {
            parts[part.type] = part.value;
        }

        // Midnight formats as hour 24 in some engines; the date has already rolled, so it is 00.
        const hour = parts.hour === '24' ? '00' : parts.hour;
        return parts.year + '-' + parts.month + '-' + parts.day
            + '\t' + hour + '-' + parts.minute + '-' + parts.second
            + '\t' + (parts.timeZoneName || 'ET');
    }

    function storageGet(key) {
        try {
            return window.localStorage.getItem(key);
        } catch (err) {
            return null;
        }
    }

    function storageSet(key, value) {
        try {
            window.localStorage.setItem(key, value);
            return true;
        } catch (err) {
            return false;
        }
    }

    function storageRemove(key) {
        try {
            window.localStorage.removeItem(key);
            return true;
        } catch (err) {
            return false;
        }
    }

    return {
        canSaveToDirectory: canSaveToDirectory,
        saveToDirectory: saveToDirectory,
        download: download,
        copyText: copyText,
        storageGet: storageGet,
        storageSet: storageSet,
        storageRemove: storageRemove,
        canUseNoteFolder: canUseNoteFolder,
        pickNoteFolder: pickNoteFolder,
        noteFolderName: noteFolderName,
        forgetNoteFolder: forgetNoteFolder,
        writeNoteFile: writeNoteFile,
        easternNow: easternNow
    };
})();
