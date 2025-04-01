namespace Badluck_Achievements.Components.Models
{
    public class GamingNews
    {
        // News topic
        public string topic { get; set; } = string.Empty;
        // News brief description
        public string description { get; set; } = string.Empty;
        // News brief url
        public string newsUrl { get; set; } = string.Empty;
        // Publish time
        public DateTime? time { get; set; }

        public GamingNews(string topic, string description, string newsUrl, DateTime? time) 
        { 
            this.topic = topic;
            this.description = description;
            this.newsUrl = newsUrl;
            this.time = time;
        }
    }
}
