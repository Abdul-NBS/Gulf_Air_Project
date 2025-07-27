using System.Collections.Generic;

namespace eBriefingWebApp.Models
{
    public class ClsAnswer
    {
        public string Text { get; set; }
        public bool Value { get; set; }

        public static List<ClsAnswer> GetAnswers()
        {
            List<ClsAnswer> _status = new List<ClsAnswer>();

            _status.Add(new ClsAnswer
            {
                Text = "Correct",
                Value = true
            });

            _status.Add(new ClsAnswer
            {
                Text = "Wrong",
                Value = false
            });

            return _status;
        }
    }
}