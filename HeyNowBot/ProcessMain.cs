using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeyNowBot.Service;

namespace HeyNowBot
{
    public class ProcessMain
    {
        private ITelegramService _bot;
        private IServiceSupabase _supabase;
        private ITaskRunService _taskRunService;
        private TimeChekerService _timeChekerService;
        private NaverFinanceService _naverFinanceService;

        public async Task RunAsync()
        {
            await SetLoadAsync();
            _timeChekerService = new TimeChekerService();

            await _bot.SendMessageAsync("[HeyNowBot] 시작");

            _timeChekerService.OnHourReached += async (hour, minute) =>
            {
                var parts = new List<string>();

                // 3시간마다 방문자
                if (hour % 3 == 0)
                {
                    var msg = await _taskRunService.GetCountAlarmMessageAsync(hour);
                    if (!string.IsNullOrWhiteSpace(msg))
                        parts.Add(msg);
                }

                // RSS
                var rssMsg = await _taskRunService.GetRssNewsMessageAsync(isDebug: false);
                if (!string.IsNullOrWhiteSpace(rssMsg))
                    parts.Add(rssMsg);

                // 11:00~15:00 정각 주가(1시간 간격)
                if (hour >= 11 && hour <= 15)
                {
                    // 주말/장외면 차단은 IsMarketOpenAsync에서(주말 false 포함)
                    if (await _naverFinanceService.IsMarketOpenAsync("360750"))
                    {
                        var msg = await _taskRunService.GetStockPriceMessageAsync();
                        if (!string.IsNullOrWhiteSpace(msg))
                            parts.Add(msg);
                    }
                }

                if (parts.Count == 0)
                    return;

                await _bot.SendMessageAsync(string.Join("\n\n", parts));
            };

            _timeChekerService.On10MinReached += async (hour, minute) =>
            {
                // 09:00~09:59 10분 간격
                if (hour != 9)
                    return;

                // 주말/장외면 미발송
                if (!await _naverFinanceService.IsMarketOpenAsync("360750"))
                    return;

                var msg = await _taskRunService.GetStockPriceMessageAsync();
                if (!string.IsNullOrWhiteSpace(msg))
                    await _bot.SendMessageAsync(msg);
            };

            _timeChekerService.On30MinReached += async (hour, minute) =>
            {
                // 10:00~10:59 30분 간격
                if (hour != 10)
                    return;

                // 주말/장외면 미발송
                if (!await _naverFinanceService.IsMarketOpenAsync("360750"))
                    return;

                var msg = await _taskRunService.GetStockPriceMessageAsync();
                if (!string.IsNullOrWhiteSpace(msg))
                    await _bot.SendMessageAsync(msg);
            };

            _timeChekerService.Start();
            await Task.Delay(Timeout.Infinite);
        }

        private async Task SetLoadAsync()
        {
            _bot = new TelegramService();
            _supabase = new ServiceSupabase();

            _naverFinanceService = new NaverFinanceService();
            var rssService = new RssService();

            _taskRunService = new TaskRunService(
                supabase: _supabase,
                naverFinance: _naverFinanceService,
                rssService: rssService
            );

            await _taskRunService.InitializeRssAsync();
        }
    }
}
