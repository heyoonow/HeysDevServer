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
        private NaverFinanceService _naverFinanceService;
        public ProcessMain()
        {
               
        }
        public async Task RunAsync()
        {
            await SetLoadAsync();
            _timeChekerService = new TimeChekerService();
            await _bot.SendMessageAsync($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}[HeyNowBot] 봇이 시작되었습니다. ");

            _timeChekerService.OnHourReached += async (hour, minute)  =>
            {
                if (hour % 3 == 0 && minute == 0)
                {
                    Console.WriteLine(
        $"[Send] reportHour={hour} | sendTime={DateTime.Now:yyyy-MM-dd HH:mm:ss}"
    );
                    await _taskRunService.CountAlarmAsync(hour);
                }

                if (hour <= 9 && hour <= 15 )
                {

                    await _taskRunService.SendStockPrice();
                }
            };
            _timeChekerService.Start();
            await Task.Delay(Timeout.Infinite);
        }

        private async Task SetLoadAsync()
        {
            _bot = new TelegramService();
            _supabase = new ServiceSupabase();

            _naverFinanceService = new NaverFinanceService();
            _taskRunService = new TaskRunService(telegram: _bot, supabase: _supabase, naverFinance:_naverFinanceService);
        }
    }
}
