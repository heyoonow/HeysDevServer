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
        
        // _rssUrls 및 _rssService 필드 제거 (TaskRunService로 이관됨)

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
                
                if (hour % 3 == 0)
                {
                    await _taskRunService.CountAlarmAsync(hour);
                }
            };

            // 2. 매 30분 간격 실행 (0분, 30분)
            _timeChekerService.On30MinReached += async (hour, minute) =>
            {
                Console.WriteLine($"[ProcessMain] 30Min Reached :{hour}:{minute}");

                if (hour >= 9 && hour <= 15)
                {
                    await _taskRunService.SendStockPrice();
                }
            };

            // 3. 매 10분 간격 실행: RSS 체크 (비즈니스 로직 위임)
            _timeChekerService.On10MinReached += async (hour, minute) =>
            {
                await _taskRunService.CheckRssNewsAsync();

            };

            // 4. 매 1분 간격 실행
            _timeChekerService.On1MinReached += async (hour, minute) => 
            {

                // 필요 시 추가
            };

            _timeChekerService.Start();
            await Task.Delay(Timeout.Infinite);
        }

        private async Task SetLoadAsync()
        {
            _bot = new TelegramService();
            _supabase = new ServiceSupabase();

            _naverFinanceService = new NaverFinanceService();
            var rssService = new RssService(); // 로컬 생성 후 주입
            
            // TaskRunService 생성자에 rssService 전달
            _taskRunService = new TaskRunService(telegram: _bot, supabase: _supabase, naverFinance:_naverFinanceService, rssService: rssService);

            // RSS 서비스 초기화 호출
            await _taskRunService.InitializeRssAsync();
        }
    }
}
