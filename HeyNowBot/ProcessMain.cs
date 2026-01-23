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

            await _bot.SendMessageAsync($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [HeyNowBot] 봇이 시작되었습니다.");

            // 1) 정각(매시간): 정각 리포트 + (3시간마다 방문자) + (RSS)
            _timeChekerService.OnHourReached += async (hour, minute) =>
            {
                var now = DateTime.Now;
                var sb = new StringBuilder();

                sb.AppendLine($"🕒 {now:yyyy-MM-dd HH:mm}  |  [HeyNowBot] 정각 리포트");
                sb.AppendLine($"- 현재 시간: {hour:00}:{minute:00}");
                sb.AppendLine("--------------------");

                var hasBody = false;

                if (hour % 3 == 0)
                {
                    var msg = await _taskRunService.GetCountAlarmMessageAsync(hour);
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        hasBody = true;
                        sb.AppendLine("👥 방문자");
                        sb.AppendLine(msg);
                        sb.AppendLine("--------------------");
                    }
                }

                var rssMsg = await _taskRunService.GetRssNewsMessageAsync(isDebug: false);
                if (!string.IsNullOrWhiteSpace(rssMsg))
                {
                    hasBody = true;
                    sb.AppendLine("📰 RSS");
                    sb.AppendLine(rssMsg);
                    sb.AppendLine("--------------------");
                }

                // 11:00~15:00 정각에만 주가(1시간 간격)
                if (hour >= 11 && hour <= 15)
                {
                    var msg = await _taskRunService.GetStockPriceMessageAsync();
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        hasBody = true;
                        sb.AppendLine("📈 주가");
                        sb.AppendLine(msg);
                        sb.AppendLine("--------------------");
                    }
                }

                if (!hasBody)
                {
                    sb.AppendLine("✅ 업데이트 없음");
                    sb.AppendLine("이번 정각에는 전달할 신규 정보가 없습니다.");
                    sb.AppendLine("--------------------");
                }

                sb.AppendLine("끝.");

                await _bot.SendMessageAsync(sb.ToString().Trim());
            };

            // 2) 10분 간격: 09:00~09:59 에만 주가 (장 시간 아닐 때는 미발송)
            _timeChekerService.On10MinReached += async (hour, minute) =>
            {
                // 장 시간(09:00~15:00) 아니면 종료
                if (hour < 9 || hour > 15)
                    return;

                // 09시대만 10분 간격
                if (hour != 9)
                    return;

                var msg = await _taskRunService.GetStockPriceMessageAsync();
                if (string.IsNullOrWhiteSpace(msg))
                    return;

                var now = DateTime.Now;
                var text =
                    $"🕒 {now:yyyy-MM-dd HH:mm}  |  [HeyNowBot] 주가 알림(10분)\n" +
                    "--------------------\n" +
                    $"📈 주가\n{msg}\n" +
                    "--------------------";

                await _bot.SendMessageAsync(text);
            };

            // 3) 30분 간격: 10:00~10:59 에만 주가 (장 시간 아닐 때는 미발송)
            _timeChekerService.On30MinReached += async (hour, minute) =>
            {
                // 장 시간(09:00~15:00) 아니면 종료
                if (hour < 9 || hour > 15)
                    return;

                // 10시대만 30분 간격
                if (hour != 10)
                    return;

                var msg = await _taskRunService.GetStockPriceMessageAsync();
                if (string.IsNullOrWhiteSpace(msg))
                    return;

                var now = DateTime.Now;
                var text =
                    $"🕒 {now:yyyy-MM-dd HH:mm}  |  [HeyNowBot] 주가 알림(30분)\n" +
                    "--------------------\n" +
                    $"📈 주가\n{msg}\n" +
                    "--------------------";

                await _bot.SendMessageAsync(text);
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
