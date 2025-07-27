using System.Collections.Generic;

namespace eBriefingWebApp.Models
{
    public class CategoryComparer : IComparer<string>
    {
        private readonly List<string> _order = new List<string> { "CM", "CMD", "CS", "CSD", "FG", "FGD", "FA", "FAD" };

        public int Compare(string x, string y)
        {
            int xIndex = _order.IndexOf(x);
            int yIndex = _order.IndexOf(y);

            if (xIndex == -1) xIndex = _order.Count;
            if (yIndex == -1) yIndex = _order.Count;

            return xIndex.CompareTo(yIndex);
        }
    }
}