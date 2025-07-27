using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewAppraisalDataPojo
    {
        public int Id { get; set; }
        public String appraiserStaffNo {  get; set; }
        public String appraiserName { get; set; }
        public String crewStaffName { get; set; }
        public String crewStaffNo { get; set; }
        public String BlockName { get; set; }
        public decimal TotalScore { get; set; }
        public DateTime? submittedDate { get; set; }
    }
}