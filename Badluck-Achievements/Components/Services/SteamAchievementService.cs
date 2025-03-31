using Steam.Models.DOTA2;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Models.SteamStore;
using SteamWebAPI2.Utilities;

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

        public async Task<List<SteamAchievement>> GetGameAchievementsAsync(ulong steamUserId, uint appId)
        {
            var steamUserStats = _steamFactory.CreateSteamWebInterface<SteamUserStats>();
            
            try
            {
                var response = await steamUserStats.GetPlayerAchievementsAsync(appId, steamUserId);
                var schema = await steamUserStats.GetSchemaForGameAsync(appId);

                return response.Data.Achievements
                    .Select(a =>
                    {
                        var schemaAchievement = schema.Data.AvailableGameStats.Achievements
                        .FirstOrDefault(x => x.Name == a.Name);

                        return new SteamAchievement
                        {
                            name = a.Name,
                            isAchieved = a.Achieved == 1,
                            unlockTime = a.UnlockTime.ToUnixTimeStamp() > 0 ? a.UnlockTime : null,
                            iconUrl = schemaAchievement?.Icon ?? ""
                        };
                    })
                    .ToList();
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching achievements: {ex.Message}");
                return new List<SteamAchievement>();
            }
        }
    }

    public class SteamAchievement
    {
        public string name { get; set; } = string.Empty;
        public bool isAchieved { get; set; }
        public DateTime? unlockTime { get; set; }
        public string iconUrl { get; set; } = string.Empty;
    }
}
