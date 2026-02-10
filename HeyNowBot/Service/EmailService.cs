using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HeyNowBot.Service
{
    /// <summary>
    /// SMTP를 이용한 Gmail 이메일 전송 서비스
    /// Gmail 계정 + 앱 비밀번호로 동작
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string subject, string body, bool isHtml = true);
    }

    public class EmailService : IEmailService
    {
        public async Task<bool> SendEmailAsync(string subject, string body, bool isHtml = true)
        {
            if (!Constants.Email.EnableEmailNotification)
            {
                Log("이메일 알림이 비활성화 상태입니다.");
                return false;
            }

            try
            {
                using (var client = new SmtpClient(Constants.Email.SmtpServer, Constants.Email.SmtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.Credentials = new NetworkCredential(
                        Constants.Email.SenderEmail,
                        Constants.Email.GmailAppPassword
                    );

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(
                            Constants.Email.SenderEmail,
                            Constants.Email.SenderName,
                            System.Text.Encoding.UTF8
                        );
                        mailMessage.To.Add(Constants.Email.RecipientEmail);
                        mailMessage.Subject = subject;
                        mailMessage.SubjectEncoding = System.Text.Encoding.UTF8;
                        mailMessage.Body = body;
                        mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
                        mailMessage.IsBodyHtml = isHtml;

                        await client.SendMailAsync(mailMessage);
                    }
                }

                Log($"이메일 전송 성공: {subject}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"이메일 전송 실패: {ex.Message}");
                return false;
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [EmailService] {message}");
        }
    }
}
