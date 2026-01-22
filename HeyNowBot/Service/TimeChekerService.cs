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
        private int _lastTriggeredHour = -1;
        private int _lastTriggeredMinute = -1;
        
        public event Action<int, int> OnHourReached;

        public void Start()
        {
            // 30초마다 체크 (30,000ms = 30초)
            // 이렇게 하면 0분과 30분을 확실히 잡을 수 있음
            _timer = new Timer(CheckTime, null, 0, 30000);
            
            Console.WriteLine("[TimeChekerService] 타이머 시작 (30초 간격)");
        }

        public void Stop()
        {
            _timer?.Dispose();
            Console.WriteLine("[TimeChekerService] 타이머 중지");
        }

        private void CheckTime(object state)
        {
            var now = DateTime.Now;
            
            // 0분 또는 30분인지 체크
            if (now.Minute != 0 && now.Minute != 30)
                return;

            // 이미 트리거했는지 체크
            if (_lastTriggeredHour == now.Hour && _lastTriggeredMinute == now.Minute)
                return;

            // 트리거
            _lastTriggeredHour = now.Hour;
            _lastTriggeredMinute = now.Minute;
            
            Console.WriteLine($"[TimeChecker] fire event | now={now:yyyy-MM-dd HH:mm:ss} | hour={now.Hour} | minute={now.Minute}");
            
            // 이벤트를 별도 Task로 실행
            Task.Run(() => OnHourReached?.Invoke(now.Hour, now.Minute));
        }
    }
}
