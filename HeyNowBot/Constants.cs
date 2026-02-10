using System.Collections.Generic;

namespace HeyNowBot
{
    /// <summary>
    /// 애플리케이션 전역 상수 정의
    /// </summary>
    public static class Constants
    {
        public static class Telegram
        {
            /// <summary>Telegram Bot Token (환경변수로 설정 권장)</summary>
            public static string BotToken => 
                System.Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") 
                ?? "8439410251:AAEnbnXVmQfzJTNg9PF8Ik8V7q7mVLnCJoo";

            /// <summary>Telegram Chat ID (환경변수로 설정 권장)</summary>
            public static long ChatId =>
                long.Parse(System.Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID") ?? "7747196424");
        }

        public static class Stock
        {
            /// <summary>모니터링 주식 코드 (삼성전자)</summary>
            public const string MonitoringStockCode = "360750";
        }

        public static class Rss
        {
            /// <summary>기본 RSS 피드 URL 목록</summary>
            public static readonly List<string> FeedUrls = new()
            {
                "https://bbs.ruliweb.com/ps/board/300001/rss",
                "https://bbs.ruliweb.com/mobile/board/300008/rss",
                "https://bbs.ruliweb.com/market/board/1020/rss",
            };

            /// <summary>키워드별 RSS URL 목록 (여러 소스)</summary>
            public static readonly Dictionary<string, List<string>> KeywordRssUrls = new()
            {
                { "Flutter", new List<string>
                {
                    "https://news.google.com/rss/search?q=flutter&hl=ko&gl=KR",
                    "https://www.reddit.com/r/FlutterDev/new/.rss",
                    "https://www.reddit.com/r/flutter/new/.rss",
                    "https://www.reddit.com/r/Dart/new/.rss",
                }},
            };

            /// <summary>키워드별 뉴스 최대 개수</summary>
            public const int MaxKeywordNews = 10;
        }

        public static class Schedule
        {
            /// <summary>야간 알림 금지 활성화 (22:00 ~ 06:00)</summary>
            public const bool EnableQuietHours = true;

            /// <summary>야간 시작 시간</summary>
            public const int QuietHourStart = 22;

            /// <summary>야간 종료 시간</summary>
            public const int QuietHourEnd = 6;

            /// <summary>주식 시장 시작 시간</summary>
            public const int StockMarketStartHour = 9;

            /// <summary>오전 주식 조회 종료 시간</summary>
            public const int StockMarketMorningEndHour = 11;

            /// <summary>오후 주식 시장 종료 시간</summary>
            public const int StockMarketEndHour = 15;

            /// <summary>오후 주식 시장 종료 분</summary>
            public const int StockMarketEndMinute = 30;

            /// <summary>방문자 집계 주기 (시간)</summary>
            public const int VisitCountIntervalHours = 3;

            /// <summary>메시지 배치 플러시 딜레이 (밀리초)</summary>
            public const int MessageFlushDelayMs = 400;

            /// <summary>Supabase 초기화 대기시간 (밀리초)</summary>
            public const int SupabaseInitWaitMs = 300;

            /// <summary>데이터베이스 요청 간 대기시간 (밀리초)</summary>
            public const int DatabaseRequestDelayMs = 100;
        }

        public static class Message
        {
            /// <summary>Telegram 메시지 최대 길이 (여유분 포함)</summary>
            public const int MaxMessageLength = 3500;

            /// <summary>봇 시작 메시지</summary>
            public const string StartMessage = "[HeyNowBot] 시작";

            /// <summary>RSS 초기화 메시지</summary>
            public const string RssInitializationMessage = "RSS 초기화 중...";

            /// <summary>RSS 체크 메시지</summary>
            public const string RssCheckStartMessage = "RSS 체크 시작";

            /// <summary>RSS 초기화 완료 메시지</summary>
            public const string RssInitCompleteMessage = "초기화 완료";

            /// <summary>요청 실패 메시지</summary>
            public const string RequestFailedMessage = "요청 실패";

            /// <summary>파싱 오류 메시지</summary>
            public const string ParsingErrorMessage = "RSS 파싱 오류";

            /// <summary>주식 정보 실패 메시지</summary>
            public const string StockInfoFailedMessage = "[NaverFinanceService] 주식 정보를 가져오는데 실패";

            /// <summary>타이머 시작 메시지</summary>
            public const string TimerStartMessage = "타이머 시작 (1초 간격, 분 단위 1회 트리거)";

            /// <summary>타이머 중지 메시지</summary>
            public const string TimerStopMessage = "타이머 중지";
        }

        public static class UserAgent
        {
            /// <summary>HTTP 요청용 User-Agent</summary>
            public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

            /// <summary>Naver Finance Referer</summary>
            public const string NaverFinanceReferer = "https://finance.naver.com/";

            /// <summary>Accept-Language 헤더</summary>
            public const string AcceptLanguage = "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7";
        }

        public static class Supabase
        {
            /// <summary>Supabase URL (환경변수로 설정 권장)</summary>
            public static string Url =>
                System.Environment.GetEnvironmentVariable("SUPABASE_URL")
                ?? "https://hlskzjtcivwyixxaynpl.supabase.co";

            /// <summary>Supabase Anon Key (환경변수로 설정 권장)</summary>
            public static string AnonKey =>
                System.Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY")
                ?? "sb_publishable_vVNmybp22sZAxPkghJvoZQ__h3GgxKv";
        }

        public static class Email
        {
            /// <summary>수신 이메일 주소 (Gmail 계정)</summary>
            public const string RecipientEmail = "heyoonow@gmail.com";

            /// <summary>이메일 발신자명</summary>
            public const string SenderName = "HeyNowBot Daily Report";

            /// <summary>발신 Gmail 계정</summary>
            public const string SenderEmail = "heyoonow@gmail.com";

            /// <summary>Gmail 앱 비밀번호 (여기에 입력)</summary>
            public const string GmailAppPassword = "yalq mmci wuau escl";

            /// <summary>SMTP 서버</summary>
            public const string SmtpServer = "smtp.gmail.com";

            /// <summary>SMTP 포트</summary>
            public const int SmtpPort = 587;

            /// <summary>일일 보고서 발송 시간</summary>
            public const int DailyReportHour = 8;

            /// <summary>일일 보고서 발송 분</summary>
            public const int DailyReportMinute = 0;

            /// <summary>이메일 제목 형식</summary>
            public const string SubjectFormat = "[HeyNowBot] 일일 보고서 - {0}";

            /// <summary>이메일 활성화</summary>
            public const bool EnableEmailNotification = true;
        }
    }
}
