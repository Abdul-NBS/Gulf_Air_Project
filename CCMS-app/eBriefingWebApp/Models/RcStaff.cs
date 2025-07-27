using System;

namespace eBriefingWebApp.Controllers
{
    public class RcStaff
    {
        public long? sEQNUM { get; set; }
        public string posCode { get; set; }
        public String family { get; set; }
        public string fullName { get; set; }
        public DateTime? block_Date { get; set; }

        public DateTime? expiry_Date { get; set; }

        public RcStaff(long? sEQNUM, string posCode, string family, string fullName, DateTime? block_Date, DateTime? expiry_Date)
        {
            this.sEQNUM = sEQNUM;
            this.posCode = posCode;
            this.block_Date = block_Date;
            this.family= family;
            this.expiry_Date = expiry_Date;
            this.fullName = fullName;
        }
    }
}