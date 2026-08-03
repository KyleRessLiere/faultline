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
builder.Services.AddSingleton<PlaytestView>();
builder.Services.AddSingleton<FightFiles>();
builder.Services.AddSingleton<CustomFightStore>();
builder.Services.AddSingleton<SessionLog>();
builder.Services.AddSingleton<PlaytestNotes>();
builder.Services.AddSingleton<RunStore>();
builder.Services.AddSingleton<RunSession>();
builder.Services.AddSingleton<BoardAnimator>();

await builder.Build().RunAsync();
