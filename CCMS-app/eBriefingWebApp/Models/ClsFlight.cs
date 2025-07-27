using System;

namespace eBriefingWebApp.Models
{
    public class ClsFlight
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; }
        public DateTime FlightDate { get; set; }
        public string Orig { get; set; }
        public string Dest { get; set; }
        public string EQP { get; set; }
    }
}