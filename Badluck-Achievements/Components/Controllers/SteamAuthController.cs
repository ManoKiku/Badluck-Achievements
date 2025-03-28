using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Badluck_Achievements.Components.Controllers
{
    [Route("steam-auth")]
    public class SteamAuthController : Controller
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public SteamAuthController(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var parameters = new Dictionary<string, string>
            {
                {"openid.ns", "http://specs.openid.net/auth/2.0"},
                {"openid.mode", "checkid_setup"},
                {"openid.return_to", $"{baseUrl}/steam-auth/login-callback"},
                {"openid.realm", baseUrl},
                {"openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select"},
                {"openid.identity", "http://specs.openid.net/auth/2.0/identifier_select"}
            };

            var steamUrl = "https://steamcommunity.com/openid/login?" +
                          string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

            return Redirect(steamUrl);
        }

        [HttpGet("login-callback")]
        public async Task<IActionResult> LoginCallback()
        {
            var query = Request.Query;
            var validationResult = await ValidateOpenIdResponse(query);

            if (!validationResult.Success) return Redirect("/login-error");

            var steamId = validationResult.ClaimedId.Split('/').Last();
            var userInfo = await GetSteamUserInfo(steamId);

            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, steamId),
            new(ClaimTypes.Name, userInfo.PersonaName),
            new("Avatar", userInfo.AvatarFullUrl)
        };

            await HttpContext.SignInAsync(
                new ClaimsPrincipal(new ClaimsIdentity(claims, "Steam")),
                new AuthenticationProperties { IsPersistent = true });

            return Redirect("/");
        }

        private async Task<OpenIdValidationResult> ValidateOpenIdResponse(IQueryCollection query)
        {
            var validationParams = new Dictionary<string, string> { { "openid.mode", "check_authentication" } };
            foreach (var key in query.Keys.Where(k => k.StartsWith("openid.")))
            {
                validationParams[key] = query[key];
            }

            var response = await _httpClient.PostAsync(
                "https://steamcommunity.com/openid/login",
                new FormUrlEncodedContent(validationParams));

            var content = await response.Content.ReadAsStringAsync();
            return new OpenIdValidationResult
            {
                Success = content.Contains("is_valid:true"),
                ClaimedId = query["openid.claimed_id"]
            };
        }

        private async Task<SteamUser> GetSteamUserInfo(string steamId)
        {
            var apiKey = _config["ApiKeys:SteamApiKey"];
            var response = await _httpClient.GetFromJsonAsync<SteamApiResponse>(
                $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids={steamId}");

            return response.Response.Players.First();
        }
    }
    public class OpenIdValidationResult
    {
        public bool Success { get; set; }
        public string ClaimedId { get; set; }
    }

    public class SteamApiResponse
    {
        public PlayerList Response { get; set; }
    }

    public class PlayerList
    {
        public List<SteamUser> Players { get; set; }
    }

    public class SteamUser
    {
        public string SteamId { get; set; }
        public string PersonaName { get; set; }
        public string AvatarFullUrl { get; set; }
    }
}
