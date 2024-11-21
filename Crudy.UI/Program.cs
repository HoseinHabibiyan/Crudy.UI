using Blazored.LocalStorage;
using Crudy.UI;
using Crudy.UI.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#region Identity

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
builder.Services.AddScoped(sp => (IAuthService)sp.GetRequiredService<AuthenticationStateProvider>());
builder.Services.AddHttpClient(
        "Auth",
        opt => opt.BaseAddress = new Uri(builder.Configuration["ApiUrl"]!))
    .AddHttpMessageHandler<CookieHandler>();
#endregion

builder.Services.AddHttpClient("default", opt => opt.BaseAddress = new Uri(builder.Configuration["ApiUrl"]!));
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
