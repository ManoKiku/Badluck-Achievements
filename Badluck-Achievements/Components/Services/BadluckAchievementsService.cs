using Badluck_Achievements.Components.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Text;
using SteamWebAPI2.Interfaces;
using AngleSharp.Dom;
using Steam.Models.SteamCommunity;
using AngleSharp.Io;
using Badluck_Achievements.Components;
using System.Net.Http;
using System.Linq;

namespace Components.Services_Achievements.Components
{
    public class BadluckAchievementsService
    {
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly SteamAchievementService _steamService;
        public readonly List<Tuple<uint, string>> tags;

        private string steamApiKey;
        private string newsApiKey;


        public BadluckAchievementsService(IWebHostEnvironment webHostEnvironment, SteamAchievementService steamService, string steamApiKey, string newsApiKey) 
        {
            _hostingEnvironment = webHostEnvironment;
            _steamService = steamService;
            this.steamApiKey = steamApiKey;
            this.newsApiKey = newsApiKey;

			string tagsPath = Path.Combine(_hostingEnvironment.WebRootPath, "data", "tags.json");
            string json = File.ReadAllText(tagsPath);
			var parsed = JArray.Parse(json);

            tags = parsed.Select(x => new Tuple<uint, string>(x.Value<uint>("id"), x.Value<string>("name")!)).ToList();
		}

        public async Task<List<Tuple<DateTime, int>>?> LoadGameTimeAnalytics(uint steamId, HttpClient? httpClient = null)
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }

            var result = await httpClient.GetAsync($"https://steamcharts.com/app/{steamId}/chart-data.json");

            if (!result.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await result.Content.ReadAsStringAsync();
            Console.WriteLine(json);
            var parsed = JArray.Parse(json);
            return parsed
                .Select(x=> new Tuple<DateTime, int>(DateTimeOffset.FromUnixTimeSeconds(x[0].Value<long>() / 1000).UtcDateTime, x[1]!.Value<int>()))
                .Chunk(24)
                .Select(x => new Tuple<DateTime, int>(x.First().Item1, (int)x.Average(y => y.Item2)))
                .DistinctBy(x => x.Item1.DayOfYear)
                .TakeLast(14)
                .ToList();
        }

        public async Task<List<SteamGame>?> LoadGamesAsync(int amount = 8, List<uint>? tags = null, List<string>? os = null, string? query = null, uint? price = null, bool withAchievements = true, bool withPlayers = true, HttpClient? httpClient = null)
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }

            try
            {
                string baseUrl = "https://store.steampowered.com/search/results?category1=998&json=1";

                if(tags != null)
                {
                    baseUrl += $"&tags={string.Join(",", tags)}";
                }
                if(os != null)
                {
                    baseUrl += $"&os={string.Join(",", os)}";
                }
                if(query != null)
                {
                    baseUrl += $"&term={query}";
                }
                if(price != null)
                {
                    if(price <= 0)
                    {
                        baseUrl += "&maxprice=free";
                    }
                    else
                    {
                        baseUrl += $"&maxprice={price}";
                    }
                }

                var response = await httpClient.GetAsync(baseUrl);

                List<SteamGame> popularGames = new List<SteamGame>();

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var g = JObject.Parse(json);

                var requests = g["items"]!.Take(amount).Select(x =>
                {
                    Match match = Regex.Match(x.Value<string>("logo")!, @"/apps/(\d+)");
                    string? appID = string.Empty;
                    if (match.Success)
                    {
                        appID = match.Groups[1].Value;
                    }

                    return new
                    {
                        name = x.Value<string>("name") ?? "Unknown",
                        logo = x.Value<string>("logo") ?? "default.png",
                        achievmentTask = withAchievements ?
                            httpClient.GetAsync("http://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/?format=json&ignore_preferences=1&gameid=" + appID) :
                            null,
                        playerCountTask = withPlayers ?
                            _steamService.GetNumberOfCurrentPlayersForGameAsync(uint.Parse(appID)) :
                            null,
                        appId = uint.Parse(appID)
                    };

                }
                    ).ToList();

                if(withAchievements)
                    await Task.WhenAll(requests.Select(x => x.achievmentTask));

                if(withPlayers)
                    await Task.WhenAll(requests.Select(x => x.playerCountTask));

                foreach (var r in requests)
                {
                    string s = string.Empty;
                    JObject parsed = new JObject();
                    if (withAchievements)
                    {
                        s = await r.achievmentTask.Result.Content.ReadAsStringAsync();
                        parsed = JObject.Parse(s);
                    }
                        
                    uint achievementsCount = 0;
                    if (parsed.HasValues)
                    {
                        achievementsCount = (uint)parsed["achievementpercentages"]!["achievements"]!.Count();
                    }

                    uint playersCount = withPlayers ? r.playerCountTask.Result : 0;

                    popularGames.Add(new SteamGame(
                        r.name,
                        r.logo,
                        achievementsCount,
                        playersCount,
                        r.appId));
                }

                return popularGames;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<GamingNews>?> LoadGamingNewsAsync(HttpClient? httpClient = null)
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }

            try
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Badluck-Achievements");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var url = $"https://newsapi.org/v2/everything?q=gaming&apiKey={newsApiKey}&pageSize=2";


                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);

                List<GamingNews> news = parsed["articles"]!
                    .Select(article => new GamingNews(
                        article.Value<string>("title") ?? "No title",
                        article.Value<string>("description") ?? "No description",
                        article.Value<string>("url") ?? "#",
                        DateTime.ParseExact(article.Value<string>("publishedAt")!, "MM/dd/yyyy HH:mm:ss", null)
                )).Reverse().ToList();

                return news;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<SteamAchievement>?> LoadLatestAchievements(ulong steamId, HttpClient? httpClient = null)
        {
            if(httpClient == null)
            {
                httpClient = new HttpClient();
            }

            try
            {
                Random random = new Random();
                List<SteamAchievement> achievements = new List<SteamAchievement>();
                var stats = await _steamService.LoadUserStats(httpClient, steamId);

                if(stats.completedAchievements == 0)
                {
                    return null;
                }

                var games = stats.games.Where(x => x.completedAchievements != 0).ToList();

                ulong appId = games[random.Next(0, games.Count() - 1)].appId;
                achievements = stats.achievements
                    .Where(x => x.appId == appId)
                    .OrderBy(x => x.achievePercentage)
                    .Take(5)
                    .ToList();

                return achievements;
            }
            catch
            {
                return null;
            }
        }
    }
}
