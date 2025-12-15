using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeyNowBot.Service;
using Supabase.Postgrest.Models;

namespace HeyNowBot
{
    public class ProcessMain
    {

        private ITelegramService _bot;
        private IServiceSupabase _supabase;
        private TaskRunService _taskRunService;
        private TimeChekerService _timeChekerService;
        public ProcessMain()
        {
               
        }
        public async Task RunAsync()
        {
            await SetLoadAsync();
            _timeChekerService = new TimeChekerService();
            await _bot.SendMessageAsync("[HeyNowBot] 봇이 시작되었습니다.15551231");

            _timeChekerService.OnHourReached += async (hour)  =>
            {
                await _taskRunService.CountAlarmAsync(hour);
                if (hour % 6 == 0)
                {
                    await _taskRunService.UpdateCountAsync();
                }
            };
            _timeChekerService.Start();
            await Task.Delay(Timeout.Infinite);
        }

        private async Task SetLoadAsync()
        {
            _bot = new TelegramService();
            _supabase = new ServiceSupabase();
            _taskRunService = new TaskRunService(telegram: _bot, supabase: _supabase);
        }
    }
}
