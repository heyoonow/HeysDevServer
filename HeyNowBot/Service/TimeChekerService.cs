using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    public class TimeChekerService
    {
        private Timer _timer;
        private int _lastTriggeredHour = -1;
        private int _lastTriggeredMinute = -1;
        
        // 기존: 1시간(정각) 알림
        public event Action<int, int> OnHourReached;
        
        // 추가: 30분, 10분, 1분 단위 알림
        public event Action<int, int> On30MinReached;
        public event Action<int, int> On10MinReached;
        public event Action<int, int> On1MinReached;

        public void Start()
        {
            // 1분 감지를 위해 10초(10,000ms) 간격 체크로 변경하여 정확도 향상
            // (기존 30초도 가능하지만 1분 이벤트 누락 방지를 위해 단축)
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
            
            // 같은 분(Minute)에 중복 실행 방지
            if (_lastTriggeredHour == now.Hour && _lastTriggeredMinute == now.Minute)
                return;

            // 트리거 타임 업데이트
            _lastTriggeredHour = now.Hour;
            _lastTriggeredMinute = now.Minute;
            
            Console.WriteLine($"[TimeChecker] Tick | now={now:yyyy-MM-dd HH:mm:ss}");

            // 1. 매 1분 마다 실행
            Task.Run(() => On1MinReached?.Invoke(now.Hour, now.Minute));

            // 2. 매 10분 (0, 10, 20, 30, 40, 50분)
            if (now.Minute % 10 == 0)
            {
                Task.Run(() => On10MinReached?.Invoke(now.Hour, now.Minute));
            }

            // 3. 매 30분 (0, 30분)
            if (now.Minute % 30 == 0)
            {
                Task.Run(() => On30MinReached?.Invoke(now.Hour, now.Minute));
            }

            // 4. 매 1시간 (정각 0분)
            // 기존에는 30분에도 울렸으나, '1시간마다'라는 정의에 맞춰 정각에만 울리도록 수정
            if (now.Minute == 0)
            {
                Task.Run(() => OnHourReached?.Invoke(now.Hour, now.Minute));
            }
        }
    }
}
