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
            Console.WriteLine("[TaskRunService] RSS 초기화 중...");
            foreach (var url in _rssUrls)       
            {
                await rssService.GetNewFeedsAsync(url, isDebug: false);
            }
        }

        public async Task<string?> GetRssNewsMessageAsync(bool isDebug = false)
        {
            Console.WriteLine($"[TaskRunService] RSS 체크 시작 (Debug: {isDebug})");

            var sb = new StringBuilder();
            var hasAny = false;

            foreach (var url in _rssUrls)
            {
                // FIX: isDebug 고정(true) 버그 제거
                var newItems = await rssService.GetNewFeedsAsync(url, isDebug);

                if (newItems == null || newItems.Count == 0)
                    continue;

                hasAny = true;

                var prefix = isDebug ? "[DEBUG] " : "";
                sb.AppendLine($"{prefix}📰 [RSS 업데이트]");
                sb.AppendLine($"Source: {url}");

                foreach (var item in newItems)
                {
                    sb.AppendLine();
                    sb.AppendLine($"제목: {item.Title}");
                    sb.AppendLine($"일시: {item.PubDate:MM-dd HH:mm}");
                    sb.AppendLine($"링크: {item.Link}");
                }

                sb.AppendLine();
                sb.AppendLine("----");
                sb.AppendLine();
            }

            return hasAny ? sb.ToString().Trim() : null;
        }
    }
}
