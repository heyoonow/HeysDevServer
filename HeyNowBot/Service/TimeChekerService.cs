using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    public class TimeChekerService
    {
        private Timer _timer;
        
        public event Action<int, int> OnHourReached;

        public void Start()
        {
            ScheduleNextTrigger();
        }

        public void Stop()
        {
            _timer?.Dispose();
        }

        private void ScheduleNextTrigger()
        {
            var now = DateTime.Now;
            var next = GetNextTriggerTime(now);
            var delay = (next - now).TotalMilliseconds;

            Console.WriteLine($"[TimeChecker] 다음 트리거 예정: {next:yyyy-MM-dd HH:mm:ss} (약 {delay / 1000:F0}초 후)");

            _timer?.Dispose();
            _timer = new Timer(OnTimerFired, null, (int)delay, Timeout.Infinite);
        }

        private DateTime GetNextTriggerTime(DateTime current)
        {
            var minute = current.Minute;
            var nextMinute = minute < 30 ? 30 : 60;
            
            var next = new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0);
            
            if (nextMinute == 60)
            {
                next = next.AddHours(1);
            }
            else
            {
                next = next.AddMinutes(30);
            }

            return next;
        }

        private void OnTimerFired(object state)
        {
            var now = DateTime.Now;
            Console.WriteLine($"[TimeChecker] fire event | now={now:yyyy-MM-dd HH:mm:ss} | hour={now.Hour} | minute={now.Minute}");
            
            // 다음 타이머를 먼저 예약하여 이벤트 실행 시간에 영향받지 않도록 함
            ScheduleNextTrigger();
            
            // 이벤트를 별도 Task로 실행
            Task.Run(() => OnHourReached?.Invoke(now.Hour, now.Minute));
        }
    }
}
