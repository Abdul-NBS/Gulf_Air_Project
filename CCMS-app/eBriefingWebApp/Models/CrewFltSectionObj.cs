using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class CrewFltSectionObj
    {
       public int SectionId { get; set; }
        public String SectionName { get; set; }
         
        public List<SectionQuestionAnswer> qnalist {  get; set; }

        public String secComment { get; set; }
    }
}