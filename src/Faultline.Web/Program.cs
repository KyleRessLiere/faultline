using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Faultline.Web;
using Faultline.Web.Shell;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<GameSession>();
builder.Services.AddSingleton<FightFiles>();
builder.Services.AddSingleton<CustomFightStore>();
builder.Services.AddSingleton<PlaytestNotes>();

await builder.Build().RunAsync();
