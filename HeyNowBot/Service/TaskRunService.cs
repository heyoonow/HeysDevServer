using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    public interface ITaskRunService
    {
        Task CountAlarmAsync(int hour);
        Task SendStockPrice();
        Task InitializeRssAsync(); 
        Task CheckRssNewsAsync(bool isDebug = false); // 파라미터 추가
    }

    public class TaskRunService : ITaskRunService
    {
        public ITelegramService telegram;
        public IServiceSupabase supabase;
        public NaverFinanceService naverFinance;
        public RssService rssService; 

        private readonly List<string> _rssUrls = new List<string>
        {
             "https://bbs.ruliweb.com/ps/board/300001/rss", 
             "https://bbs.ruliweb.com/mobile/board/300008/rss", 
        };

        public TaskRunService(ITelegramService telegram, IServiceSupabase supabase, NaverFinanceService naverFinance, RssService rssService)
        {
            this.telegram = telegram;
            this.supabase = supabase;
            this.naverFinance = naverFinance;
            this.rssService = rssService;
        }

        public async Task CountAlarmAsync(int hour) 
        { 
            var visitList = await supabase.GetVisitListAsync();
            var visitCount = visitList?.Count ?? 0;
            var visitDuplicateCount = visitList?.GroupBy(x=>x.UserId).ToList().Count;
            var message = $"[HeyNowBot] {hour}시 오늘 방문자 수: {visitCount}명 (중복제외: {visitDuplicateCount}명)";
            await telegram.SendMessageAsync(message);
            await supabase.UpdateVisitLog();
        }

        public async Task SendStockPrice()
        {
            var stockInfo = await naverFinance.GetStockInfoAsync("360750");
            if (stockInfo == null)
            {
                await telegram.SendMessageAsync("주식 정보를 가져오는데 실패");
                return;
            }
            var message = $"[NaverFinanceService] {stockInfo.Name}({stockInfo.Code}) - 현재가: {stockInfo.CurrentPrice}, 전일대비: {stockInfo.PreviousDayChange} ({stockInfo.ChangeRate})";
            await telegram.SendMessageAsync(message);
        }

        // RSS 초기화: 실행 시점 기준 최신 글 시간만 기록 (알림 미발송)
        public async Task InitializeRssAsync()
        {
            Console.WriteLine("[TaskRunService] RSS 초기화 중...");
            foreach (var url in _rssUrls)
            {
                // 초기화 시에는 디버그 모드 X
                await rssService.GetNewFeedsAsync(url, isDebug: false);
            }
        }

        // RSS 체크: 새 글이 있으면 알림 발송 (디버그 모드 지원)
        public async Task CheckRssNewsAsync(bool isDebug = false)
        {
            Console.WriteLine($"[TaskRunService] RSS 체크 시작 (Debug: {isDebug})");
            foreach (var url in _rssUrls)
            {
                // Service로 isDebug 전달
                var newItems = await rssService.GetNewFeedsAsync(url, true);
                
                foreach (var item in newItems)
                {
                    // 디버그일 때는 [DEBUG] 말머리 추가하면 구분하기 좋음
                    var prefix = isDebug ? "[DEBUG] " : "";
                    var msg = $"{prefix}📰 [새로운 뉴스]\n\n제목: {item.Title}\n일시: {item.PubDate:MM-dd HH:mm}\n링크: {item.Link}";
                    await telegram.SendMessageAsync(msg);
                }
            }
        }
    }
}
