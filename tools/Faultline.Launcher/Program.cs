using System.Diagnostics;
using System.Net;
using System.Text;

namespace Faultline.Launcher;

/// <summary>
/// Serves the published game out of the folder beside this executable and opens a browser at it.
/// </summary>
/// <remarks>
/// <para>
/// The whole point is that somebody who has never installed a developer tool can double-click one
/// file and play. Published self-contained, so the runtime travels inside the executable and there
/// is nothing to install; the console window it opens is the server, and closing it stops the game,
/// which is the only thing a player has to know.
/// </para>
/// <para>
/// A Blazor WebAssembly app is a folder of static files that needs <em>a</em> web server and does
/// not care which. <see cref="HttpListener"/> is in the base runtime, so this is a few hundred lines
/// rather than an ASP.NET dependency that would triple the download to serve a folder.
/// </para>
/// </remarks>
public static class Program
{
    private const string Root = "wwwroot";

    /// <summary>
    /// Content types the game is actually served with. Written out rather than guessed, because
    /// WebAssembly is the one the browser refuses to run when it is wrong: a <c>.wasm</c> file sent
    /// as <c>application/octet-stream</c> loads and then fails at instantiation, which reads as "the
    /// game is broken" rather than "a header is wrong".
    /// </summary>
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".mjs"] = "text/javascript; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".wasm"] = "application/wasm",
        [".webmanifest"] = "application/manifest+json",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".ico"] = "image/x-icon",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf",
        [".txt"] = "text/plain; charset=utf-8",
        [".map"] = "application/json; charset=utf-8",

        // The runtime's own payloads. Octet-stream is correct for all of them and the browser only
        // needs them to arrive intact.
        [".dll"] = "application/octet-stream",
        [".pdb"] = "application/octet-stream",
        [".dat"] = "application/octet-stream",
        [".blat"] = "application/octet-stream",
        [".bin"] = "application/octet-stream",
    };

    /// <summary>Entry point.</summary>
    /// <param name="args">Optional port, for the rare case where the chosen one is taken.</param>
    /// <returns>Zero on a clean exit.</returns>
    public static int Main(string[] args)
    {
        Console.Title = "Faultline";
        Console.OutputEncoding = Encoding.UTF8;

        var root = Path.Combine(AppContext.BaseDirectory, Root);
        if (!Directory.Exists(root))
        {
            return Fail(
                "The game files are missing.",
                "This program expects a '" + Root + "' folder sitting beside it. If you unzipped",
                "only the executable, unzip the whole folder again and keep the files together.");
        }

        int port = args.Length > 0 && int.TryParse(args[0], out int chosen) ? chosen : FreePort();
        var address = "http://localhost:" + port + "/";

        HttpListener listener;
        try
        {
            listener = new HttpListener();
            listener.Prefixes.Add(address);
            listener.Start();
        }
        catch (HttpListenerException error)
        {
            return Fail(
                "Could not start the game's local server.",
                "Windows reported: " + error.Message,
                "Another program may be using the port. Try again, or run this file with a",
                "different number after it, like:  Faultline 5200");
        }

        Banner(address);
        Open(address);

        // One request at a time is plenty: the only client is the browser on this machine, and a
        // queue nobody is waiting in costs nothing.
        while (listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = listener.GetContext();
            }
            catch (HttpListenerException)
            {
                break;
            }

            try
            {
                Serve(context, root);
            }
            catch (Exception error) when (error is IOException or HttpListenerException)
            {
                // The browser hung up mid-response, which happens on every reload. Not a fault.
            }
        }

        return 0;
    }

    private static void Serve(HttpListenerContext context, string root)
    {
        var relative = Uri.UnescapeDataString(context.Request.Url?.AbsolutePath ?? "/").TrimStart('/');
        var file = Resolve(root, relative);

        if (file is null)
        {
            // Every unknown path is the app's own routing, not a missing file: /play and /notes are
            // pages the game draws, and the browser asks this server for them on a refresh. Handing
            // back index.html is what makes a reloaded page work instead of 404ing.
            file = Path.Combine(root, "index.html");
            if (!File.Exists(file))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }
        }

        var bytes = File.ReadAllBytes(file);
        context.Response.ContentType = ContentTypes.TryGetValue(Path.GetExtension(file), out var type)
            ? type
            : "application/octet-stream";

        // Nothing is cached. A player who is handed a new copy of the folder must not be served the
        // old game out of their browser, and there is no bandwidth to save on localhost.
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        context.Response.Close();
    }

    /// <summary>
    /// The file a request names, or <c>null</c> when it names none. Refuses anything that would
    /// climb out of the served folder.
    /// </summary>
    private static string? Resolve(string root, string relative)
    {
        if (relative.Length == 0)
        {
            relative = "index.html";
        }

        var full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

        // A request is allowed to name files inside the folder and nothing else. This server only
        // ever talks to one browser on one machine, but a path that escapes the folder is a bug
        // whoever is running it should not have to think about.
        var fence = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(fence, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return File.Exists(full) ? full : null;
    }

    private static int FreePort()
    {
        // Asking the operating system for a free port beats picking one and hoping: a second copy of
        // the game, or anything else already on 5137, would otherwise fail at startup.
        var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private static void Open(string address)
    {
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // No browser, no default, or a locked-down machine. The address is on screen either way,
            // and telling somebody to paste a link is a far better failure than crashing.
        }
    }

    private static void Banner(string address)
    {
        Console.WriteLine();
        Console.WriteLine("  FAULTLINE");
        Console.WriteLine("  ---------");
        Console.WriteLine();
        Console.WriteLine("  The game is running at  " + address);
        Console.WriteLine("  Your browser should have opened it. If not, paste that address in.");
        Console.WriteLine();
        Console.WriteLine("  >> Leave this window open while you play. Closing it stops the game. <<");
        Console.WriteLine();
    }

    private static int Fail(params string[] lines)
    {
        Console.WriteLine();
        foreach (var line in lines)
        {
            Console.WriteLine("  " + line);
        }

        Console.WriteLine();
        Console.WriteLine("  Press Enter to close.");
        Console.ReadLine();
        return 1;
    }
}
