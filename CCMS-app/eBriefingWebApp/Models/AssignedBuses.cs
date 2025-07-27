using System;
using System.Collections.Generic;

namespace eBriefingWebApp.Models
{
    public class AssignedBuses
    {
        public int Bus_Id { get; set; }
        public int GroupId { get; set; }
        public DateTime Date { get; set; }
        public string Group_Name { get; set; }
        public List<PickupDetail> Details { get; set; }
    }
}