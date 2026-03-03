using System.Threading.Tasks;
using HeyNowBot.Domain.Entities;

namespace HeyNowBot.Domain.Interfaces
{
    public interface INaverFinanceService
    {
        Task<StockInfo> GetStockInfoAsync(string stockCode);
        Task<bool> IsMarketOpenAsync(string stockCode);
    }
}
