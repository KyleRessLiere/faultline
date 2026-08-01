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
        const label = description || 'Faultline fight';

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
        storageRemove: storageRemove
    };
})();
