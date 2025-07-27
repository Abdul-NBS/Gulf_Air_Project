using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class AppraisalInfo
    {
        public CrewAppraisal crewAppraisal { get; set; }
        /*public int crewId { get; set; }
        public String CrewName { get; set; }
        public String evaluatorId { get; set; }
        public int Evaluatorname { get; set; }

        public int FltNo { get; set; }

        public String fltOrg { get; set; }
        public String fltDest { get; set; }

        public DateTime fltDate { get; set; }*/

        

        public List<CrewEvalScores> CrewEvalScorelist { get; set; }

    }
}