using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class FFFormInfo
    {
        public  int famId { get;  set; }
        public DateTime? fltDate { get; set; }

        public int Flightid { get; set; }
        public String FlightNo { get; set; }
        
        public String StaffNo { get; set; }

        public String StaffName { get; set; }

        public int BlockId { get; set; }

    }
}