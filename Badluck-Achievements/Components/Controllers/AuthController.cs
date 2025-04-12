using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet("nonfile")]
    public async Task<IActionResult> Nonfile(string returnUrl = "/")
    {
        await HttpContext.SignOutAsync();
        return LocalRedirect(returnUrl);
    }
}