using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeyNowBot.Model;
using Supabase;

namespace HeyNowBot.Service
{
    /// <summary>
    /// Supabase 데이터베이스 접근 서비스
    /// </summary>
    public interface ISupabaseService
    {
        Task<List<VisitLogModel>?> GetVisitListAsync();
        Task UpdateVisitLogAsync();
    }

    public class SupabaseService : ISupabaseService
    {
        private bool _isInitialized = false;
        private Client _supabaseClient;

        public SupabaseService()
        {
            InitializeAsync().ConfigureAwait(false);
        }

        private async Task InitializeAsync()
        {
            try
            {
                var options = new SupabaseOptions
                {
                    AutoConnectRealtime = true,
                };
                _supabaseClient = new Client(Constants.Supabase.Url, Constants.Supabase.AnonKey, options);
                await _supabaseClient.InitializeAsync();
                _isInitialized = true;
                Log("Supabase 초기화 완료");
            }
            catch (Exception ex)
            {
                Log($"Supabase 초기화 오류: {ex.Message}");
            }
        }

        public async Task<List<VisitLogModel>?> GetVisitListAsync()
        {
            if (!await EnsureInitializedAsync())
                return null;

            try
            {
                var dt = DateTime.Now.AddDays(-1);
                var result = await _supabaseClient.From<VisitLogModel>()
                    .Where(x => x.CreatedAt >= dt)
                    .Get();

                return result.Models.ToList();
            }
            catch (Exception ex)
            {
                Log($"방문 기록 조회 오류: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateVisitLogAsync()
        {
            if (!await EnsureInitializedAsync())
                return;

            try
            {
                var dt = DateTime.Today.AddDays(-3).Date;
                var result = await _supabaseClient.From<VisitLogModel>()
                    .Where(x => x.Count == -1 && x.CreatedAt >= dt)
                    .Get();

                var list = result.Models.ToList();
                if (list.Count == 0)
                    return;

                foreach (var item in list)
                {
                    var r = await _supabaseClient.From<VisitLogModel>()
                        .Where(x => x.UserId == item.UserId && x.CreatedAt < item.CreatedAt)
                        .Get();

                    await _supabaseClient
                        .From<VisitLogModel>()
                        .Where(x => x.Id == item.Id)
                        .Set(x => x.Count, r.Models.Count)
                        .Update();

                    await Task.Delay(Constants.Schedule.DatabaseRequestDelayMs);
                }
            }
            catch (Exception ex)
            {
                Log($"방문 기록 업데이트 오류: {ex.Message}");
            }
        }

        private async Task<bool> EnsureInitializedAsync()
        {
            if (_isInitialized)
                return true;

            await Task.Delay(Constants.Schedule.SupabaseInitWaitMs);
            return _isInitialized;
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [SupabaseService] {message}");
        }
    }

    // 레거시 호환성 유지를 위한 별칭
    [Obsolete("SupabaseService를 사용하세요")]
    public class ServiceSupabase : SupabaseService { }
}
