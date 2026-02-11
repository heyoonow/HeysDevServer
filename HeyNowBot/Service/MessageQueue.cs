using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    public interface IMessageQueue
    {
        void Enqueue(string message);
    }

    public class MessageQueue : IMessageQueue
    {
        private readonly ITelegramService _telegramService;
        private readonly object _lockObj = new();
        private readonly List<string> _pendingMessages = new();
        private CancellationTokenSource _flushCts;

        public MessageQueue(ITelegramService telegramService)
        {
            _telegramService = telegramService ?? throw new ArgumentNullException(nameof(telegramService));
        }

        public void Enqueue(string message)
        {
            if (!IsSendAllowed())
            {
                Log($"야간 시간대 - 메시지 버림: {message[..Math.Min(30, message.Length)]}");
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (_lockObj)
            {
                _pendingMessages.Add(message);
                Log($"메시지 큐 추가 (대기중: {_pendingMessages.Count}개)");
            }

            ScheduleFlush();
        }

        private void ScheduleFlush()
        {
            CancellationTokenSource cts;

            lock (_lockObj)
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
                    await Task.Delay(Constants.Schedule.MessageFlushDelayMs, cts.Token);
                    await FlushAsync();
                }
                catch (OperationCanceledException)
                {
                    Log("플러시 취소됨");
                }
                catch (Exception ex)
                {
                    Log($"플러시 오류: {ex.Message}");
                }
            });
        }

        private async Task FlushAsync()
        {
            if (!IsSendAllowed())
            {
                lock (_lockObj)
                {
                    Log($"야간 시간대 - 큐 비움: {_pendingMessages.Count}개 메시지");
                    _pendingMessages.Clear();
                }
                return;
            }

            List<string> batch;

            lock (_lockObj)
            {
                if (_pendingMessages.Count == 0)
                    return;

                batch = new List<string>(_pendingMessages);
                _pendingMessages.Clear();
            }

            var uniqueMessages = new HashSet<string>(batch);
            var finalMessage = string.Join("\n\n", uniqueMessages).Trim();

            if (!string.IsNullOrWhiteSpace(finalMessage))
            {
                Log($"메시지 전송 시작 ({uniqueMessages.Count}개)");
                try
                {
                    await _telegramService.SendMessageAsync(finalMessage);
                    Log("텔레그램 전송 완료!");
                }
                catch (Exception ex)
                {
                    Log($"텔레그램 전송 실패: {ex.Message}");
                }
            }
        }

        private static bool IsSendAllowed()
        {
            if (!Constants.Schedule.EnableQuietHours)
                return true;

            var hour = DateTime.Now.Hour;
            return !(hour >= Constants.Schedule.QuietHourStart || hour < Constants.Schedule.QuietHourEnd);
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [MessageQueue] {message}");
        }
    }
}
