using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewDataPojo
    {
        public String StaffNumber {  get; set; }

        public CrewGroomingAttribute Grooming { get; set; }
        public CrewWeightDetailList WeightDetailList { get; set; }

        public List<Crew_WarningLetter> WarningsList { get; set; }

        public List<Crew_Commendation> crew_Commendations { get; set; }
        public List<CrewAppraisalDataPojo> crewAppraisalDataPojos { get; set; }

        public String FullName { get; set; }
        public String Designation {  get; set; }

        public int preFlightTotalPass {get; set; }

        public int preFlightTotalFail {get;set; }

        public List<Crew_Appreciation> crewAppreciation { get; set; }
        public List<PreFlight_CrewTest> preFlight_CrewTest { get; set; }
        public string imagepath { get;  set; }
        public string gender { get;  set; }
        public string nation { get;  set; }
    }
}