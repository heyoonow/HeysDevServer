// See https://aka.ms/new-console-template for more information
using HeyNowBot;
using HeyNowBot.Service;

var telegramService = new TelegramService();
var emailService = new EmailService();
var supabaseService = new SupabaseService();
var rssService = new RssService();
var naverFinanceService = new NaverFinanceService();
var taskRunService = new TaskRunService(supabaseService, naverFinanceService, rssService);
var timeCheckerService = new TimeCheckerService();
var messageQueue = new MessageQueue(telegramService);
var dailyReportService = new DailyReportService(taskRunService, emailService);

var telegramBot = new TelegramService();
var processMain = new ProcessMain(
    telegramBot,
    supabaseService,
    taskRunService,
    timeCheckerService,
    messageQueue,
    dailyReportService
);

// 정상 모드: 아침 8시에 자동으로 메일 발송
await processMain.RunAsync();
