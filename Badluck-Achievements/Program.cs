using Badluck_Achievements.Components;
using Components.Services_Achievements.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
SteamAchievementService steamAchievementService = new SteamAchievementService(builder.Configuration["ApiKeys:SteamApiKey"]!);
builder.Services.AddSingleton(steamAchievementService).
    AddSingleton(new BadluckAchievementsService(steamAchievementService, builder.Configuration["ApiKeys:SteamApiKey"]!, builder.Configuration["ApiKeys:NewsApiKey"]!));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Steam";
})
.AddCookie("Cookies")
.AddSteam(options =>
{
    options.ApplicationKey = builder.Configuration["ApiKeys:SteamApiKey"];
});

builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToFile("/Home.razor");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();