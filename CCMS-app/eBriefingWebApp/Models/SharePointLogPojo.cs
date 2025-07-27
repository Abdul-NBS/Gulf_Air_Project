
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;

namespace eBriefingWebApp.Models
{
    public class SharePointLogPojo
    {
        public DateTime fromDate { get; set; }

        public DateTime toDate { get; set; }

        public string operation {  get; set; }

        public string fileName {  get; set; }
        public List<SharePointLog> sharePOintLoglist { get; set; }
    }
}