using System;

namespace HeyNowBot.Domain.Entities
{
    public class StockInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string CurrentPrice { get; set; }
        public string PreviousDayChange { get; set; }
        public string ChangeRate { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercent { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
