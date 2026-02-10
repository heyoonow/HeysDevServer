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
        Task<string?> GetKeywordNewsMessageAsync(string keyword);
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

                return $"[NaverFinanceService] {stockInfo.Name}({stockInfo.Code}) - " +
                       $"현재가: {stockInfo.CurrentPrice}, 전일대비: {stockInfo.PreviousDayChange} ({stockInfo.ChangeRate})";
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

        public async Task<string?> GetKeywordNewsMessageAsync(string keyword)
        {
            try
            {
                Log($"키워드 뉴스 검색 시작: {keyword}");

                // 키워드별 RSS URL이 있으면 사용, 없으면 기본 URL에서 필터링
                if (!Constants.Rss.KeywordRssUrls.TryGetValue(keyword, out var keywordRssUrls))
                {
                    Log($"'{keyword}'에 대한 전용 RSS URL이 없습니다. 기본 RSS에서 필터링합니다.");
                    return await SearchInBasicRssAsync(keyword);
                }

                // 모든 키워드 RSS URL에서 뉴스 수집
                var allNewItems = new List<RssItem>();
                var yesterday = DateTime.Now.Date.AddDays(-1);

                foreach (var keywordRssUrl in keywordRssUrls)
                {
                    try
                    {
                        var newItems = await _rssService.GetNewFeedsAsync(keywordRssUrl, isDebug: false);
                        
                        // 어제 이후 발행된 뉴스만 필터링
                        var recentItems = newItems?
                            .Where(item => item.PubDate.Date >= yesterday)
                            .ToList();

                        if (recentItems?.Count > 0)
                        {
                            Log($"{keywordRssUrl}에서 {recentItems.Count}개의 어제 이후 뉴스 발견");
                            allNewItems.AddRange(recentItems);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"RSS 조회 오류 ({keywordRssUrl}): {ex.Message}");
                    }
                }

                if (allNewItems.Count == 0)
                {
                    Log($"[{keyword}] 어제 이후 발행된 뉴스 없음");
                    return null;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"[{keyword}] 어제 이후 뉴스 {allNewItems.Count}개");

                foreach (var item in allNewItems.OrderByDescending(x => x.PubDate).Take(Constants.Rss.MaxKeywordNews))
                {
                    var categoryText = string.IsNullOrWhiteSpace(item.Category) ? "" : $"[{item.Category}] ";
                    var block = $"- {categoryText}{item.Title}\n  {item.Link}\n";
                    sb.Append(block);
                }

                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                Log($"키워드 뉴스 검색 오류: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> SearchInBasicRssAsync(string keyword)
        {
            try
            {
                var allNewItems = new List<RssItem>();

                foreach (var url in Constants.Rss.FeedUrls)
                {
                    try
                    {
                        var newItems = await _rssService.GetNewFeedsAsync(url, isDebug: false);
                        var keywordItems = newItems?.Where(item => 
                            item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            item.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (keywordItems?.Count > 0)
                            allNewItems.AddRange(keywordItems);
                    }
                    catch (Exception ex)
                    {
                        Log($"RSS 조회 오류 ({url}): {ex.Message}");
                    }
                }

                if (allNewItems.Count == 0)
                    return null;

                var sb = new StringBuilder();
                sb.AppendLine($"[{keyword}] 관련 뉴스 {allNewItems.Count}개");

                foreach (var item in allNewItems.OrderByDescending(x => x.PubDate).Take(Constants.Rss.MaxKeywordNews))
                {
                    var categoryText = string.IsNullOrWhiteSpace(item.Category) ? "" : $"[{item.Category}] ";
                    var block = $"- {categoryText}{item.Title}\n  {item.Link}\n";
                    sb.Append(block);
                }

                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                Log($"기본 RSS 필터링 오류: {ex.Message}");
                return null;
            }
        }
    }
}
