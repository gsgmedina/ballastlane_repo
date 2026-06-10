using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaskManager.Web;
using TaskManager.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Base address of the Task Manager API (configurable via wwwroot/appsettings.json).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7179";

// ---- Auth ----
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

// ---- API clients (typed HttpClients with a bearer-token handler) ----
builder.Services.AddScoped<BearerTokenHandler>();
builder.Services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<TaskApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

await builder.Build().RunAsync();
