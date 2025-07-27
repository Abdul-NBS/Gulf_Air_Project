using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class MonthlyAppraisalData
    {
        public int totalFlight {  get; set; }

        public int year { get; set; }
        public int month { get; set; }
        public double? overallAverage{  get; set; }

        public double? monthlyAverage {  get; set; }

        public List<MonthlyCrewAppraisalData> monthlyCrewAppraisalDatas { get; set; }

        public List<MonthlyLeadAppraisalData> monthlyLeadAppraisalDatas { get; set; }
        public int OABCount { get;  set; }
    }
}