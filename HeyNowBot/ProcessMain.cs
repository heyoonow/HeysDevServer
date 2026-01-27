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

        // 같은 분에 여러 트리거가 겹쳐도 1번만 보내기 위한 배치 버퍼
        private readonly object _sendLock = new object();
        private readonly List<string> _pendingMessages = new List<string>();
        private CancellationTokenSource _flushCts;

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

                if (parts.Count == 0)
                    return;

                QueueMessage(string.Join("\n\n", parts));
                ScheduleFlush();
            };

            // 주식 알림: 11시 이전(09:00~10:59) = 10분 간격 (주말 스킵)
            _timeChekerService.On10MinReached += async (hour, minute) =>
            {
                if (IsWeekend(DateTime.Now))
                    return;

                // 09~10시대만 10분 간격
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
                if (IsWeekend(DateTime.Now))
                    return;

                // 11:00 ~ 15:30만 허용
                if (hour < 11)
                    return;

                if (hour > 15)
                    return;

                if (hour == 15 && minute > 30)
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
