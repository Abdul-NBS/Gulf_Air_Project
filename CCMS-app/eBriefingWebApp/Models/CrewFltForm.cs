using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewFltForm
    {
        public CrewFlightForm crewFlt {  get; set; }
        public List<CrewFltSectionObj> sections { get; set; }
    }
}