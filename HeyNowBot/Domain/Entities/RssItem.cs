using System;

namespace HeyNowBot.Domain.Entities
{
    public class RssItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public DateTime PubDate { get; set; }
        public string Category { get; set; }
    }
}
