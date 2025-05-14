using AspNet.Security.OpenId.Steam;
using Badluck_Achievements.Components.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

[Route("api/auth")]
[ApiController]
public class AuthController : Controller
{
    private readonly AppDbContext _appDbContext;
    public AuthController(AppDbContext context)
    {
        _appDbContext = context;
    }

    [HttpGet("login")]
    public IActionResult Login(string returnUrl = "/")
    {
        return Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl },
            SteamAuthenticationDefaults.AuthenticationScheme
        );
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout(string returnUrl = "/")
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect(returnUrl);
    }

    [HttpGet("login-error")]
    public IActionResult LoginError(string errorMessage)
    {
        return RedirectToPage("/Error", new { Error = errorMessage });
    }
}