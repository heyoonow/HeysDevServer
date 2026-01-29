using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    public class TimeChekerService
    {
        private Timer _timer;
        private long _lastMinuteKey = -1;

        public event Func<int, int, Task> OnHourReached;
        public event Func<int, int, Task> On30MinReached;
        public event Func<int, int, Task> On10MinReached;
        public event Func<int, int, Task> On1MinReached;

        public void Start()
        {
            _timer = new Timer(CheckTime, null, 0, 1000); // 1초마다 체크
            Log("타이머 시작 (1초 간격, 분 단위 1회 트리거)");
        }

        public void Stop()
        {
            _timer?.Dispose();
            Log("타이머 중지");
        }

        private void CheckTime(object state)
        {
            var now = DateTime.Now;

            // 분 단위로 1회만 실행되게 키 생성 (YYYYMMDDHHmm)
            var minuteKey =
                (now.Year * 100000000L) +
                (now.Month * 1000000L) +
                (now.Day * 10000L) +
                (now.Hour * 100L) +
                now.Minute;

            if (_lastMinuteKey == minuteKey)
                return;

            _lastMinuteKey = minuteKey;
            _ = FireAsync(now);
        }

        private async Task FireAsync(DateTime now)
        {
            await SafeInvokeAsync(On1MinReached, now.Hour, now.Minute);

            if (now.Minute % 10 == 0)
                await SafeInvokeAsync(On10MinReached, now.Hour, now.Minute);

            if (now.Minute % 30 == 0)
                await SafeInvokeAsync(On30MinReached, now.Hour, now.Minute);

            if (now.Minute == 0)
                await SafeInvokeAsync(OnHourReached, now.Hour, now.Minute);
        }

        private static async Task SafeInvokeAsync(Func<int, int, Task> handler, int hour, int minute)
        {
            if (handler is null) return;

            foreach (var d in handler.GetInvocationList())
            {
                try
                {
                    await ((Func<int, int, Task>)d)(hour, minute);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [TimeChekerService] 이벤트 처리 오류: {ex}");
                }
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [TimeChekerService] {message}");
        }
    }
}
