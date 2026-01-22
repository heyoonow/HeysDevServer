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

            // 1. 매 시간 정각 실행
            _timeChekerService.OnHourReached += async (hour, minute)  =>
            {
                Console.WriteLine($"[ProcessMain] Hour Reached :{hour}:{minute}");
                
                // 3시간마다 알림 (0, 3, 6, 9...시)
                if (hour % 3 == 0)
                {
                    await _taskRunService.CountAlarmAsync(hour);
                }
            };

            // 2. 매 30분 간격 실행 (0분, 30분)
            _timeChekerService.On30MinReached += async (hour, minute) =>
            {
                Console.WriteLine($"[ProcessMain] 30Min Reached :{hour}:{minute}");

                // 주식 장 운영 시간 (09:00 ~ 15:59) 동안 주가 전송
                if (hour >= 9 && hour <= 15)
                {
                    await _taskRunService.SendStockPrice();
                }
            };

            // 3. 매 10분 간격 실행 (추후 필요 시 로직 추가)
            _timeChekerService.On10MinReached += async (hour, minute) =>
            {
                Console.WriteLine($"[ProcessMain] 10Min Reached :{hour}:{minute}");
            };

            // 4. 매 1분 간격 실행 (디버깅 및 정밀 체크용)
            _timeChekerService.On1MinReached += async (hour, minute) => 
            {
                // Console.WriteLine($"[ProcessMain] 1Min Reached :{hour}:{minute}");
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

            //await _taskRunService.SendStockPrice();
        }
    }
}
