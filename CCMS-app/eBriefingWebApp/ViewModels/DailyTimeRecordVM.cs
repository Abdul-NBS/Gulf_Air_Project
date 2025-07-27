using System;

namespace eBriefingWebApp.ViewModels
{
    public class DailyTimeRecordVM
    {
        public int Id { get; set; }
        public int BlockId { get; set; }
        public int FlightId { get; set; }
        public string Duty { get; set; }
        public string FlightNumber { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime FlightDate { get; set; }
        public string Route { get; set; }
        public string Room { get; set; }
        public string Status { get; set; }
    }




}