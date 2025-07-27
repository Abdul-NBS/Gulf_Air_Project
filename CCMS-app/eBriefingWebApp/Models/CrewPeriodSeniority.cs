using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewPeriodSeniority
    {
        public int? periodMonth {  get; set; }
        public int? periodYear { get; set; }
        public String Period {  get; set; }
        public int? periodSeniority {  get; set; }

        public int? id { get; set; }
        public int? CSEId { get; set; }
    }
}