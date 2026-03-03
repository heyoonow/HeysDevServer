using System.Threading.Tasks;

namespace HeyNowBot.Domain.Interfaces
{
    public interface ITelegramService
    {
        Task SendMessageAsync(string text);
    }
}
