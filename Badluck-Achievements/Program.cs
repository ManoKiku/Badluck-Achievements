using AspNet.Security.OpenId.Steam;
using Badluck_Achievements.Components;
using Badluck_Achievements.Components.Pages;
using Components.Services_Achievements.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();

SteamAchievementService steamAchievementService = new SteamAchievementService(builder.Configuration["ApiKeys:SteamApiKey"]!);
builder.Services.AddSingleton(steamAchievementService).
    AddSingleton(new BadluckAchievementsService(builder.Environment, steamAchievementService, builder.Configuration["ApiKeys:SteamApiKey"]!, builder.Configuration["ApiKeys:NewsApiKey"]!));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = SteamAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie("Cookies")
.AddSteam(options =>
{
    options.CallbackPath = "/signin-steam";
    options.ApplicationKey = builder.Configuration["ApiKeys:SteamApiKey"];
});

builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();
app.MapFallbackToFile("/Routes.razor");

Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

app.Run();