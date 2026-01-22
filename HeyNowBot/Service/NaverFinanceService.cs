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
            // 리눅스 서버에서 EUC-KR(949) 인코딩을 사용하기 위한 필수 설정
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _httpClient = new HttpClient();
            // 네이버 차단을 피하기 위해 헤더를 실제 브라우저와 유사하게 설정
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://finance.naver.com/");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7");
        }

        /// <summary>
        /// 네이버 금융 페이지에서 HTML을 가져와 EUC-KR로 디코딩합니다.
        /// </summary>
        private async Task<string> GetHtmlWithEucKrAsync(string url)
        {
            var bytes = await _httpClient.GetByteArrayAsync(url);
            return Encoding.GetEncoding("euc-kr").GetString(bytes);
        }

        public async Task<bool> IsMarketOpenAsync(string stockCode = "005930")
        {
            try
            {
                var url = $"https://finance.naver.com/item/main.naver?code={stockCode}";
                var html = await GetHtmlWithEucKrAsync(url);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var realtimeNode = htmlDoc.DocumentNode.SelectSingleNode("//em[@class='realtime']");

                // "실시간" 글자가 포함되어 있거나 특정 노드가 존재하면 장 중으로 판단
                if (realtimeNode != null && realtimeNode.InnerText.Trim().Contains("실시간"))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IsMarketOpenAsync] 오류: {ex.Message}");
                return false;
            }
        }

        public async Task<StockInfo> GetStockInfoAsync(string stockCode)
        {
            // 장 오픈 여부 확인 시에도 인코딩 수정 버전 사용
            // (서버 IP 차단 여부를 먼저 확인하기 위해 로그 추가 권장)
            try
            {
                var url = $"https://finance.naver.com/item/main.naver?code={stockCode}";
                var html = await GetHtmlWithEucKrAsync(url);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var stockInfo = new StockInfo
                {
                    Code = stockCode,
                    UpdateTime = DateTime.Now
                };

                // 종목명
                var nameNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wrap_company']//h2/a");
                if (nameNode != null)
                {
                    stockInfo.Name = nameNode.InnerText.Trim();
                }

                // 현재가
                var priceNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='today']//p[@class='no_today']/em/span[@class='blind']");
                if (priceNode != null)
                {
                    stockInfo.CurrentPrice = priceNode.InnerText.Trim();
                }

                // 전일대비 및 등락률
                var noExdayNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='today']//p[@class='no_exday']");
                if (noExdayNode != null)
                {
                    var isUp = noExdayNode.SelectSingleNode(".//em[contains(@class, 'ico_up')]") != null;
                    var isDown = noExdayNode.SelectSingleNode(".//em[contains(@class, 'ico_down')]") != null;

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
                            stockInfo.PreviousDayChange = changeValue.StartsWith("+") ? changeValue : $"+{changeValue}";
                            stockInfo.ChangeRate = changeRateValue.StartsWith("+") ? changeRateValue : $"+{changeRateValue}";
                        }
                        else
                        {
                            stockInfo.PreviousDayChange = changeValue == "0" ? "0 (보합)" : changeValue;
                            stockInfo.ChangeRate = changeRateValue == "0.00%" ? "0.00%" : changeRateValue;
                        }
                    }
                }

                return stockInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetStockInfoAsync] 코드 {stockCode} 처리 중 오류: {ex.Message}");
                return null;
            }
        }

        public async Task<List<StockInfo>> GetMultipleStockInfoAsync(params string[] stockCodes)
        {
            // 병렬 처리 시에도 동일한 HttpClient를 사용하여 효율성 유지
            var tasks = stockCodes.Select(code => GetStockInfoAsync(code));
            var results = await Task.WhenAll(tasks);
            return results.Where(r => r != null).ToList();
        }
    }
}