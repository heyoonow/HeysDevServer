using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    /// <summary>
    /// 메시지 큐 및 배치 전송 서비스
    /// 야간 조용한 시간대를 피하고, 중복 메시지를 제거한 후 배치 전송
    /// </summary>
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
            // 전송 금지 시간대 확인
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
                    // 플러시 취소됨
                }
                catch (Exception ex)
                {
                    Log($"플러시 오류: {ex.Message}");
                }
            });
        }

        private async Task FlushAsync()
        {
            // 최종 방어선: 전송 금지 시간대 확인
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

            // 중복 제거 후 전송
            var uniqueMessages = new HashSet<string>(batch);
            var finalMessage = string.Join("\n\n", uniqueMessages).Trim();

            if (!string.IsNullOrWhiteSpace(finalMessage))
            {
                Log($"메시지 전송 ({uniqueMessages.Count}개)");
                await _telegramService.SendMessageAsync(finalMessage);
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
