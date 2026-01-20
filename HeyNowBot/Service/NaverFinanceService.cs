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
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<bool> IsMarketOpenAsync(string stockCode = "005930")
        {
            try
            {
                var url = $"https://finance.naver.com/item/main.naver?code={stockCode}";
                var html = await _httpClient.GetStringAsync(url);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var realtimeNode = htmlDoc.DocumentNode.SelectSingleNode("//em[@class='realtime']");
                
                if (realtimeNode != null && realtimeNode.InnerText.Trim().Contains("실시간"))
                {
                    return true;
                }
                
                
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<StockInfo> GetStockInfoAsync(string stockCode)
        {
            if (!await IsMarketOpenAsync())
            {
                return null;
            }

            try
            {
                var url = $"https://finance.naver.com/item/main.naver?code={stockCode}";
                var html = await _httpClient.GetStringAsync(url);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var stockInfo = new StockInfo
                {
                    Code = stockCode,
                    UpdateTime = DateTime.Now
                };

                var nameNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wrap_company']//h2/a");
                if (nameNode != null)
                {
                    stockInfo.Name = nameNode.InnerText.Trim();
                }

                var priceNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='today']//p[@class='no_today']/em/span[@class='blind']");
                if (priceNode != null)
                {
                    stockInfo.CurrentPrice = priceNode.InnerText.Trim();
                }

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
                Console.WriteLine($"[NaverFinanceService] 오류 발생: {ex.Message}");
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
