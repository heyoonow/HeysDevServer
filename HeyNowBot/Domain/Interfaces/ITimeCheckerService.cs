using System;
using System.Threading.Tasks;

namespace HeyNowBot.Domain.Interfaces
{
    public interface ITimeCheckerService
    {
        event Func<int, int, Task> OnHourReached;
        event Func<int, int, Task> On30MinReached;
        event Func<int, int, Task> On10MinReached;
        event Func<int, int, Task> On1MinReached;

        void Start();
        void Stop();
    }
}
