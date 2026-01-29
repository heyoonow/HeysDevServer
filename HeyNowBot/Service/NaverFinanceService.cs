using System;
using System.Collections.Generic;
using System.Linq;
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
        public DateTime UpdateTime { get; set; }
    }

    public class NaverFinanceService
    {
        private readonly HttpClient _httpClient;

        public NaverFinanceService()
        {
            // [필수] 리눅스 서버에서 EUC-KR(CP949) 인코딩을 인식하기 위한 설정
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _httpClient = new HttpClient();
            // 실제 브라우저와 유사한 헤더 설정 (차단 방지)
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://finance.naver.com/");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7");
        }

        /// <summary>
        /// 네이버 금융 페이지 HTML 가져오기 (헤더/메타태그 기반 인코딩 자동 감지)
        /// </summary>
        private async Task<string> GetHtmlAsync(string url)
        {
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();

            // 1. 헤더에서 인코딩 확인 (가장 정확함)
            var charSet = response.Content.Headers.ContentType?.CharSet;
            if (!string.IsNullOrEmpty(charSet))
            {
                try
                {
                    return Encoding.GetEncoding(charSet).GetString(bytes);
                }
                catch { }
            }

            // 2. 인코딩 감지 (기본값 EUC-KR, 메타태그/BOM에 따라 자동 변경)
            var doc = new HtmlDocument();
            using (var ms = new System.IO.MemoryStream(bytes))
            {
                // 기본 인코딩을 EUC-KR로 설정하되, detectEncoding=true로 BOM/Meta 태그 확인
                doc.Load(ms, Encoding.GetEncoding("euc-kr"), true);
                return doc.Encoding.GetString(bytes);
            }
        }

        public async Task<bool> IsMarketOpenAsync(string stockCode = "005930")
        {
            // 주말은 무조건 휴장
            var now = DateTime.Now;
            if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return false;

            // 기본 장 시간 가드 (09:00 ~ 15:30)
            // - 15시 이후는 15:30까지만 허용
            if (now.Hour < 9)
                return false;
            if (now.Hour > 15)
                return false;
            if (now.Hour == 15 && now.Minute > 30)
                return false;

            try
            {
                var url = $"https://finance.naver.com/item/main.naver?code={stockCode}";
                var html = await GetHtmlAsync(url);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var realtimeNode = htmlDoc.DocumentNode.SelectSingleNode("//em[@class='realtime']");

                // "실시간"이 있어도 휴장/마감 화면에서 오탐될 수 있어
                // 위의 주말/시간 가드가 1차 방어선
                return realtimeNode != null && realtimeNode.InnerText.Contains("실시간");
            }
            catch
            {
                return false;
            }
        }

        public async Task<StockInfo> GetStockInfoAsync(string stockCode)
        {
            try
            {
                var url = $"https://finance.naver.com/item/main.naver?code={stockCode}";
                var html = await GetHtmlAsync(url); // 인코딩 자동 감지 메서드로 변경

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var stockInfo = new StockInfo
                {
                    Code = stockCode,
                    UpdateTime = DateTime.Now
                };

                // 1. 종목명 (특수문자 및 한글 깨짐 방지 DeEntitize)
                var nameNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wrap_company']//h2/a");
                if (nameNode != null)
                {
                    stockInfo.Name = HtmlEntity.DeEntitize(nameNode.InnerText.Trim());
                }

                // 2. 현재가
                var priceNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='today']//p[@class='no_today']/em/span[@class='blind']");
                if (priceNode != null)          
                {
                    stockInfo.CurrentPrice = priceNode.InnerText.Trim();
                }

                // 3. 전일대비 및 등락률 파싱 (서버 환경에서도 안전한 판별 로직)
                var noExdayNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='today']//p[@class='no_exday']");
                if (noExdayNode != null)
                {
                    // em 태그의 class에 'up' 또는 'down'이 포함되어 있는지 검사 (가장 확실함)
                    var statusNode = noExdayNode.SelectSingleNode(".//em");
                    string classAttr = statusNode?.GetAttributeValue("class", "") ?? "";

                    bool isUp = classAttr.Contains("up");
                    bool isDown = classAttr.Contains("down");

                    var blindNodes = noExdayNode.SelectNodes(".//span[@class='blind']");
                    if (blindNodes != null && blindNodes.Count >= 2)
                    {
                        var changeValue = blindNodes[0].InnerText.Trim();
                        var changeRateValue = blindNodes[1].InnerText.Trim();

                        if (isDown)
                        {
                            stockInfo.PreviousDayChange = $"-{changeValue}";
                            stockInfo.ChangeRate = $"-{changeRateValue}";
                        }
                        else if (isUp)
                        {
                            stockInfo.PreviousDayChange = $"+{changeValue}";
                            stockInfo.ChangeRate = $"+{changeRateValue}";
                        }
                        else
                        {
                            stockInfo.PreviousDayChange = "0";
                            stockInfo.ChangeRate = "0.00%";
                        }
                    }
                }

                return stockInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [NaverFinanceService] {stockCode} 오류: {ex.Message}");
                return null;
            }
        }

        public async Task<List<StockInfo>> GetMultipleStockInfoAsync(params string[] stockCodes)
        {
            var tasks = stockCodes.Select(code => GetStockInfoAsync(code));
            var results = await Task.WhenAll(tasks);
            return results.Where(r => r != null).ToList();
        }
    }
}