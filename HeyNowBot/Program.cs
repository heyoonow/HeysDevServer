// See https://aka.ms/new-console-template for more information
using HeyNowBot;
using HeyNowBot.Service;

Console.WriteLine($"[DEBUG] TELEGRAM_BOT_TOKEN: {System.Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? "NOT SET"}");
Console.WriteLine($"[DEBUG] TELEGRAM_CHAT_ID: {System.Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID") ?? "NOT SET"}");

var telegramService = new TelegramService();
var supabaseService = new SupabaseService();
var rssService = new RssService();
var naverFinanceService = new NaverFinanceService();
var taskRunService = new TaskRunService(supabaseService, naverFinanceService, rssService);
var timeCheckerService = new TimeCheckerService();
var messageQueue = new MessageQueue(telegramService);

var processMain = new ProcessMain(
    telegramService,
    supabaseService,
    taskRunService,
    timeCheckerService,
    messageQueue
);

// 정상 모드
await processMain.RunAsync();
