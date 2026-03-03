using System.Collections.Generic;
using System.Threading.Tasks;
using HeyNowBot.Domain.Entities;

namespace HeyNowBot.Domain.Interfaces
{
    public interface IRssService
    {
        Task<List<RssItem>> GetNewFeedsAsync(string rssUrl, bool isDebug = false);
    }
}
