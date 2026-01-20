using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HeyNowBot.Service
{
    public interface ITelegramService
    {
        Task SendMessageAsync(string text);
    }
    public class TelegramService : ITelegramService
    {
        private const string _botToken = "8439410251:AAEnbnXVmQfzJTNg9PF8Ik8V7q7mVLnCJoo";
        private const long _chatId = 7747196424;
        //private static readonly Lazy<TelegramService> _instance =new Lazy<TelegramService>(() => new TelegramService());
        //public static TelegramService Instance => _instance.Value;
        private readonly TelegramBotClient _bot;

        public TelegramService()
        {
            _bot = new TelegramBotClient(_botToken);
        }
        public async Task SendMessageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

#if DEBUG
            Console.WriteLine($"[TelegramService] 전송 메시지: {text}");
            return;
#endif
            try
            {
                await _bot.SendMessage(
                    chatId: _chatId,
                    text: text
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramService] 전송 오류: {ex.Message}");
            }
        }
    }
}
