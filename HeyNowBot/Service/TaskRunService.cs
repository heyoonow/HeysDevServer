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
        Task UpdateCountAsync();
    }
    public class TaskRunService : ITaskRunService
    {
        public  ITelegramService telegram;
        public  IServiceSupabase supabase;
        public TaskRunService(ITelegramService telegram, IServiceSupabase supabase)
        {
            this.telegram = telegram;
            this.supabase = supabase;
        }

        public async Task CountAlarmAsync(int hour) 
        { 
            var visitList = await supabase.GetVisitListAsync();
            var visitCount = visitList?.Count ?? 0;
            var visitDuplicateCount = visitList?.GroupBy(x=>x.UserId).ToList().Count;
            var message = $"[HeyNowBot] {hour}시 오늘 방문자 수: {visitCount}명 (중복제외: {visitDuplicateCount}명)";
            await telegram.SendMessageAsync(message);
        }


        public async Task UpdateCountAsync()
        {
            await supabase.UpdateVisitLog();
        }

    }
}
