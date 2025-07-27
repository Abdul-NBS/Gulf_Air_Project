using System;

namespace eBriefingWebApp.Models
{
    public class AppraisalPojo
    {
       

        public int Id { get; set; }
        public DateTime? submitteddate { get; set; }
        public String BlockNo { get; set; }
        public Decimal TotalScore {  get; set; }
        public string appraiserId { get; set; }
        public string appraiserName { get; set; }
        public string appraiseeStaffNo { get;  set; }
        public string appraiseeStaffName { get;  set; }
    }
}