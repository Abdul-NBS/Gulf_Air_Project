using System.Collections.Generic;

namespace eBriefingWebApp.ViewModels
{
    public class TimeVM
    {
        public static List<string> GetHr()
        {
            List<string> hr = new List<string>();

            hr.Add("00");
            hr.Add("01");
            hr.Add("02");
            hr.Add("03");
            hr.Add("04");

            return hr;
        }

        public static List<string> GetMin()
        {
            List<string> Min = new List<string>();

            for (int i = 0; i < 60; i++)
            {
                if (i < 10)
                {
                    Min.Add("0" + i.ToString());
                }
                else
                {
                    Min.Add(i.ToString());
                }
            }

            return Min;
        }
    }
}