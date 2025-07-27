using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class RecencyRptInfo
    {
        public String StaffNmbr { get; set; }

        public String StaffName { get; set; }   

        public DateTime lastFlightDoorDuty { get; set; }

        public String LastPos { get; set; }
    }
}