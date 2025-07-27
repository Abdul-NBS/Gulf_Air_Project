using System.Collections.Generic;

namespace eBriefingWebApp.Models
{
    public class ClsStatus
    {
        public string Text { get; set; }
        public string Value { get; set; }

        public static List<ClsStatus> ListGetStatus()
        {
            List<ClsStatus> _status = new List<ClsStatus>();

            _status.Add(new ClsStatus
            {
                Text = "E",
                Value = "E"
            });

            _status.Add(new ClsStatus
            {
                Text = "L",
                Value = "L"
            });

            _status.Add(new ClsStatus
            {
                Text = "J",
                Value = "J"
            });

            _status.Add(new ClsStatus
            {
                Text = "Exclude",
                Value = "Exclude"
            });

            _status.Add(new ClsStatus
            {
                Text = "Pin",
                Value = "Pin"
            });

            return _status;
        }
    }

}