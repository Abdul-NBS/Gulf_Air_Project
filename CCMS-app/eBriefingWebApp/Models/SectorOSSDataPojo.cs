using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class SectorOSSDataPojo
    {
        public int? orginSectorId {  get; set; }
        public int? dstnSectorId { get; set; }
      
        public List<CrewFlightForm> crFlightForm { get; set; }
       
    }
}