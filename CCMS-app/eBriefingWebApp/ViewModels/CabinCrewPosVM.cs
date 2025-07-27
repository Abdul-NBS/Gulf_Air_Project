using System;
using System.Collections.Generic;

namespace eBriefingWebApp.ViewModels
{
    public class CabinCrewPosVM
    {
        public int Id { get; set; }
        public string Block_No { get; set; }
        public DateTime Block_Date { get; set; }
        public DateTime End_Date { get; set; }
        public int Days_No { get; set; }
        public string Eqp { get; set; }
        public DateTime Start_Time { get; set; }
        public List<OCB_Flights> Flights { get; set; }
        public List<OCB_CrewPos> CrewPos { get; set; }
    }
}