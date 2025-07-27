using System.Collections.Generic;

namespace eBriefingWebApp.Models
{
    public class ClsTestQuestion
    {
        public int QuestionId { get; set; }
        public List<ClsTestAnswers> TestAnswers { get; set; }
    }

    public class ClsTestAnswers
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public int FlightId { get; set; }
        public string StaffNumber { get; set; }
    }
}