using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class FamFormIndexInfo
    {
        public FamFormIndexInfo() { }
        public String fltNo { get; set; }
        public int posId { get; set; }
        public int PosName { get; set; }
        public int BlockId { get; set; }
        public int StaffNo { get; set; }
        public int StaffName { get; set; }

        public int Flightid { get; set; }
    }
}