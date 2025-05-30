using Badluck_Achievements.Components;
using Badluck_Achievements.Components.Models;
using Newtonsoft.Json.Linq;
using Steam.Models;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamPlayer;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

namespace Components.Services
{
    public class SteamAchievementService
    {
        private readonly SteamWebInterfaceFactory _steamFactory;
        private readonly string _steamApiKey;

        public SteamAchievementService(string steamApiKey)
        {
            _steamApiKey = steamApiKey;
            _steamFactory = new SteamWebInterfaceFactory(_steamApiKey);
        }

        public async Task<uint> GetNumberOfCurrentPlayersForGameAsync(uint appId)
        {
            var response = await _steamFactory.CreateSteamWebInterface<SteamUserStats>().
                GetNumberOfCurrentPlayersForGameAsync(appId);
            return response.Data;
        }

        public async Task<OwnedGamesResultModel?> GetOwnedGames(ulong steamId)
        {
            var data = await _steamFactory.CreateSteamWebInterface<PlayerService>()
                .GetOwnedGamesAsync(steamId, includeAppInfo: true, includeFreeGames: true);
            return data.Data;
        }

        public async Task<IReadOnlyCollection<GlobalAchievementPercentageModel>> GetGlobalAchievementPercentagesForAppAsync(uint appId)
        {
            var response = await _steamFactory.
                CreateSteamWebInterface<SteamUserStats>().
                GetGlobalAchievementPercentagesForAppAsync(appId);


            return response.Data;
        }

        public async Task<PlayerAchievementResultModel> GetPlayerAchievementsAsync(uint appId, ulong steamUserId)
        {
            var response = await _steamFactory.
                CreateSteamWebInterface<SteamUserStats>().
                GetPlayerAchievementsAsync(appId, steamUserId);
            return response.Data;
        }

        public async Task<PlayerSummaryModel> GetPlayerSummaries(ulong steamUserId)
        {
            var response = await _steamFactory.
                CreateSteamWebInterface<SteamUser>().GetPlayerSummariesAsync(new ReadOnlyCollection<ulong>(new List<ulong>() { steamUserId }));
            return response.Data.First();
        }

        public async Task<UserStats?> LoadUserStats(HttpClient httpClient, ulong steamId)
        {
            try
            {
                UserStats? stats = new UserStats();

                var games = await GetOwnedGames(steamId);

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Badluck-Achievements");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var list = new List<List<OwnedGameModel?>>();

                for (int i = 0; i < games.GameCount; i += 400)
                {
                    list.Add(games.OwnedGames.ToList().GetRange(i, (int)Math.Min(400, games.GameCount - i)));
                }

                var urls = list.Select((x) =>
                {
                    StringBuilder builder = new StringBuilder($"https://api.steampowered.com/IPlayerService/GetTopAchievementsForGames/v1/?key={_steamApiKey}&steamid={steamId}&max_achievements=10000");

                    for (int i = 0; i < x.Count; ++i)
                    {
                        builder.Append($"&appids[{i}]={x[i].AppId}");
                    }

                    return builder.ToString();
                }).ToArray();

                var requests = urls.Select
                    (
                        url => httpClient.GetAsync(url)
                    ).ToList();

                await Task.WhenAll(requests);

                var responses = requests.Select
                    (
                        task => task.Result
                    );

                foreach (var r in responses)
                {
                    var s = await r.Content.ReadAsStringAsync();
                    var parsed = JObject.Parse(s);

                    foreach (JObject i in parsed["response"]!["games"]!)
                    {
                        stats.TotalAchievements += i.Value<int>("total_achievements");
                        stats.Games.Add(new GameInfo
                        {
                            AppId = i.Value<ulong>("appid"),
                            TotalAhievements = i.Value<ulong>("total_achievements")
                        });

                        if (i is JObject obj && !obj.ContainsKey("achievements"))
                        {
                            continue;
                        }

                        stats.Games.Last().CompletedAchievements = (ulong)i["achievements"]!.Count(); ;

                        stats.CompletedAchievements += i["achievements"]!.Count();

                        foreach (JObject j in i["achievements"]!)
                        {
                            stats.Achievements.Add(new SteamAchievement
                            {
                                Name = j.Value<string>("name")!,
                                IsAchieved = true,
                                AchievePercentage = double.Parse(j.Value<string>("player_percent_unlocked")),
                                IconUrl = $"https://cdn.fastly.steamstatic.com/steamcommunity/public/images/apps/{i["appid"]}/{j.Value<string>("icon")}",
                                AppId = i.Value<uint>("appid"),
                                Bit = j.Value<uint>("bit")
                            });
                        }
                    }
                }

                stats.TotalGames = games.GameCount;
                stats.HoursPlayed = games.OwnedGames.Sum(x => x.PlaytimeForever.TotalHours);

                return stats;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Tuple<List<SteamPlayerGame>?, UserStats>> GetPlayerGames(ulong steamId, UserStats stats = null)
        {
            HttpClient httpClient = new HttpClient();
            var playerInterface = _steamFactory.CreateSteamWebInterface<PlayerService>();
            var responseGames = await playerInterface.GetOwnedGamesAsync(steamId, includeAppInfo: true, includeFreeGames: true);
            var games = responseGames.Data.OwnedGames;

            var list = new List<List<OwnedGameModel?>>();

            stats ??= await LoadUserStats(httpClient, steamId);

            return new Tuple<List<SteamPlayerGame>?, UserStats>(games.Select((g, i) => new SteamPlayerGame
            {
                appID = g.AppId,
                name = g.Name,
                PlaytimeForever = g.PlaytimeForever.TotalHours,
                Playtime2Weeks = g.PlaytimeLastTwoWeeks?.TotalHours,
                IconUrl = g.ImgIconUrl,
                img = $"https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/{g.AppId}/header.jpg",
                achievementsCount = stats.Games.Find(x => x.AppId == g.AppId).TotalAhievements,
                CompletedAchievements = stats.Games.Find(x => x.AppId == g.AppId).CompletedAchievements,
            }).ToList(), stats);
        }

        public async Task<List<SteamAchievement>> GetAllPlayerAchievements(ulong steamId)
        {
            var playerService = _steamFactory.CreateSteamWebInterface<PlayerService>();
            var ownedGames = await playerService.GetOwnedGamesAsync(steamId, includeAppInfo: false);

            var resultDict = new ConcurrentDictionary<uint, List<SteamAchievement>>();

            await Parallel.ForEachAsync(ownedGames.Data.OwnedGames, async (game, ct) =>
            {
                try
                {
                    var achievements = await GetGameAchievementsAsync(steamId, game.AppId);
                    if (achievements.Count == 0)
                    {
                        resultDict.TryAdd(game.AppId, achievements);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });

            var result = new List<SteamAchievement>();
            foreach (var i in resultDict)
            {
                result.Concat(i.Value);
            }

            return result;
        }

        public async Task<List<SteamAchievement>> GetGameAchievementsAsync(ulong steamUserId, uint appId, string language = "english", HttpClient? httpClient = null)
        {
            var steamUserStats = _steamFactory.CreateSteamWebInterface<SteamUserStats>();

            try
            {
                if (httpClient == null)
                    httpClient = new HttpClient();
                Task<ISteamWebResponse<PlayerAchievementResultModel>> responseTask = steamUserStats.GetPlayerAchievementsAsync(appId, steamUserId, "english");
                var achievementsTask = httpClient.GetAsync($"https://api.steampowered.com/IPlayerService/GetGameAchievements/v1?key={_steamApiKey}&appid={appId}&language={language}");

                await Task.WhenAll(achievementsTask);
                ISteamWebResponse<PlayerAchievementResultModel>? response = null;
                var responeAchievements = achievementsTask.Result;

                try
                {
                    response = await responseTask;
                }
                catch
                {
                    Console.WriteLine("No achievements info");
                }

                string jsonAchievements = await responeAchievements.Content.ReadAsStringAsync();

                var achievements = JObject.Parse(jsonAchievements);
                return achievements["response"]!["achievements"]!.Select(x =>
                {
                    PlayerAchievementModel? achievement = null;
                    if (response != null)
                    {
                        achievement =
                            response.Data.Achievements.FirstOrDefault(a => a.APIName == x.Value<string>("internal_name")) ??
                            new PlayerAchievementModel
                            {
                                APIName = x.Value<string>("internal_name"),
                                Achieved = 0,
                                UnlockTime = DateTime.MinValue
                            };
                    }

                    achievement ??= new PlayerAchievementModel
                        {
                            APIName = x.Value<string>("internal_name"),
                            Achieved = 0,
                            UnlockTime = DateTime.MinValue
                        };

                    return new SteamAchievement
                    {
                        ApiName = achievement.APIName,
                        Name = x.Value<string>("localized_name"),
                        IsAchieved = achievement.Achieved != 0,
                        AchievePercentage = Convert.ToDouble(x.Value<string>("player_percent_unlocked")),
                        UnlockTime = achievement.UnlockTime,
                        IconUrl = $"https://cdn.fastly.steamstatic.com/steamcommunity/public/images/apps/{appId}/{x.Value<string>("icon")}",
                        AppId = appId,
                        Bit = 0
                    };
                }

                ).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching achievements: {ex.Message}");
                return [];
            }
        }

        public async Task<double> GetAchievementPercentage(uint appID, string achievementApiName)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0001/?gameid=" + appID);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);
                return double.Parse(parsed["achievementpercentages"]["achievements"]["achievement"]
                    .Where(x => x["name"].ToString() == achievementApiName).First()["percent"].ToString());
            }
            else
            {
                return 0;
            }
        }


    }
}