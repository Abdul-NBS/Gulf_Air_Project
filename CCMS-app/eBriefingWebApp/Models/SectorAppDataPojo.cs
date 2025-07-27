using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
    public class SectorAppDataPojo
    {
        public int? orginSectorId {  get; set; }
        public int? dstnSectorId { get; set; }
        public int totalOAB {get; set; }
        public int totalFlight { get; set; }
        public List<CrewAppraisalDataPojo> crewAppraisalDatas { get; set; }
        public double? percentOverall { get; set; }

        public int TLIdCd { get; set; }
    }
}