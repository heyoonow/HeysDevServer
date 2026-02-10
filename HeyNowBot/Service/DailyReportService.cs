using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    /// <summary>
    /// 일일 보고서 생성 및 전송 서비스
    /// 전날의 방문자 통계, 주식 정보, RSS 뉴스 등을 모아서 텍스트 형식으로 생성
    /// </summary>
    public interface IDailyReportService
    {
        Task<bool> SendDailyReportAsync();
    }

    public class DailyReportService : IDailyReportService
    {
        private readonly ITaskRunService _taskRunService;
        private readonly IEmailService _emailService;

        public DailyReportService(ITaskRunService taskRunService, IEmailService emailService)
        {
            _taskRunService = taskRunService ?? throw new ArgumentNullException(nameof(taskRunService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<bool> SendDailyReportAsync()
        {
            try
            {
                Log("일일 보고서 생성 시작");

                var body = await GenerateReportTextAsync();
                var yesterday = DateTime.Now.AddDays(-1);
                var subject = string.Format(Constants.Email.SubjectFormat, yesterday.ToString("yyyy-MM-dd"));

                var result = await _emailService.SendEmailAsync(subject, body, isHtml: false);
                
                if (result)
                    Log("일일 보고서 전송 완료");
                else
                    Log("일일 보고서 전송 실패");

                return result;
            }
            catch (Exception ex)
            {
                Log($"일일 보고서 생성 오류: {ex.Message}");
                return false;
            }
        }

        private async Task<string> GenerateReportTextAsync()
        {
            var sb = new StringBuilder();

            // Flutter 뉴스만 가져오기
            var flutterNews = await _taskRunService.GetKeywordNewsMessageAsync("Flutter");

            if (!string.IsNullOrWhiteSpace(flutterNews))
            {
                sb.AppendLine(flutterNews);
            }
            else
            {
                sb.AppendLine("?? Flutter 관련 뉴스: 없음");
            }

            return sb.ToString();
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [DailyReportService] {message}");
        }
    }
}
