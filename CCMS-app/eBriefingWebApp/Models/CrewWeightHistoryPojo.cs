using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewWeightHistoryPojo
    {
        public int Id { get; set; }
        public string StaffNumber { get; set; }
   
       
        public Nullable<decimal> Height { get; set; }
        
        public Nullable<int> Weight_Status_Id { get; set; }
        public Nullable<decimal> Weight { get; set; }
        public Nullable<System.DateTime> Weight_Entry_Date { get; set; }
        public string Remarks { get; set; }
       
        public Decimal lossorgain { get; set; }
       
        public string Updated_By { get; set; }

        public string updatedByName { get; set; }

        public string weightstat { get; set; }



    }
}