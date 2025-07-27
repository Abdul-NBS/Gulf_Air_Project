using System;
using System.Collections.Generic;

namespace eBriefingWebApp.ViewModels
{
    public class CrewDashVM
    {
        public string PasswordAlert { get; set; }
        public string USBAlert { get; set; }
        public bool UpdatedUsb { get; set; }
        public string SecondChanceAlert { get; set; }
        public List<TodayBlock> TodayBlocksList { get; set; }
        public List<CrewPreFlight> CrewPreFlightList { get; set; }
        public List<CrewPreFlightSecondChance> CrewPreFlightSecondChanceList { get; set; }
        public List<BriefingRoom> BriefingRoomList { get; set; }

        public CrewDashVM()
        {
            TodayBlocksList = new List<TodayBlock>();
            CrewPreFlightList = new List<CrewPreFlight>();
            BriefingRoomList = new List<BriefingRoom>();
            CrewPreFlightSecondChanceList = new List<CrewPreFlightSecondChance>();
        }
    }

    public class TodayBlock
    {
        public int BlockId { get; set; }
        public string BlockNumber { get; set; }
        public DateTime BlockDate { get; set; }
        public DateTime StartTime { get; set; }
        public int Length { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CrewPreFlight
    {
        public int TestId { get; set; }
        public int CrewPosId { get; set; }
        public int BlockId { get; set; }
        public int FlightId { get; set; }
        public string BlockNumber { get; set; }
        public DateTime BlockDate { get; set; }
        public string FlightNumber { get; set; }
        public string Orig { get; set; }
        public string Dest { get; set; }
        public DateTime StartTime { get; set; }
        public string Status { get; set; }
        public bool AllowSecondChance { get; set; }
        public bool DidSecondChance { get; set; }
        public bool IsFaildTest { get; set; }
    }

    public class CrewPreFlightSecondChance
    {
        public int TestId { get; set; }
        public int CrewPosId { get; set; }
        public int BlockId { get; set; }
        public int FlightId { get; set; }
        public string BlockNumber { get; set; }
        public DateTime BlockDate { get; set; }
        public string FlightNumber { get; set; }
        public string Orig { get; set; }
        public string Dest { get; set; }
        public DateTime StartTime { get; set; }
        public string Status { get; set; }
    }

    public class BriefingRoom
    {
        public string FlightNumber { get; set; }
        public DateTime CheckinTime { get; set; }
        public string Orig { get; set; }
        public string Dest { get; set; }
        public string Room { get; set; }
    }
}