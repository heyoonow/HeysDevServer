using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    public interface ITaskRunService
    {
        Task<string?> GetCountAlarmMessageAsync(int hour);
        Task<string?> GetStockPriceMessageAsync();
        Task InitializeRssAsync();
        Task<string?> GetRssNewsMessageAsync(bool isDebug = false);
    }

    public class TaskRunService : ITaskRunService
    {
        public IServiceSupabase supabase;
        public NaverFinanceService naverFinance;
        public RssService rssService;

        private readonly List<string> _rssUrls = new List<string>
        {
            "https://bbs.ruliweb.com/ps/board/300001/rss",
            "https://bbs.ruliweb.com/mobile/board/300008/rss",
            "https://bbs.ruliweb.com/market/board/1020/rss",
        };

        public TaskRunService(IServiceSupabase supabase, NaverFinanceService naverFinance, RssService rssService)
        {
            this.supabase = supabase;
            this.naverFinance = naverFinance;
            this.rssService = rssService;
        }

        public async Task<string?> GetCountAlarmMessageAsync(int hour)
        {
            var visitList = await supabase.GetVisitListAsync();
            var visitCount = visitList?.Count ?? 0;
            var visitDuplicateCount = visitList?.GroupBy(x => x.UserId).ToList().Count;

            await supabase.UpdateVisitLog();

            return $"[HeyNowBot] {hour}시 오늘 방문자 수: {visitCount}명 (중복제외: {visitDuplicateCount}명)";
        }

        public async Task<string?> GetStockPriceMessageAsync()
        {
            var stockInfo = await naverFinance.GetStockInfoAsync("360750");
            if (stockInfo == null)
                return "[NaverFinanceService] 주식 정보를 가져오는데 실패";

            return $"[NaverFinanceService] {stockInfo.Name}({stockInfo.Code}) - 현재가: {stockInfo.CurrentPrice}, 전일대비: {stockInfo.PreviousDayChange} ({stockInfo.ChangeRate})";
        }

        public async Task InitializeRssAsync()
        {
            Log("RSS 초기화 중...");
            foreach (var url in _rssUrls)
            {
                await rssService.GetNewFeedsAsync(url, isDebug: false);
            }
        }

        public async Task<string?> GetRssNewsMessageAsync(bool isDebug = false)
        {
            Log($"RSS 체크 시작 (Debug: {isDebug})");

            // 텔레그램 메시지 길이 방어(여유분 포함)
            const int maxMessageLength = 3500;

            var allNewItems = new List<RssItem>();

            foreach (var url in _rssUrls)
            {
                var newItems = await rssService.GetNewFeedsAsync(url, isDebug);

                if (newItems == null || newItems.Count == 0)
                    continue;

                allNewItems.AddRange(newItems);
            }

            if (allNewItems.Count == 0)
                return null;

            var prefix = isDebug ? "[DEBUG] " : "";
            var sb = new StringBuilder();

            sb.AppendLine($"{prefix}RSS 새 글 {allNewItems.Count}개");

            // 최신순 정렬 후 전부 출력
            foreach (var item in allNewItems.OrderByDescending(x => x.PubDate))
            {
                var categoryText = string.IsNullOrWhiteSpace(item.Category) ? "" : $"[{item.Category}] ";

                var block = new StringBuilder();
                block.AppendLine($"- {categoryText}{item.Title}");
                block.AppendLine($"  {item.Link}");

                if (sb.Length + block.Length + 20 > maxMessageLength)
                {
                    sb.AppendLine("(이하 생략)");
                    break;
                }

                sb.Append(block);
            }

            return sb.ToString().Trim();
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [TaskRunService] {message}");
        }
    }
}
