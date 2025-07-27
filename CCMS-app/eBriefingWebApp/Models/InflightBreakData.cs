using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.EnterpriseServices.Internal;
using System.Linq;
using System.Web;
using System.Windows.Media.Converters;

namespace eBriefingWebApp.Models
{
    public class InflightBreakData
    {
        public DateTime From_Block_Date { get; set; }
        public DateTime To_Block_Date { get; set; }
        public int? orginSectorId { get; set; }
        public int? dstnSectorId { get; set; }
        public int? flightNo { get; set; }

        // This will hold the results from the database
        // We'll project into a custom DTO/ViewModel that includes StaffName
        public List<InflightRestSheetDisplayModel> InflightRestSheetsDisplay { get; set; }
    }

    public class InflightRestSheetDisplayModel
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; }
        public DateTime FlightDate { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public int StaffNumber { get; set; }
        public string StaffName { get; set; } // This will come from CrewDetails
        public DateTime SubmissionDate { get; set; }
        // Add other properties from InflightRestSheet you want to display
        public string RestingStaff { get; set; }
        public string TakingOverStaff { get; set; }
        public string Location { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TotalTime { get; set; }
        public string RestingStaffNumber { get; set; }
        public string RestingStaffFullName { get; set; }
        public string RestingStaffCategory { get; set; }
        public string RestingStaffDisplay { get; set; } // Combined string for display and hover
        public int? RestingCrewPosId { get; set; } // Added for Resting Staff's OCB_CREWPOS Id

        public String RestingCrewPos { get; set; }
        // New properties for Taking Over Staff
        public string TakingOverStaffNumber { get; set; }
        public string TakingOverStaffFullName { get; set; }
        public string TakingOverStaffCategory { get; set; }
        public string TakingOverStaffDisplay { get; set; } // Combined string for display and hover
        public int? TakingOverCrewPosId { get; set; }
        public String TakingOverCrewPos { get; set; }

    }
}