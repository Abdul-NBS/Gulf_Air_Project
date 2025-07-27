using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewAppDataPojo
    {
  
        public int totalOAB {get; set; }
        public int totalFlight { get; set; }
        public List<CrewAppraisalDataPojo> crewAppraisalDatas { get; set; }
        public double? percentOverall { get; set; }

        public string  crewId { get; set; }
    }
}