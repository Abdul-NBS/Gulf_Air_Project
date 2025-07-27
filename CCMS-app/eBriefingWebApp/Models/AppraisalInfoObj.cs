
using Microsoft.Graph.Models;
using System.Collections.Generic;

namespace eBriefingWebApp.Models
{
    public class AppraisalInfoObj
    {
        public CrewAppraisal crewAppraisal { get; set; }

        public List<CrewEvalScoreInfo> CrewEvalScorelist { get; set; }
    }
}