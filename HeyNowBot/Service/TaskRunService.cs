using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeyNowBot.Model;

namespace HeyNowBot.Service
{
    /// <summary>
    /// 작업 실행 서비스
    /// </summary>
    public interface ITaskRunService
    {
        Task<string?> GetCountAlarmMessageAsync(int hour);
        Task<string?> GetStockPriceMessageAsync();
        Task InitializeRssAsync();
        Task<string?> GetRssNewsMessageAsync(bool isDebug = false);
        Task<List<VisitLogModel>?> GetVisitListAsync();
    }

    public class TaskRunService : ITaskRunService
    {
        private readonly ISupabaseService _supabase;
        private readonly INaverFinanceService _naverFinance;
        private readonly IRssService _rssService;

        public TaskRunService(
            ISupabaseService supabase,
            INaverFinanceService naverFinance,
            IRssService rssService)
        {
            _supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
            _naverFinance = naverFinance ?? throw new ArgumentNullException(nameof(naverFinance));
            _rssService = rssService ?? throw new ArgumentNullException(nameof(rssService));
        }

        public async Task<string?> GetCountAlarmMessageAsync(int hour)
        {
            try
            {
                var visitList = await _supabase.GetVisitListAsync();
                var visitCount = visitList?.Count ?? 0;
                var uniqueCount = visitList?.GroupBy(x => x.UserId).Count() ?? 0;

                await _supabase.UpdateVisitLogAsync();

                return $"[HeyNowBot] {hour}시 오늘 방문자 수: {visitCount}명 (중복제외: {uniqueCount}명)";
            }
            catch (Exception ex)
            {
                Log($"방문자 통계 오류: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetStockPriceMessageAsync()
        {
            try
            {
                var stockInfo = await _naverFinance.GetStockInfoAsync(Constants.Stock.MonitoringStockCode);
                if (stockInfo == null)
                    return Constants.Message.StockInfoFailedMessage;

                var changeSign = stockInfo.ChangeAmount >= 0 ? "+" : "";
                var changePercentSign = stockInfo.ChangePercent >= 0 ? "+" : "";

                return $"[NaverFinanceService] {stockInfo.Name}({stockInfo.Code}) - " +
                       $"현재가: {stockInfo.CurrentPrice}, 전일대비: {changeSign}{stockInfo.ChangeAmount:F0} ({changePercentSign}{stockInfo.ChangePercent:F2}%)";
            }
            catch (Exception ex)
            {
                Log($"주식 정보 조회 오류: {ex.Message}");
                return Constants.Message.StockInfoFailedMessage;
            }
        }

        public async Task InitializeRssAsync()
        {
            Log(Constants.Message.RssInitializationMessage);
            
            foreach (var url in Constants.Rss.FeedUrls)
            {
                try
                {
                    await _rssService.GetNewFeedsAsync(url, isDebug: false);
                }
                catch (Exception ex)
                {
                    Log($"RSS 초기화 오류 ({url}): {ex.Message}");
                }
            }
        }

        public async Task<string?> GetRssNewsMessageAsync(bool isDebug = false)
        {
            try
            {
                Log($"{Constants.Message.RssCheckStartMessage} (Debug: {isDebug})");

                var allNewItems = new List<RssItem>();

                foreach (var url in Constants.Rss.FeedUrls)
                {
                    try
                    {
                        var newItems = await _rssService.GetNewFeedsAsync(url, isDebug);
                        if (newItems?.Count > 0)
                            allNewItems.AddRange(newItems);
                    }
                    catch (Exception ex)
                    {
                        Log($"RSS 조회 오류 ({url}): {ex.Message}");
                    }
                }

                if (allNewItems.Count == 0)
                    return null;

                var prefix = isDebug ? "[DEBUG] " : "";
                var sb = new StringBuilder();
                sb.AppendLine($"{prefix}RSS 새 글 {allNewItems.Count}개");

                foreach (var item in allNewItems.OrderByDescending(x => x.PubDate))
                {
                    var categoryText = string.IsNullOrWhiteSpace(item.Category) ? "" : $"[{item.Category}] ";
                    var block = $"- {categoryText}{item.Title}\n  {item.Link}\n";

                    if (sb.Length + block.Length + 20 > Constants.Message.MaxMessageLength)
                    {
                        sb.AppendLine("(이하 생략)");
                        break;
                    }

                    sb.Append(block);
                }

                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                Log($"RSS 뉴스 메시지 생성 오류: {ex.Message}");
                return null;
            }
        }

        public async Task<List<VisitLogModel>?> GetVisitListAsync()
        {
            try
            {
                return await _supabase.GetVisitListAsync();
            }
            catch (Exception ex)
            {
                Log($"방문 목록 조회 오류: {ex.Message}");
                return null;
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [TaskRunService] {message}");
        }
    }
}
