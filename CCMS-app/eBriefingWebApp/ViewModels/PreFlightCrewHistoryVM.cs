using System;

namespace eBriefingWebApp.ViewModels
{
    public class PreFlightCrewHistoryVM
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; }
        public DateTime FlightDate { get; set; }
        public string Orig { get; set; }
        public string Dest { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
    }
}