using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    /// <summary>
    /// 시간 기반 이벤트 트리거 서비스
    /// 분 단위로 1회 실행되며, 1분, 10분, 30분, 1시간 주기 이벤트를 발생시킴
    /// </summary>
    public interface ITimeCheckerService
    {
        event Func<int, int, Task> OnHourReached;
        event Func<int, int, Task> On30MinReached;
        event Func<int, int, Task> On10MinReached;
        event Func<int, int, Task> On1MinReached;

        void Start();
        void Stop();
    }

    public class TimeCheckerService : ITimeCheckerService
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
            Log(Constants.Message.TimerStartMessage);
        }

        public void Stop()
        {
            _timer?.Dispose();
            Log(Constants.Message.TimerStopMessage);
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
            if (handler is null)
                return;

            foreach (var d in handler.GetInvocationList())
            {
                try
                {
                    await ((Func<int, int, Task>)d)(hour, minute);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [TimeCheckerService] 이벤트 처리 오류: {ex.Message}");
                }
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [TimeCheckerService] {message}");
        }
    }

    // 레거시 호환성 유지를 위한 별칭
    [Obsolete("TimeCheckerService를 사용하세요")]
    public class TimeChekerService : TimeCheckerService { }
}
