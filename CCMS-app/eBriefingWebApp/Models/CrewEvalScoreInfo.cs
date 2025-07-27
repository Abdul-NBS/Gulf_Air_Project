using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewEvalScoreInfo
    {
        

        public string crewId { get; set; }
        public int sectionId { get; set; }
        public int answerId { get; set; }
        public int questionId { get; set; }
        public string Section { get; set; }

        public string Question { get; set; }
        public string Answer { get; set; }

        public string comments { get; set; }

        public int OrderNumber {  get; set; }

        public int? qnAnswer {  get; set; }

        public int? qnAnswerId { get; set; }
        public int? scorePoint { get; internal set; }
        public string scoreDesc { get; internal set; }
    }
}