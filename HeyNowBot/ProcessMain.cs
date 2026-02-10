using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HeyNowBot.Service;

namespace HeyNowBot
{
    /// <summary>
    /// 애플리케이션 메인 프로세스
    /// 시간 기반 이벤트에 따라 작업을 실행하고 메시지를 전송
    /// </summary>
    public class ProcessMain
    {
        private readonly ITelegramService _bot;
        private readonly ISupabaseService _supabase;
        private readonly ITaskRunService _taskRunService;
        private readonly ITimeCheckerService _timeCheckerService;
        private readonly IMessageQueue _messageQueue;
        private readonly IDailyReportService _dailyReportService;

        public ProcessMain(
            ITelegramService bot,
            ISupabaseService supabase,
            ITaskRunService taskRunService,
            ITimeCheckerService timeCheckerService,
            IMessageQueue messageQueue,
            IDailyReportService dailyReportService)
        {
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
            _taskRunService = taskRunService ?? throw new ArgumentNullException(nameof(taskRunService));
            _timeCheckerService = timeCheckerService ?? throw new ArgumentNullException(nameof(timeCheckerService));
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
            _dailyReportService = dailyReportService ?? throw new ArgumentNullException(nameof(dailyReportService));
        }

        public async Task RunAsync()
        {
            await _bot.SendMessageAsync(Constants.Message.StartMessage);
            Log("RunAsync 시작");

            await _taskRunService.InitializeRssAsync();
            RegisterEventHandlers();
            _timeCheckerService.Start();

            await Task.Delay(Timeout.Infinite);
        }

        private void RegisterEventHandlers()
        {
            _timeCheckerService.OnHourReached += HandleHourReachedAsync;
            _timeCheckerService.On10MinReached += HandleOn10MinReachedAsync;
            _timeCheckerService.On30MinReached += HandleOn30MinReachedAsync;
            _timeCheckerService.OnHourReached += HandleDailyReportAsync;
        }

        private async Task HandleDailyReportAsync(int hour, int minute)
        {
            try
            {
                // 아침 8시에만 실행
                if (hour != Constants.Email.DailyReportHour || minute != Constants.Email.DailyReportMinute)
                    return;

                Log("일일 보고서 전송 시작");
                await _dailyReportService.SendDailyReportAsync();
            }
            catch (Exception ex)
            {
                Log($"일일 보고서 전송 오류: {ex.Message}");
            }
        }

        private async Task HandleHourReachedAsync(int hour, int minute)
        {
            try
            {
                var parts = new List<string>();

                // 3시간 단위 방문자 통계
                if (hour % Constants.Schedule.VisitCountIntervalHours == 0)
                {
                    var msg = await _taskRunService.GetCountAlarmMessageAsync(hour);
                    if (!string.IsNullOrWhiteSpace(msg))
                        parts.Add(msg);
                }

                // RSS 뉴스
                var rssMsg = await _taskRunService.GetRssNewsMessageAsync(isDebug: false);
                if (!string.IsNullOrWhiteSpace(rssMsg))
                    parts.Add(rssMsg);

                if (parts.Count > 0)
                {
                    _messageQueue.Enqueue(string.Join("\n\n", parts));
                }
            }
            catch (Exception ex)
            {
                Log($"시간 도달 이벤트 처리 오류: {ex.Message}");
            }
        }

        private async Task HandleOn10MinReachedAsync(int hour, int minute)
        {
            try
            {
                if (!IsStockTradingHours(hour, minute))
                    return;

                if (hour < 9 || hour >= Constants.Schedule.StockMarketMorningEndHour)
                    return;

                var msg = await _taskRunService.GetStockPriceMessageAsync();
                if (!string.IsNullOrWhiteSpace(msg))
                    _messageQueue.Enqueue(msg);
            }
            catch (Exception ex)
            {
                Log($"10분 이벤트 처리 오류: {ex.Message}");
            }
        }

        private async Task HandleOn30MinReachedAsync(int hour, int minute)
        {
            try
            {
                if (!IsStockTradingHours(hour, minute))
                    return;

                if (hour < Constants.Schedule.StockMarketMorningEndHour)
                    return;

                var msg = await _taskRunService.GetStockPriceMessageAsync();
                if (!string.IsNullOrWhiteSpace(msg))
                    _messageQueue.Enqueue(msg);
            }
            catch (Exception ex)
            {
                Log($"30분 이벤트 처리 오류: {ex.Message}");
            }
        }

        private static bool IsStockTradingHours(int hour, int minute)
        {
            var now = DateTime.Now;

            // 주말 제외
            if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return false;

            // 시장 시간 확인 (09:00 ~ 15:30)
            if (hour < Constants.Schedule.StockMarketStartHour)
                return false;

            if (hour > Constants.Schedule.StockMarketEndHour)
                return false;

            if (hour == Constants.Schedule.StockMarketEndHour && 
                minute > Constants.Schedule.StockMarketEndMinute)
                return false;

            return true;
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ProcessMain] {message}");
        }
    }
}
