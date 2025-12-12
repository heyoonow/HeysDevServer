using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeyNowBot.Service;

namespace HeyNowBot
{
    public class ProcessMain
    {
        public ProcessMain()
        {
               
        }
        public async void Run()
        {
            Console.WriteLine("Hello, World!");
            await TelegramService.Instance.SendMessageAsync("Hello from HeyNowBot!");
            await Task.Delay(30000);
        }
    }
}
