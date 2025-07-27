namespace eBriefingWebApp.Models
{
    public class CrewEvalScores
    {
        public string crewId { get; set;}
        public int sectionId { get; set; }
        public int answerId { get; set; }
        public int questionId { get; set; }
        public string Section { get; set; }

        public string Question { get; set; }
        public string Answer { get; set; }

        public string commnets { get; set; }

    }
}