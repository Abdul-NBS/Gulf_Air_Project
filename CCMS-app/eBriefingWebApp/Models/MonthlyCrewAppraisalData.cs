using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class MonthlyCrewAppraisalData
    {
        public String StaffNo {  get; set; }
        public String StaffName{  get; set; }

        public double monthlyScoreAverage {  get; set; }

        public int totalFlight { get; set; }


    }
}