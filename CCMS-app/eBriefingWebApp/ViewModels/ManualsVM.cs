using System;

namespace eBriefingWebApp.ViewModels
{
    public class ManualsVM
    {
        public string StaffNumber { get; set; }
        public string FullName { get; set; }
        public string Title { get; set; }
        public DateTime VersionDate { get; set; }
        public DateTime VersionDueDate { get; set; }
        public DateTime AcknowledgeDate { get; set; }
    }
}