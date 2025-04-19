using Badluck_Achievements.Components;
using Badluck_Achievements.Components.Models;
using Newtonsoft.Json.Linq;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamPlayer;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Mappings;
using SteamWebAPI2.Models;
using SteamWebAPI2.Utilities;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;

namespace Components.Services_Achievements.Components
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
                CreateSteamWebInterface<SteamUser>().GetPlayerSummariesAsync(new ReadOnlyCollection<ulong>(new List<ulong>() { steamUserId}));
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

                    for (int i = 0; i < x.Count(); ++i)
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
                        stats.totalAchievements += i.Value<int>("total_achievements");
                        stats.games.Add(new GameInfo
                        {
                            appId = i.Value<ulong>("appid"),
                            totalAhievements = i.Value<ulong>("total_achievements")
                        });

                        if (i is JObject obj && !obj.ContainsKey("achievements"))
                        {
                            continue;
                        }

                        stats.games.Last().completedAchievements = (ulong)i["achievements"]!.Count(); ;

                        stats.completedAchievements += i["achievements"]!.Count();

                        foreach (JObject j in i["achievements"]!)
                        {
                            stats.achievements.Add(new SteamAchievement
                            {
                                name = j.Value<string>("name")!,
                                isAchieved = true,
                                achievePercentage = double.Parse(j.Value<string>("player_percent_unlocked")),
                                iconUrl = $"https://cdn.fastly.steamstatic.com/steamcommunity/public/images/apps/{i["appid"]}/{j.Value<string>("icon")}",
                                appId = i.Value<uint>("appid")
                            });
                        }
                    }
                }

				stats.totalGames = games.GameCount;
				stats.hoursPlayed = games.OwnedGames.Sum(x => x.PlaytimeForever.TotalHours);

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

            if(stats == null)
            {
                await LoadUserStats(httpClient, steamId);
            }

            return new Tuple<List<SteamPlayerGame>?, UserStats>(games.Select((g, i) => new SteamPlayerGame
            {
                appID = g.AppId,
                name = g.Name,
                playtimeForever = g.PlaytimeForever.TotalHours,
                playtime2Weeks = g.PlaytimeLastTwoWeeks?.TotalHours,
                iconUrl = g.ImgIconUrl,
                img = $"https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/{g.AppId}/header.jpg",
                achievementsCount = stats.games.Find(x=> x.appId == g.AppId).totalAhievements,
                completedAchievements = stats.games.Find(x => x.appId == g.AppId).completedAchievements,
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
                    if (achievements.Any())
                    {
                        resultDict.TryAdd(game.AppId, achievements);
                    }
                }
                catch(Exception e)
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

        public async Task<List<SteamAchievement>> GetGameAchievementsAsync(ulong steamUserId, uint appId)
        {
            var steamUserStats = _steamFactory.CreateSteamWebInterface<SteamUserStats>();

            try
            {
                var response = await steamUserStats.GetPlayerAchievementsAsync(appId, steamUserId);
                var schema = await steamUserStats.GetSchemaForGameAsync(appId);

                var achievementTasks = response.Data.Achievements
                    .Select(async a =>
                    {
                        var schemaAchievement = schema.Data.AvailableGameStats?.Achievements?
                            .ToDictionary(a => a.Name, a => a);

                        Steam.Models.SchemaGameAchievementModel sa;
                        return new SteamAchievement
                        {
                            name = a.Name,
                            isAchieved = a.Achieved == 1,
                            unlockTime = a.UnlockTime.ToUnixTimeStamp() > 0 ? a.UnlockTime : null,
                            iconUrl = schemaAchievement.TryGetValue(a.APIName, out sa) ? $"{sa.Icon}" : null,
                            achievePercentage = await GetAchievementPercentage(appId, a.APIName)
                        };
                    })
                    .ToList();
                var achievments = await Task.WhenAll(achievementTasks);
                return achievments.ToList();
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching achievements: {ex.Message}");
                return new List<SteamAchievement>();
            }
        }

        private async Task<double> GetAchievementPercentage(uint appID, string achievementApiName)
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
