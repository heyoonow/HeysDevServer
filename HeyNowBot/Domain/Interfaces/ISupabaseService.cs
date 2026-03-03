using System.Collections.Generic;
using System.Threading.Tasks;
using HeyNowBot.Domain.Entities;

namespace HeyNowBot.Domain.Interfaces
{
    public interface ISupabaseService
    {
        Task<List<VisitLog>?> GetVisitListAsync();
        Task UpdateVisitLogAsync();
    }
}
