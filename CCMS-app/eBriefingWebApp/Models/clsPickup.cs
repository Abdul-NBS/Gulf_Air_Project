using System;

namespace eBriefingWebApp.Models
{
    public class clsPickup
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Zone { get; set; }
        public string FlightDetails { get; set; }
        public DateTime FlightDateTime { get; set; }
        public int Quantity { get; set; }
        public string Position { get; set; }
        public string Name { get; set; }
        public string StaffNumber { get; set; }
        public int BusId { get; set; }
        public int GroupId { get; set; }
    }
}