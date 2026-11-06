using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Nxt.UI;
using NxtShift;
using NxtShift.Web;
using NxtShift.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Arabic first (the family default); English when the browser prefers it.
builder.Services.AddScoped(sp => new StoredLocale(
    sp.GetRequiredService<LocalStore>(), sp.GetRequiredService<IJSRuntime>(), [FixedLocale.Arabic, FixedLocale.English]));
builder.Services.AddScoped<INxtLocale>(sp => sp.GetRequiredService<StoredLocale>());

builder.Services.AddNxtUi(o =>
{
    o.App = NxtAppId.Shift;
    o.LogoUrl = "logo.png";
    o.SettingsHref = "settings";
    o.Description = new()
    {
        ["ar"] = "سجّل دخولك بنقرة، وتذكّر تسجيل خروجك عند انتهاء نوبتك.",
        ["en"] = "Punch in with one tap and get reminded to punch out when your shift is over."
    };
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IShiftStorage, BrowserShiftStorage>();
builder.Services.AddScoped<ShiftStore>();
builder.Services.AddScoped<ShiftText>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<ReminderService>();

await builder.Build().RunAsync();
