using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HeyNowBot.Service
{
    public class RssItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public DateTime PubDate { get; set; }
        public string Category { get; set; }
    }

    /// <summary>
    /// RSS 피드 파싱 및 새 글 감지 서비스
    /// </summary>
    public interface IRssService
    {
        Task<List<RssItem>> GetNewFeedsAsync(string rssUrl, bool isDebug = false);
    }

    public class RssService : IRssService
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, DateTime> _lastCheckTimes = new();

        public RssService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent.DefaultUserAgent);
        }

        public async Task<List<RssItem>> GetNewFeedsAsync(string rssUrl, bool isDebug = false)
        {
            try
            {
                using var response = await _httpClient.GetAsync(rssUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Log($"{Constants.Message.RequestFailedMessage}: {rssUrl} ({response.StatusCode})");
                    return new List<RssItem>();
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                var xdoc = XDocument.Load(stream);

                var items = xdoc.Descendants("item")
                    .Select(item => new RssItem
                    {
                        Title = item.Element("title")?.Value.Trim(),
                        Link = item.Element("link")?.Value.Trim(),
                        PubDate = ParseDate(item.Element("pubDate")?.Value),
                        Category = ParseCategory(item)
                    })
                    .Where(x => x.PubDate != DateTime.MinValue)
                    .OrderByDescending(x => x.PubDate)
                    .ToList();

                if (!items.Any())
                    return new List<RssItem>();

                if (isDebug)
                {
                    Log($"디버그 모드 - 최신 글 1개 강제 반환: {rssUrl}");
                    return items.Take(1).ToList();
                }

                var latestItemDate = items.First().PubDate;

                if (!_lastCheckTimes.ContainsKey(rssUrl))
                {
                    _lastCheckTimes[rssUrl] = latestItemDate;
                    Log($"{Constants.Message.RssInitCompleteMessage}: {rssUrl} (최신글: {latestItemDate:yyyy-MM-dd HH:mm:ss})");
                    return new List<RssItem>();
                }

                var lastCheckTime = _lastCheckTimes[rssUrl];
                var newItems = items.Where(x => x.PubDate > lastCheckTime).ToList();

                if (newItems.Any())
                    _lastCheckTimes[rssUrl] = newItems.First().PubDate;

                return newItems;
            }
            catch (Exception ex)
            {
                Log($"{Constants.Message.ParsingErrorMessage} ({rssUrl}): {ex.Message}");
                return new List<RssItem>();
            }
        }

        private static string ParseCategory(XElement itemElement)
        {
            var categories = itemElement.Elements("category")
                .Select(x => x.Value?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return categories.Count == 0 ? "" : string.Join(", ", categories);
        }

        private static DateTime ParseDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return DateTime.MinValue;

            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;

            if (DateTime.TryParse(dateStr, out var resultLocal))
                return resultLocal;

            return DateTime.MinValue;
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [RssService] {message}");
        }
    }
}