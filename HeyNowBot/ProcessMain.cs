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
        public async Task RunAsync()
        {
            Console.WriteLine("Start");
            await TelegramService.Instance.SendMessageAsync("Hello from HeyNowBot!");
            await Task.Delay(30000);
            Console.WriteLine("Hello, World!");
        }
    }
}
