using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewAppraisalData
    {
        public String StaffNo {  get; set; }
        public String StaffName{  get; set; }

       

        public int totalFlight { get; set; }

        public List<AppraisalPojo> pojoAppraisals { get; set; }

    }
}