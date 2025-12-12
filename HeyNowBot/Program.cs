// See https://aka.ms/new-console-template for more information
using HeyNowBot.Service;


while (true)
{
    Console.WriteLine("Hello, World!");
    await TelegramService.Instance.SendMessageAsync("Hello from HeyNowBot!");
    await Task.Delay(30000);
}