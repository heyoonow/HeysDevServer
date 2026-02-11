using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HeyNowBot.Service
{
    public class StockInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string CurrentPrice { get; set; }
        public string PreviousDayChange { get; set; }
        public string ChangeRate { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercent { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// 네이버 금융 주식 정보 조회 서비스
    /// </summary>
    public interface INaverFinanceService
    {
        Task<StockInfo> GetStockInfoAsync(string stockCode);
        Task<bool> IsMarketOpenAsync(string stockCode = "005930");
    }

    public class NaverFinanceService : INaverFinanceService
    {
        private readonly HttpClient _httpClient;

        public NaverFinanceService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent.DefaultUserAgent);
            _httpClient.DefaultRequestHeaders.Add("Referer", Constants.UserAgent.NaverFinanceReferer);
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", Constants.UserAgent.AcceptLanguage);
        }

        public async Task<bool> IsMarketOpenAsync(string stockCode = "005930")
        {
            var now = DateTime.Now;
            
            if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return false;

            if (now.Hour < 9 || now.Hour > 15)
                return false;
            
            if (now.Hour == 15 && now.Minute > 30)
                return false;

            try
            {
                var html = await GetHtmlAsync($"https://finance.naver.com/item/main.naver?code={stockCode}");
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var realtimeNode = htmlDoc.DocumentNode.SelectSingleNode("//em[@class='realtime']");
                return realtimeNode?.InnerText.Contains("실시간") ?? false;
            }
            catch (Exception ex)
            {
                Log($"장 상태 확인 오류: {ex.Message}");
                return false;
            }
        }

        public async Task<StockInfo> GetStockInfoAsync(string stockCode)
        {
            try
            {
                var html = await GetHtmlAsync($"https://finance.naver.com/item/main.naver?code={stockCode}");
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var stockInfo = new StockInfo
                {
                    Code = stockCode,
                    UpdateTime = DateTime.Now
                };

                // 회사명
                var nameNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wrap_company']//h2/a");
                if (nameNode != null)
                    stockInfo.Name = HtmlEntity.DeEntitize(nameNode.InnerText.Trim());

                // 현재가
                var priceNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='today']//p[@class='no_today']/em/span[@class='blind']");
                if (priceNode != null)
                    stockInfo.CurrentPrice = priceNode.InnerText.Trim();

                // 전일대비 및 변률
                var noExdayNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='today']//p[@class='no_exday']");
                if (noExdayNode != null)
                {
                    var blindNodes = noExdayNode.SelectNodes(".//span[@class='blind']");
                    if (blindNodes?.Count >= 2)
                    {
                        stockInfo.PreviousDayChange = blindNodes[0].InnerText.Trim();
                        stockInfo.ChangeRate = blindNodes[1].InnerText.Trim();

                        // 부모 요소의 클래스를 확인하여 상승/하락 판단
                        var parentClass = noExdayNode.GetAttributeValue("class", "");
                        bool isDown = parentClass.Contains("down") || parentClass.Contains("fall");

                        // 문자열을 숫자로 파싱
                        if (decimal.TryParse(stockInfo.PreviousDayChange.Replace(",", ""), out var changeAmount))
                        {
                            stockInfo.ChangeAmount = isDown ? -changeAmount : changeAmount;
                        }

                        if (decimal.TryParse(stockInfo.ChangeRate.Replace("%", "").Replace(",", ""), out var changePercent))
                        {
                            stockInfo.ChangePercent = isDown ? -changePercent : changePercent;
                        }
                    }
                }

                return stockInfo;
            }
            catch (Exception ex)
            {
                Log($"주식 정보 조회 오류 ({stockCode}): {ex.Message}");
                return null;
            }
        }

        private async Task<string> GetHtmlAsync(string url)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync();

                var charSet = response.Content.Headers.ContentType?.CharSet;
                if (!string.IsNullOrEmpty(charSet))
                {
                    try
                    {
                        return Encoding.GetEncoding(charSet).GetString(bytes);
                    }
                    catch { }
                }

                var doc = new HtmlDocument();
                using (var ms = new System.IO.MemoryStream(bytes))
                {
                    doc.Load(ms, Encoding.GetEncoding("euc-kr"), true);
                    return doc.Encoding.GetString(bytes);
                }
            }
            catch (Exception ex)
            {
                Log($"HTML 로드 오류 ({url}): {ex.Message}");
                throw;
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NaverFinanceService] {message}");
        }
    }
}