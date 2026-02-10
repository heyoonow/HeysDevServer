using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace HeyNowBot.Service
{
    /// <summary>
    /// Telegram 메시지 전송 서비스
    /// </summary>
    public interface ITelegramService
    {
        Task SendMessageAsync(string text);
    }

    public class TelegramService : ITelegramService
    {
        private readonly TelegramBotClient _bot;

        public TelegramService()
        {
            _bot = new TelegramBotClient(Constants.Telegram.BotToken);
        }

        public async Task SendMessageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var preview = text[..Math.Min(50, text.Length)];
            Log($"메시지 전송: {preview}{'…'}");

            try
            {
                await _bot.SendMessage(
                    chatId: Constants.Telegram.ChatId,
                    text: text,
                    linkPreviewOptions: new()
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
