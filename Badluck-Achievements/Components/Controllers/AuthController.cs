using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Steam.Models.DOTA2;

[Route("api/auth")]
public class AuthController : Controller
{
    [HttpGet("login")]
    public IActionResult Login(string scheme, string returnUrl = "/")
    {
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, scheme);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout(string returnUrl = "/")
    {
        await HttpContext.SignOutAsync();
        return LocalRedirect(returnUrl);
    }

    [HttpGet("login-error")]
    public async Task<IActionResult> LoginError(string returnUrl = "/")
    {
        await HttpContext.SignOutAsync();
        return LocalRedirect(returnUrl);
    }

    [HttpGet("nonfile")]
    public async Task<IActionResult> Nonfile(string returnUrl = "/")
    {
        await HttpContext.SignOutAsync();
        return LocalRedirect(returnUrl);
    }
}