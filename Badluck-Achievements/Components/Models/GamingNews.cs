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

        public GamingNews(string topic, string description, string newsUrl) 
        { 
            this.topic = topic;
            this.description = description;
            this.newsUrl = newsUrl;
        }
    }
}
