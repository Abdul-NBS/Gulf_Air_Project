using System;
using System.Collections.Generic;

namespace eBriefingWebApp.ViewModels
{
    public class OCBCrewPosVM
    {
        public int BlockId { get; set; }
        public string BlockNumber { get; set; }
        public DateTime StartOn { get; set; }
        public DateTime EndAt { get; set; }
        public string Lenght { get; set; }
        public string Aircraft { get; set; }
        public string Duty { get; set; }
        public string PickUpTime { get; set; }
        public string ReportTime { get; set; }
        public string SchedTimeDep { get; set; }
        public List<OCB_Flights> Flights { get; set; }
        public List<CrewPos> CrewPos { get; set; }
    }

    public class CrewPos
    {
        public int Id { get; set; }
        public int CrewPosId { get; set; }
        public string Category { get; set; }
        public string FullName { get; set; }
        public string Lang { get; set; }
        public string Gender { get; set; }
        public string Nat { get; set; }
        public string PosCode { get; set; }
        public OCB_AC_Pos AcPos { get; set; }
        public bool IsDeleted { get; set; }
        public OCB_Comments OCB_Comment { get; set; }
        public List<Pos> Last4Pos { get; set; }
    }

    public class Pos
    {
        public string PosCode { get; set; }
        public string EQP { get; set; }
        public int Days_No { get; set; }
    }

}