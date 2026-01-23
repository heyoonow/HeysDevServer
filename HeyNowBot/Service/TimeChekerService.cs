using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    public class TimeChekerService
    {
        private Timer _timer;

        public event Func<int, int, Task> OnHourReached;
        public event Func<int, int, Task> On30MinReached;
        public event Func<int, int, Task> On10MinReached;
        public event Func<int, int, Task> On1MinReached;

        public void Start()
        {
            // 다음 "분"의 0초에 맞춰 얼라인 (예: 09:10:00)
            var now = DateTime.Now;
            var nextMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
            var dueTimeMs = (int)Math.Max(0, (nextMinute - now).TotalMilliseconds);

            _timer = new Timer(CheckTime, null, dueTimeMs, 60000); // 이후 1분마다
            Console.WriteLine($"[TimeChekerService] 타이머 시작 (다음 분 정각까지 {dueTimeMs}ms, 이후 1분 간격)");
        }

        public void Stop()
        {
            _timer?.Dispose();
            Console.WriteLine("[TimeChekerService] 타이머 중지");
        }

        private void CheckTime(object state)
        {
            var now = DateTime.Now;
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
                    Console.WriteLine($"[TimeChekerService] 이벤트 처리 오류: {ex}");
                }
            }
        }
    }
}
