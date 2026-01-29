using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Globalization;

namespace HeyNowBot.Service
{
    public class RssItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public DateTime PubDate { get; set; }
        public string Category { get; set; }
    }

    public class RssService
    {
        private readonly HttpClient _httpClient;
        private Dictionary<string, DateTime> _lastCheckTimes = new Dictionary<string, DateTime>();

        public RssService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public async Task<List<RssItem>> GetNewFeedsAsync(string rssUrl, bool isDebug = false)
        {
            try
            {
                using var response = await _httpClient.GetAsync(rssUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[RssService] 요청 실패: {rssUrl} ({response.StatusCode})");
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

                // [추가] 디버그 모드일 경우: 무조건 최신 글 1개 반환
                if (isDebug)
                {
                    Console.WriteLine($"[RssService] 디버그 모드 - 최신 글 1개 강제 반환: {rssUrl}");
                    return items.Take(1).ToList();
                }

                var latestItemDate = items.First().PubDate;

                // 1. 처음 체크하는 URL인 경우: 기준점만 잡고 리턴 (알림 X)
                if (!_lastCheckTimes.ContainsKey(rssUrl))
                {
                    _lastCheckTimes[rssUrl] = latestItemDate;
                    Console.WriteLine($"[RssService] 초기화 완료: {rssUrl} (최신글: {latestItemDate})");
                    return new List<RssItem>();
                }

                // 2. 새로운 글 필터링
                var lastCheckTime = _lastCheckTimes[rssUrl];
                var newItems = items.Where(x => x.PubDate > lastCheckTime).ToList();

                // 3. 기준점 갱신
                if (newItems.Any())
                {
                    _lastCheckTimes[rssUrl] = newItems.First().PubDate;
                }

                return newItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RssService] RSS 파싱 오류 ({rssUrl}): {ex.Message}");
                return new List<RssItem>();
            }
        }

        private static string ParseCategory(XElement itemElement)
        {
            // RSS에 category가 여러 개면 모두 합침
            var categories = itemElement.Elements("category")
                .Select(x => x.Value?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return categories.Count == 0 ? "" : string.Join(", ", categories);
        }

        private DateTime ParseDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return DateTime.MinValue;

            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;

            if (DateTime.TryParse(dateStr, out var resultLocal))
                return resultLocal;

            return DateTime.MinValue;
        }
    }
}