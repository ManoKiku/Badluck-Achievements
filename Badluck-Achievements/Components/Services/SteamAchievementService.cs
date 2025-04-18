using Badluck_Achievements.Components;
using Badluck_Achievements.Components.Models;
using Newtonsoft.Json.Linq;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamPlayer;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Mappings;
using SteamWebAPI2.Models;
using SteamWebAPI2.Utilities;
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

        public async Task<List<SteamPlayerGame>?> GetPlayerGames(ulong steamId)
        {
            HttpClient httpClient = new HttpClient();
            var playerInterface = _steamFactory.CreateSteamWebInterface<PlayerService>();
            var responseGames = await playerInterface.GetOwnedGamesAsync(steamId, includeAppInfo: true, includeFreeGames: true);
            var games = responseGames.Data.OwnedGames;

            StringBuilder builder = new StringBuilder($"https://api.steampowered.com/IPlayerService/GetTopAchievementsForGames/v1/?key={_steamApiKey}&steamid={steamId}&max_achievements=10000");
            for (int i = 0; i < games.Count(); ++i)
            {
                builder.Append($"&appids[{i}]={games.ElementAt(i).AppId}");
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
            return games.Select((g, i) => new SteamPlayerGame
            {
                appID = g.AppId,
                name = g.Name,
                playtimeForever = g.PlaytimeForever.TotalHours,
                playtime2Weeks = g.PlaytimeLastTwoWeeks?.TotalHours,
                iconUrl = g.ImgIconUrl,
                img = $"https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/{g.AppId}/header.jpg",
                achievementsCount = (uint)parsed["response"]!["games"]![i].Value<int>("total_achievements"),
                completedAchievements = (parsed["response"]!["games"]![i] is JObject obj && obj.ContainsKey("achievements")) ? (uint)obj["achievements"]!.Count() : 0
            }).ToList();
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
