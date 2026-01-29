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

        private readonly object _sendLock = new object();
        private readonly List<string> _pendingMessages = new List<string>();
        private CancellationTokenSource _flushCts;

        public async Task RunAsync()
        {
            await SetLoadAsync();
            _timeChekerService = new TimeChekerService();

            await _bot.SendMessageAsync("[HeyNowBot] 시작");
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ProcessMain] RunAsync started");

            _timeChekerService.OnHourReached += async (hour, minute) =>
            {
                var parts = new List<string>();

                if (hour % 3 == 0)
                {
                    var msg = await _taskRunService.GetCountAlarmMessageAsync(hour);
                    if (!string.IsNullOrWhiteSpace(msg))
                        parts.Add(msg);
                }

                var rssMsg = await _taskRunService.GetRssNewsMessageAsync(isDebug: false);
                if (!string.IsNullOrWhiteSpace(rssMsg))
                    parts.Add(rssMsg);

                if (parts.Count == 0)
                    return;

                QueueMessage(string.Join("\n\n", parts));
                ScheduleFlush();
            };

            // 주식 알림: 11시 이전(09:00~10:59) = 10분 간격 (15:30 가드 포함, 주말 스킵)
            _timeChekerService.On10MinReached += async (hour, minute) =>
            {
                var now = DateTime.Now;

                if (!IsStockTime(now))
                    return;

                if (hour < 9 || hour >= 11)
                    return;

                var msg = await _taskRunService.GetStockPriceMessageAsync();
                if (string.IsNullOrWhiteSpace(msg))
                    return;

                QueueMessage(msg);
                ScheduleFlush();
            };

            // 주식 알림: 11시 이후(11:00~15:30) = 30분 간격 (주말 스킵)
            _timeChekerService.On30MinReached += async (hour, minute) =>
            {
                var now = DateTime.Now;

                if (!IsStockTime(now))
                    return;

                if (hour < 11)
                    return;

                var msg = await _taskRunService.GetStockPriceMessageAsync();
                if (string.IsNullOrWhiteSpace(msg))
                    return;

                QueueMessage(msg);
                ScheduleFlush();
            };

            _timeChekerService.Start();
            await Task.Delay(Timeout.Infinite);
        }

        private static bool IsWeekend(DateTime now)
            => now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        // 추가: 주식 알림 시간(09:00 ~ 15:30) 공통 가드
        private static bool IsStockTime(DateTime now)
        {
            if (IsWeekend(now))
                return false;

            if (now.Hour < 9)
                return false;

            if (now.Hour > 15)
                return false;

            if (now.Hour == 15 && now.Minute > 30)
                return false;

            return true;
        }

        private void QueueMessage(string message)
        {
            lock (_sendLock)
            {
                _pendingMessages.Add(message);
            }
        }

        private void ScheduleFlush()
        {
            CancellationTokenSource cts;

            lock (_sendLock)
            {
                _flushCts?.Cancel();
                _flushCts?.Dispose();
                _flushCts = new CancellationTokenSource();
                cts = _flushCts;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(400, cts.Token);
                    await FlushAsync();
                }
                catch (OperationCanceledException)
                {
                }
            });
        }

        private async Task FlushAsync()
        {
            List<string> batch;

            lock (_sendLock)
            {
                if (_pendingMessages.Count == 0)
                    return;

                batch = new List<string>(_pendingMessages);
                _pendingMessages.Clear();
            }

            var text = string.Join("\n\n", new HashSet<string>(batch)).Trim();
            if (!string.IsNullOrWhiteSpace(text))
                await _bot.SendMessageAsync(text);
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
