using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace HeyNowBot.Model
{

    [Table("visit_log")]
    public class VisitLogModel : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]  // bigserial → false(자동증가)
        public long Id { get; set; }

        [Column("app_name")]
        public string AppName { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("os_type")]
        public string OsType { get; set; }

        [Column("version")]
        public string Version { get; set; }

        [Column("event_type")]
        public string EventType { get; set; }

        [Column("referrer")]
        public string Referrer { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("update_at")]
        public DateTime? UpdateAt { get; set; }

        [Column("delete_at")]
        public DateTime? DeleteAt { get; set; }

        [Column("contry")]
        public string Country { get; set; }   // 주의: DB에 conTry 로 되어 있음 (오타 포함 그대로 해야 함)

        [Column("count")]
        public int Count{ get; set; }
    }
}
