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
        
        public event Action<int> OnHourReached;

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

            if (now.Minute == 0 && now.Second < 1)
            {
                if (_lastTriggeredHour != currentHour)
                {
                    _lastTriggeredHour = currentHour;
                    OnHourReached?.Invoke(currentHour);
                }
            }
            else if (now.Minute > 0)
            {
                _lastTriggeredHour = -1;
            }
        }
    }
}
