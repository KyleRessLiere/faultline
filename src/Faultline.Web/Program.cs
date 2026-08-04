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
builder.Services.AddSingleton<FightFiles>();
builder.Services.AddSingleton<CustomFightStore>();
builder.Services.AddSingleton<SessionLog>();
builder.Services.AddSingleton<PlaytestNotes>();
builder.Services.AddSingleton<RunStore>();
builder.Services.AddSingleton<RunSession>();
builder.Services.AddSingleton<BoardAnimator>();

await builder.Build().RunAsync();
