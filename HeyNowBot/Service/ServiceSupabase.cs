using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeyNowBot.Model;
using Supabase;

namespace HeyNowBot.Service
{
    public interface IServiceSupabase
    {
        Task<List<VisitLogModel>?> GetVisitListAsync();
        Task UpdateVisitLog();
    }
    public class ServiceSupabase:IServiceSupabase
    {
        private const string _url = "https://hlskzjtcivwyixxaynpl.supabase.co";
        private const string _anonKey = "sb_publishable_vVNmybp22sZAxPkghJvoZQ__h3GgxKv";
        private bool _isInitialized = false;
        private Client _supabaseClient;


        public ServiceSupabase()
        {
            Loaded();
        }

        private async void Loaded()
        {
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true,
            };
            _supabaseClient = new Supabase.Client(_url, _anonKey, options);
            await _supabaseClient.InitializeAsync();
            _isInitialized = true;
            Console.WriteLine("[ServiceSupabase] Supabase 클라이언트 초기화 완료");
        }

        public async Task<List<VisitLogModel>?> GetVisitListAsync()
        {
            if (!_isInitialized)
            {
                await Task.Delay(300); // 대기 후 재시도
                await GetVisitListAsync();
                return null;
            }
            Console.WriteLine("[ServiceSupabase] GetTest 메서드 호출됨");
            var dt = DateTime.Now.AddDays(-1);
            // 여기에 Supabase와 상호작용하는 코드 작성
            var result = await _supabaseClient.From<VisitLogModel>()
                //.Order("created_at", ordering: Supabase.Postgrest.Constants.Ordering.Descending)
                .Where(x=>x.CreatedAt >= dt)
                .Get();
            
            var list = result.Models.ToList();
            return list;
            
        }
        public async Task UpdateVisitLog()
        {
            if (!_isInitialized)
            {
                await Task.Delay(300); // 대기 후 재시도
                await UpdateVisitLog();
                return;
            }
            
            var dt = DateTime.Today.AddDays(-3).Date;
            // 여기에 Supabase와 상호작용하는 코드 작성
            var result = await _supabaseClient.From<VisitLogModel>()
                .Where(x => x.Count == -1 && x.CreatedAt >= dt)
                .Get();
            var list = result.Models.ToList();
            if(list.Count == 0)
            {
                Console.WriteLine($"[ServiceSupabase:{DateTime.Now.ToString()}] 업데이트할 방문자 로그가 없습니다.");
                return;
            }
            foreach (var item in list)
            {
                var r = await _supabaseClient.From<VisitLogModel>()
                .Where(x => x.UserId == item.UserId && x.CreatedAt < item.CreatedAt)
                .Get();

                await _supabaseClient
                    .From<VisitLogModel>()
                    .Where(x => x.Id == item.Id)
                    .Set(x=>x.Count, r.Models.Count)
                    .Update();
                await Task.Delay(100); // 너무 빠르게 요청 보내는 것을 방지
            }
            return;
        }
    }
}
