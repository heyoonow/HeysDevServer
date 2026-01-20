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
            
            // 다음 타이머를 먼저 예약하여 이벤트 실행 시간에 영향받지 않도록 함
            ScheduleNextTrigger();
            
            // 이벤트를 별도 Task로 실행
            Task.Run(() => OnHourReached?.Invoke(now.Hour, now.Minute));
        }
    }
}
