namespace Badluck_Achievements.Components.Data
{
    public class LeaderBoard
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public User User { get; set; }

        public double Score { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
