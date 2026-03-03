using System.Collections.Generic;
using System.Threading.Tasks;
using HeyNowBot.Domain.Entities;

namespace HeyNowBot.Domain.Interfaces
{
    public interface ITaskRunService
    {
        Task<string?> GetCountAlarmMessageAsync(int hour);
        Task<string?> GetStockPriceMessageAsync();
        Task InitializeRssAsync();
        Task<string?> GetRssNewsMessageAsync(bool isDebug = false);
        Task<List<VisitLog>?> GetVisitListAsync();
    }
}
