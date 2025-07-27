using System;
using System.Collections.Generic;

namespace eBriefingWebApp.ViewModels
{
    public class PreFlightTestVM
    {
        public int CrewTestId { get; set; }
        public int CrewPosId { get; set; }
        public int FlightId { get; set; }
        public string FlightNumber { get; set; }
        public DateTime FlightDate { get; set; }
        public string OrigDest { get; set; }
        public string EQP { get; set; }
        public bool isSecondChance { get; set; }
        public int TryCount { get; set; }
        public List<Questions> Questions { get; set; }

        public PreFlightTestVM()
        {
            Questions = new List<Questions>();
        }
    }

    public class Questions
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int SelectedAnswer { get; set; }
        public string Question { get; set; }
        public List<Answers> Answers { get; set; }

        public Questions()
        {
            Answers = new List<Answers>();
        }
    }

    public class Answers
    {
        public int Id { get; set; }
        public int QuestionID { get; set; }
        public string AnswerText { get; set; }
        public bool Answer { get; set; }
    }


}