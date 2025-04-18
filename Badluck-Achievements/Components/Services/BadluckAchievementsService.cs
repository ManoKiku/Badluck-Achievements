using Badluck_Achievements.Components.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace Components.Services_Achievements.Components
{
    public class BadluckAchievementsService
    {
        private readonly SteamAchievementService _steamService;

        private string steamApiKey;
        private string newsApiKey;


        public BadluckAchievementsService(SteamAchievementService steamService, string steamApiKey, string newsApiKey) 
        {
            _steamService = steamService;
            this.steamApiKey = steamApiKey;
            this.newsApiKey = newsApiKey;
        }

        public async Task<List<SteamGame>?> LoadPopularGamesAsync(HttpClient httpClient, int amount = 8)
        {
            try
            {
                var response = await httpClient.GetAsync("https://store.steampowered.com/search/results?category1=998&json=1");

                List<SteamGame> popularGames = new List<SteamGame>();

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);

                await Parallel.ForEachAsync(parsed["items"]!.Take(amount), async (item, ct) =>
                {
                    try
                    {
                        Match match = Regex.Match(item.Value<string>("logo")!, @"/apps/(\d+)");
                        string? appID = string.Empty;
                        if (match.Success)
                        {
                            appID = match.Groups[1].Value;
                        }

                        var url = $"http://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/?gameid={appID}&format=json";

                        var response = await httpClient.GetAsync(url);

                        uint achievementsCount = 0;
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var parsed = JObject.Parse(json);
                            achievementsCount = (uint)parsed["achievementpercentages"]!["achievements"]!.Count();
                        }

                        uint playersCount = await _steamService.
                        GetNumberOfCurrentPlayersForGameAsync(uint.Parse(appID));

                        popularGames.Add(new SteamGame(
                            item.Value<string>("name") ?? "Unknown",
                            item.Value<string>("logo") ?? "default.png",
                            achievementsCount,
                            playersCount));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                });

                return popularGames;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<GamingNews>?> LoadGamingNewsAsync(HttpClient httpClient)
        {
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

                List<GamingNews> news = parsed["articles"]?
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

        public async Task<List<SteamAchievement>?> LoadLatestAchievements(HttpClient httpClient, ulong steamId)
        {
            try
            {
                Random random = new Random();
                List<SteamAchievement> achievements = new List<SteamAchievement>();
                var games = await _steamService.GetPlayerGames(steamId);

                StringBuilder builder = new StringBuilder($"https://api.steampowered.com/IPlayerService/GetTopAchievementsForGames/v1/?key={steamApiKey}&steamid={steamId}&max_achievements=6");

                for (int i = 0; i < games.Count(); ++i)
                {
                    builder.Append($"&appids[{i}]={games[i].appID}");
                }

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Badluck-Achievements");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await httpClient.GetAsync(builder.ToString());

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                if (!json.Contains("\"achievements\""))
                {
                    return null;
                }

                var parsed = JObject.Parse(json);

                while (achievements.Where(x => x.isAchieved).Count() == 0)
                {
                    int index = random.Next(0, games.Count() - 1);
                    if (parsed["response"]?["games"]?[index] is JObject gamesObj && gamesObj.ContainsKey("achievements"))
                    {
                        achievements = await _steamService.GetGameAchievementsAsync(
                            steamId,
                            games[index].appID);
                    }
                }

                achievements = achievements
                    .Where(x => x.isAchieved)
                    .OrderBy(x => x.unlockTime)
                    .Take(5)
                    .Reverse()
                    .ToList();

                return achievements;
            }
            catch
            {
                return null;
            }
        }

        public async Task<UserStats?> LoadUserStats(HttpClient httpClient, ulong steamId)
        {
            try
            {
                UserStats? stats = new UserStats();
                 var games = await _steamService.GetPlayerGames(steamId);

                StringBuilder builder = new StringBuilder($"https://api.steampowered.com/IPlayerService/GetTopAchievementsForGames/v1/?key={steamApiKey}&steamid={steamId}&max_achievements=10000");

                for (int i = 0; i < games.Count(); ++i)
                {
                    builder.Append($"&appids[{i}]={games[i].appID}");
                }

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Badluck-Achievements");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await httpClient.GetAsync(builder.ToString());

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                var parsed = JObject.Parse(json);
                int countCompletedAchievements = 0;
                int countAchievements = 0;
                foreach (JObject i in parsed["response"]!["games"]!.Where(x => x is JObject obj && obj.ContainsKey("achievements")).ToArray())
                {
                    countAchievements += i.Value<int>("total_achievements");
                    countCompletedAchievements += i["achievements"]!.Count();
                }

                stats.totalGames = games.Count();
                stats.totalAchievements = countAchievements;
                stats.completedAchievements = countCompletedAchievements;
                stats.hoursPlayed = games.Sum((x) => x.playtimeForever);

                return stats;
            }
            catch
            {
                return null;
            }
        }
    }
}
