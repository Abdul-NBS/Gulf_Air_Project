using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewGroomingPojo
    {


        public string StaffNo { get; set; }
        public string staffName { get; set; }
        public string updatedUser { get; set; }
        public string updatedusername { get; set; }
        public int eyecolor { get; set; }
        public int haircolor { get; set; }
        public decimal height { get; set; }
        public decimal weight { get; set; }

        public string weightStat { get; set; }
        public string remarks { get; set; }
        public DateTime? created { get; set; }
        public List<CrewWeightHistoryPojo> historylist { get; set; }
        public string gender { get; set; }
        public string nation { get; set; }
        public string category { get; set; }
        public double minweight { get; set; }
        public double maxweight { get; set; }
        public string eyecolorStr { get; set; }
        public string haircolorStr { get; set; }
        public string imagepath { get; set; }

        public string imageRemarks { get; set; }

        public List<CrewGroomingRemark> latestRemarksList { get; set; }
    }
}