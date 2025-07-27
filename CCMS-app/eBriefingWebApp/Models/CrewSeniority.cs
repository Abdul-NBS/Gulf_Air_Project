using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewSeniority
    {
        public int Id { get; set; }
        public String CrewId {  get; set; }
        public int? group {  get; set; }

        public int? groupSeniority {  get; set; }

        public String crewName {get; set; }

        public String crewPosition {  get; set; }
        
        public List<CrewPeriodSeniority> periodseniority { get; set; }

        public String fleet {  get; set; }

        public int? NetSeniority { get; set; }
    }
}