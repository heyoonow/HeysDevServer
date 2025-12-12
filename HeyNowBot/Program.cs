// See https://aka.ms/new-console-template for more information
using HeyNowBot.Service;
using System;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            await TelegramService.Instance.SendMessageAsync("Hello from HeyNowBot!");
            await Task.Delay(30000);
        }
    }
}