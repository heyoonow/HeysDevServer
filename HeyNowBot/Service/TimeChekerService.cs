using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    public class TimeChekerService
    {
        private Timer _timer;
        private int _lastTriggeredHour = -1;
        private int _lastTriggeredMinute = -1;

        public event Func<int, int, Task> OnHourReached;
        public event Func<int, int, Task> On30MinReached;
        public event Func<int, int, Task> On10MinReached;
        public event Func<int, int, Task> On1MinReached;

        public void Start()
        {
            _timer = new Timer(CheckTime, null, 0, 10000);
            Console.WriteLine("[TimeChekerService] 타이머 시작 (10초 간격)");
        }

        public void Stop()
        {
            _timer?.Dispose();
            Console.WriteLine("[TimeChekerService] 타이머 중지");
        }

        private void CheckTime(object state)
        {
            var now = DateTime.Now;

            if (_lastTriggeredHour == now.Hour && _lastTriggeredMinute == now.Minute)
                return;

            _lastTriggeredHour = now.Hour;
            _lastTriggeredMinute = now.Minute;

            Console.WriteLine($"[TimeChecker] Tick | now={now:yyyy-MM-dd HH:mm:ss}");

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
