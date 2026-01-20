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
    }
    public class TaskRunService : ITaskRunService
    {
        public  ITelegramService telegram;
        public  IServiceSupabase supabase;
        public NaverFinanceService naverFinance;
        public TaskRunService(ITelegramService telegram, IServiceSupabase supabase, NaverFinanceService naverFinance)
        {
            this.telegram = telegram;
            this.supabase = supabase;
            this.naverFinance = naverFinance;
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
            if (stockInfo== null)

            {
                return;

            }
                var message = $"[NaverFinanceService] {stockInfo.Name}({stockInfo.Code}) - 현재가: {stockInfo.CurrentPrice}, 전일대비: {stockInfo.PreviousDayChange} ({stockInfo.ChangeRate})";
        }

    }
}
