using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Faultline.Web;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<GameSession>(
    sp => new GameSession(sp.GetRequiredService<SessionLog>()));
// Given browser storage so that how the board is being looked at survives a reload. View only —
// nothing it remembers can change a rule, a legal command or a replay.
builder.Services.AddSingleton<PlaytestView>(
    sp => new PlaytestView(sp.GetRequiredService<FightFiles>()));
builder.Services.AddSingleton<DevPanelState>(
    sp => new DevPanelState(sp.GetRequiredService<FightFiles>()));
builder.Services.AddSingleton<ActionSpotlight>();
// Which contextual surface is over the board — inspector, expanded ability card, consumable card or
// the expanded order. Exactly one may be open, and that rule lives in one object so it cannot be
// enforced differently by four components.
builder.Services.AddSingleton<BattleSurfaces>();
builder.Services.AddSingleton<FightFiles>();
builder.Services.AddSingleton<CustomFightStore>();

// Saved test loadouts. A singleton so a build survives navigating between the picker and a board,
// which is the whole point of saving one.
builder.Services.AddSingleton<LoadoutStore>();
builder.Services.AddSingleton<SessionLog>();
builder.Services.AddSingleton<PlaytestNotes>();
builder.Services.AddSingleton<RunStore>();
builder.Services.AddSingleton<RunSession>();
builder.Services.AddSingleton<BoardAnimator>();
// Every message about the game as a whole goes here, and there is nowhere else for one to go: the
// battle screen reserves no row for text between the turn-order strip and the board.
builder.Services.AddSingleton<SystemToasts>();
// Every sitting on disk, with no setting and no prompt. A page cannot write a path, so it posts to
// whichever local host is serving it; when nothing answers, this is silently inert and the log lives
// in memory as it always did.
builder.Services.AddSingleton<PlaytestLogHost>();
builder.Services.AddSingleton<PlaytestSessionLog>();

var host = builder.Build();

// Not awaited: finding the log host is a probe over the network and the game must not wait on one to
// draw its first frame. Play that happens before the probe lands is not lost — the logger reads the
// transcript by cursor, so its first pump picks up everything that already happened.
_ = host.Services.GetRequiredService<PlaytestSessionLog>()
    .StartAsync(builder.HostEnvironment.BaseAddress);

await host.RunAsync();
