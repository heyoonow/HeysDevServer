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
            _timer = new Timer(CheckTime, null, 0, 400);
        }

        public void Stop()
        {
            _timer?.Dispose();
        }

        private void CheckTime(object state)
        {
            var now = DateTime.Now;
            int currentHour = now.Hour;
            int currentMinute = now.Minute;

            if (currentMinute % 30 == 0 && now.Second < 1)
            {
                if (_lastTriggeredHour != currentHour || _lastTriggeredMinute != currentMinute)
                {
                    Console.WriteLine(
    $"[TimeChecker] fire event | now={DateTime.Now:yyyy-MM-dd HH:mm:ss} | hour={currentHour} | minute={currentMinute}"
);

                    _lastTriggeredHour = currentHour;
                    _lastTriggeredMinute = currentMinute;
                    OnHourReached?.Invoke(currentHour, currentMinute);
                }
            }
            else if (currentMinute % 30 != 0)
            {
                _lastTriggeredHour = -1;
                _lastTriggeredMinute = -1;
            }
        }
    }
}
