using System.Collections.Generic;

namespace eBriefingWebApp.Helper
{
    public class CrewCategory
    {
        public int order { get; set; }
        public string Cat { get; set; }

        public static List<CrewCategory> getCategoryList()
        {
            List<CrewCategory> list = new List<CrewCategory>();

            list.Add(new CrewCategory
            {
                Cat = "CM",
                order = 1
            });

            list.Add(new CrewCategory
            {
                Cat = "CS",
                order = 2
            });

            list.Add(new CrewCategory
            {
                Cat = "FG",
                order = 3
            });

            list.Add(new CrewCategory
            {
                Cat = "FA",
                order = 4
            });

            list.Add(new CrewCategory
            {
                Cat = "CF",
                order = 5
            });

            return list;
        }
    }


}