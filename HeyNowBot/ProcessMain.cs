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

        // [추가] 야간 알림 금지 플래그 (true면 22:00~06:00 전송 금지)
        private const bool EnableQuietHours = true;

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

        // [추가] 야간 알림 금지 시간대(22:00 ~ 다음날 06:00)
        private static bool IsQuietHours(DateTime now)
            => now.Hour >= 22 || now.Hour < 6;

        private bool IsSendAllowed(DateTime now)
            => !EnableQuietHours || !IsQuietHours(now);

        private void QueueMessage(string message)
        {
            // [추가] 전송 금지 시간대면 큐에 쌓지 않음
            if (!IsSendAllowed(DateTime.Now))
                return;

            lock (_sendLock)
            {
                _pendingMessages.Add(message);
            }
        }

        private void ScheduleFlush()
        {
            // [추가] 전송 금지 시간대면 flush 스케줄도 하지 않음
            if (!IsSendAllowed(DateTime.Now))
                return;

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
            // [추가] 최종 방어선: 전송 금지 시간대면 버리고 끝
            if (!IsSendAllowed(DateTime.Now))
            {
                lock (_sendLock)
                {
                    _pendingMessages.Clear();
                }
                return;
            }

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
