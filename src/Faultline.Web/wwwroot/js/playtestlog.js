// Posts the session log to whichever local host is serving the game, so a sitting lands on disk
// without anybody choosing a folder.
//
// The buffer lives here rather than in C# for one reason: the last flush. A tab being closed gives a
// page a `pagehide` and no time to await anything, and `navigator.sendBeacon` is the only send that
// is guaranteed to survive it. C# hands lines over as they happen and never waits; this decides when
// they leave.
//
// Everything is a POST of `text/plain` to a query-string URL, which keeps it a CORS *simple* request
// — no preflight, so a host only has to answer the POST itself. The sidecar answers with
// `Access-Control-Allow-Origin: *`; the launcher is same-origin and needs nothing.
//
// WHEN NOTHING ANSWERS, THIS KEEPS LOOKING AND SAYS SO. It used to go quiet forever on the argument
// that a plain static file server is a legitimate way to run the game — true, but it made "logging is
// on" and "logging is reaching disk" look identical from inside the app, and a whole evening of play
// was lost to a server started the wrong way with nothing on screen to say so. The probe now repeats
// on a slow timer, so a launcher started AFTER the tab is adopted without a reload, and `state()`
// reports `searching` so a surface can show it (D-245).
window.faultlinePlaytestLog = (() => {
  const PING = 'playtest/log/ping';
  const WRITE = 'playtest/log';
  const EXPECT = 'faultline-log';
  const FLUSH_MS = 2000;
  const PROBE_MS = 5000;   // How often to look again while no host has answered.

  let base = null;      // Resolved host, with trailing slash. '' is never used; null means "no host".
  let started = false;
  let date = '';
  let file = '';

  let hosts = [];       // Candidates to keep probing while `base` is null.
  let probe = null;     // The re-probe timer; cleared the moment a host answers.

  let pending = [];
  let queued = 0;       // Characters waiting.
  let written = 0;      // Flushes that landed.
  let failed = 0;       // Flushes that did not.
  let timer = null;
  let inFlight = false;

  const url = () =>
    `${base}${WRITE}?date=${encodeURIComponent(date)}&file=${encodeURIComponent(file)}`;

  // A host is one that says so. Any other 200 — a static server handing back index.html for an
  // unknown path, which is exactly what this app's own hosts do — is not a host, and treating it as
  // one would post the whole session into a void that answers cheerfully.
  async function answers(candidate) {
    try {
      const response = await fetch(`${candidate}${PING}`, { cache: 'no-store' });
      if (!response.ok) return false;
      return (await response.text()).startsWith(EXPECT);
    } catch {
      return false;
    }
  }

  async function send(text) {
    try {
      const response = await fetch(url(), {
        method: 'POST',
        headers: { 'Content-Type': 'text/plain;charset=UTF-8' },
        body: text,
        keepalive: true,
      });
      return response.ok;
    } catch {
      return false;
    }
  }

  async function flush() {
    if (!base || inFlight || pending.length === 0) return;

    // Taken out of the buffer before the await, so lines arriving mid-flight queue for the next one
    // rather than being sent twice or dropped between the send and the clear.
    const batch = pending.join('');
    pending = [];
    queued = 0;
    inFlight = true;

    const ok = await send(batch);
    inFlight = false;

    if (ok) {
      written++;
    } else {
      // Put it back at the front: a host that restarted mid-session should pick up where it left
      // off, and a log with a hole in it is worse than one that arrives late.
      failed++;
      pending.unshift(batch);
      queued += batch.length;
    }
  }

  // The unload path. No await is possible here, so this is a beacon and its outcome is unknowable —
  // which is the correct trade against losing the tail of a fight.
  function flushNow() {
    if (!base || pending.length === 0) return;
    const batch = pending.join('');
    pending = [];
    queued = 0;
    try {
      navigator.sendBeacon(url(), new Blob([batch], { type: 'text/plain;charset=UTF-8' }));
      written++;
    } catch {
      failed++;
    }
  }

  // One sweep of the candidates. Stops the re-probe the moment somebody answers, and flushes
  // immediately so everything buffered since the tab opened lands at once.
  async function look() {
    for (const candidate of hosts) {
      if (await answers(candidate)) {
        base = candidate;
        if (probe) {
          clearInterval(probe);
          probe = null;
        }

        flush();
        return true;
      }
    }

    return false;
  }

  return {
    // Finds a host and starts the clock. `candidates` are tried in order and the first that answers
    // wins, so same-origin beats the sidecar and a launcher can never be shadowed by a stale one.
    async start(candidates, forDate, forFile) {
      if (started) return base || '';
      started = true;
      date = forDate;
      file = forFile;

      hosts = candidates || [];

      // The sinks are wired before a host is found, so lines buffer from the first one rather than
      // being dropped while the probe runs. Nothing is lost by a launcher that starts late.
      timer = setInterval(flush, FLUSH_MS);
      addEventListener('pagehide', flushNow);
      addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') flushNow();
      });

      await look();
      if (!base) {
        probe = setInterval(look, PROBE_MS);
      }

      return base || '';
    },

    // The hot path: synchronous, allocation only, never awaited by the caller.
    push(text) {
      if (!started || !text) return;
      pending.push(text);
      queued += text.length;

      // A long fight can outrun the timer. Flushing early on a full-ish buffer keeps a single POST
      // from growing without bound.
      if (queued > 65536) flush();
    },

    flush() {
      return flush();
    },

    state() {
      // One sweep of the candidates. Stops the re-probe the moment somebody answers, and flushes
  // immediately so everything buffered since the tab opened lands at once.
  async function look() {
    for (const candidate of hosts) {
      if (await answers(candidate)) {
        base = candidate;
        if (probe) {
          clearInterval(probe);
          probe = null;
        }

        flush();
        return true;
      }
    }

    return false;
  }

  return { base: base || '', queued, written, failed };
    },
  };
})();
