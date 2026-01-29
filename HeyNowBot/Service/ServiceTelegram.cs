using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
        private readonly TelegramBotClient _bot;

        public TelegramService()
        {
            _bot = new TelegramBotClient(_botToken);
        }

        public async Task SendMessageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            Log($"전송 메시지: {text}");

            try
            {
                await _bot.SendMessage(
                    chatId: _chatId,
                    text: text,
                    linkPreviewOptions: new LinkPreviewOptions
                    {
                        IsDisabled = true
                    }
                );
            }
            catch (Exception ex)
            {
                Log($"전송 실패: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [TelegramService] {message}");
        }
    }   
}
