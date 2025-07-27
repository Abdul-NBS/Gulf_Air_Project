using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using eBriefingWebApp.Helper;
using eBriefingWebApp.Interfaces;
using eBriefingWebApp.Models; // Ensure this points to your InflightBreakData and other models
using eBriefingWebApp.ViewModels; // If InflightBreakData is here
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using System;
using System.Collections.Generic;
using System.Data.Entity; // For Include and ToListAsync
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace eBriefingWebApp.Controllers
{
    public class CrewBreakController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;
        // private readonly BlobServiceClient _blobServiceClient;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public CrewBreakController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
            // _blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=ebriefingaims;AccountKey=n33PmfYsfpNf0GepSGew03MnHy1LENUN/PdCdrb8iMq2yKO609R9eoH7b3TqaRIZu1NZ18XBV4Gi+AStnIWJ9Q==;EndpointSuffix=core.windows.net");
            var region = System.Configuration.ConfigurationManager.AppSettings["AWSRegion"];
            _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));
            _bucketName = System.Configuration.ConfigurationManager.AppSettings["S3BucketName"];
        }

        private readonly CSMEntities db = new CSMEntities(); // Use private readonly for DbContext

        // GET: CrewBreak/CrewBreakData
        // Ensure flightNo is nullable here as well
        [Authorize(Roles = "CSM.Admins")]
        public async Task<System.Web.Mvc.ActionResult> CrewBreakData(DateTime? From_Block_Date, DateTime? To_Block_Date, int? orginSector, int? destSector, int? flightNo)
        {
            // Populate dropdowns for initial load
            ViewBag.OriginAirportCode = await db.AirportCodes
                                .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                .ToListAsync();

            ViewBag.DestinationAirportCode = await db.AirportCodes
                                            .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                            .ToListAsync();

            // Set default dates for the initial load if not provided
            TempData["From_Block_Date"] = From_Block_Date ?? DateTime.Today; // Use DateTime.Today for date only
            TempData["To_Block_Date"] = To_Block_Date ?? DateTime.Today;

            // Initialize the model for the view, setting default values for optional filters
            InflightBreakData inflightBreakData = new InflightBreakData
            {
                From_Block_Date = From_Block_Date ?? DateTime.Today,
                To_Block_Date = To_Block_Date ?? DateTime.Today,
                orginSectorId = orginSector,
                dstnSectorId = destSector,
                flightNo = flightNo
            };

            // If dates are provided, pre-filter the data on initial load
            if (From_Block_Date.HasValue && To_Block_Date.HasValue)
            {
                inflightBreakData.InflightRestSheetsDisplay = await GetFilteredInflightBreakData(
                    From_Block_Date.Value,
                    To_Block_Date.Value,
                    orginSector,
                    destSector,
                    flightNo
                );
            }
             if(From_Block_Date.HasValue == null || To_Block_Date.HasValue == null)
            {
                inflightBreakData.InflightRestSheetsDisplay = await GetFilteredInflightBreakData(
                   DateTime.UtcNow,
                    DateTime.UtcNow,
                    orginSector,
                    destSector,
                    flightNo
                );
            }
            return View(inflightBreakData);
        }


        // POST: CrewBreak/SectorSearchPost
        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins")]
        public async Task<ActionResult> SectorSearchPost(DateTime From_Block_Date, DateTime To_Block_Date, int? orginSectorId, int? dstnSectorId, int? flightNo)
        {
            // Populate dropdowns for the view in case of errors or re-display
            ViewBag.OriginAirportCode = await db.AirportCodes
                                .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                .ToListAsync();

            ViewBag.DestinationAirportCode = await db.AirportCodes
                                            .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                            .ToListAsync();

            // Set TempData for the dates so they persist after postback
            TempData["From_Block_Date"] = From_Block_Date;
            TempData["To_Block_Date"] = To_Block_Date;

            // Create the model to pass back to the view, preserving filter selections
            InflightBreakData inflightBreakData = new InflightBreakData
            {
                From_Block_Date = From_Block_Date,
                To_Block_Date = To_Block_Date,
                orginSectorId = orginSectorId,
                dstnSectorId = dstnSectorId,
                flightNo = flightNo
            };

            // Only From and To dates are mandatory (server-side validation)
            if (From_Block_Date == default(DateTime) || To_Block_Date == default(DateTime))
            {
                TempData["Status"] = "Error";
                TempData["Message"] = "From Date and To Date are mandatory fields.";
                ModelState.AddModelError("", "From Date and To Date are mandatory fields.");
                return View("CrewBreakData", inflightBreakData); // Return to view with error
            }

            try
            {
                // Retrieve filtered data using the helper method
                inflightBreakData.InflightRestSheetsDisplay = await GetFilteredInflightBreakData(From_Block_Date, To_Block_Date, orginSectorId, dstnSectorId, flightNo);

                if (inflightBreakData.InflightRestSheetsDisplay == null || !inflightBreakData.InflightRestSheetsDisplay.Any())
                {
                    TempData["Status"] = "Info";
                    TempData["Message"] = "No data found for the selected criteria.";
                }
                else
                {
                    TempData["Status"] = "Success";
                    TempData["Message"] = "Data loaded successfully.";
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                var error = ex.StackTrace;
                Console.WriteLine($"Error in SectorSearchPost: {ex.Message}");
                TempData["Status"] = "Error";
                TempData["Message"] = "An error occurred while retrieving data. Please try again.";
            }

            return View("CrewBreakData", inflightBreakData); // Return to view with results or error
        }

        // Helper method to get filtered data for InflightRestSheets, including staff names
        // Helper method to get filtered data for InflightRestSheets, including staff names
        // Helper method to get filtered data for InflightRestSheets, including staff names
        private async Task<List<InflightRestSheetDisplayModel>> GetFilteredInflightBreakData(
            DateTime fromDate, DateTime toDate, int? originSectorId, int? destSectorId, int? flightNo)
        {
            try
            {
                // Calculate the end date for the query *before* it gets translated to SQL
                DateTime effectiveToDate = toDate.AddDays(1);

                var query = from irs in db.InflightRestSheets
                            join ocb in db.OCB_Flights on irs.FlightId equals ocb.Id

                            // Join for Resting Staff details via OCB_CREWPOS and CrewDetails (Left Joins)
                            join restingCrewPos in db.OCB_CrewPos
                                on new { BlockId = ocb.BlockId, StaffNumber = irs.RestingStaff }
                                equals new { BlockId = restingCrewPos.BlockId, StaffNumber = restingCrewPos.StaffNumber } into rcPosGroup
                            from rcPos in rcPosGroup.DefaultIfEmpty()

                            join restingCrewDetails in db.CrewDetails
                                on rcPos.StaffNumber equals restingCrewDetails.StaffNumber into rcDetGroup
                            from rcDet in rcDetGroup.DefaultIfEmpty()

                                // Join for Taking Over Staff details via OCB_CREWPOS and CrewDetails (Left Joins)
                            join takingOverCrewPos in db.OCB_CrewPos
                                on new { BlockId = ocb.BlockId, StaffNumber = irs.TakingOverStaff }
                                equals new { BlockId = takingOverCrewPos.BlockId, StaffNumber = takingOverCrewPos.StaffNumber } into tocPosGroup
                            from tocPos in tocPosGroup.DefaultIfEmpty()

                            join takingOverCrewDetails in db.CrewDetails
                                on tocPos.StaffNumber equals takingOverCrewDetails.StaffNumber into tocDetGroup
                            from tocDet in tocDetGroup.DefaultIfEmpty()

                                // CORRECTED JOIN for Resting Crew Position Code
                                // Assuming OCB_CrewPos has a 'Pos_Id' that links to OCB_AC_Pos.Id
                            join restingPosCode in db.OCB_AC_Pos
                                on rcPos.PosId equals restingPosCode.Id into rpcGroup
                            from rpc in rpcGroup.DefaultIfEmpty()

                                // CORRECTED JOIN for Taking Over Crew Position Code
                                // Assuming OCB_CrewPos has a 'Pos_Id' that links to OCB_AC_Pos.Id
                            join takingOverPosCode in db.OCB_AC_Pos
                                on tocPos.PosId equals takingOverPosCode.Id into topcGroup
                            from topc in topcGroup.DefaultIfEmpty()

                            select new // First projection: Select raw data into an anonymous type
                            {
                                irs.Id,
                                FlightNumber = ocb.Flt_No,
                                FlightDate = ocb.Flight_Date.Value,
                                Origin = ocb.Orig,
                                Destination = ocb.Dest,
                                 // Ensure this is included

                                RestingStaffNumber = irs.RestingStaff,
                                RestingStaffFullName = rcDet != null ? rcDet.FullName : null,
                                RestingStaffCategory = rcDet != null ? rcDet.Category : null,
                                RestingCrewPosId = rcPos != null ? (int?)rcPos.Id : null,
                                RestingCrewPosCode = rpc != null ? rpc.PosCode : null, // Get raw position code from the join

                                TakingOverStaffNumber = irs.TakingOverStaff,
                                TakingOverStaffFullName = tocDet != null ? tocDet.FullName : null,
                                TakingOverStaffCategory = tocDet != null ? tocDet.Category : null,
                                TakingOverCrewPosId = tocPos != null ? (int?)tocPos.Id : null,
                                TakingOverCrewPosCode = topc != null ? topc.PosCode : null, // Get raw position code from the join

                                irs.Location,
                                irs.StartTime,
                                irs.EndTime,
                                irs.TotalTime
                            };

                // Apply mandatory date filters
                query = query.Where(item => item.FlightDate >= fromDate && item.FlightDate < effectiveToDate);

                // Apply optional filters
                if (flightNo.HasValue)
                {
                    // Assuming FlightNumber is an int from ocb.Flt_No, compare directly
                    query = query.Where(item => item.FlightNumber == flightNo.ToString());
                }

                if (originSectorId.HasValue)
                {
                    query = query.Where(item => db.AirportCodes.Any(ac => ac.Id == originSectorId.Value && ac.Code == item.Origin));
                }

                if (destSectorId.HasValue)
                {
                    query = query.Where(item => db.AirportCodes.Any(ac => ac.Id == destSectorId.Value && ac.Code == item.Destination));
                }

                // Execute the query to get data from the database into memory
                var resultsFromDb = await query.ToListAsync();

                // Second projection: Perform client-side processing (string formatting and null handling)
                return resultsFromDb.Select(item => new InflightRestSheetDisplayModel
                {
                    Id = item.Id,
                    FlightNumber = item.FlightNumber,
                    FlightDate = item.FlightDate,
                    Origin = item.Origin,
                    Destination = item.Destination,
                   // Assign SubmissionDate here

                    RestingStaffNumber = item.RestingStaffNumber,
                    RestingStaffFullName = item.RestingStaffFullName,
                    RestingStaffCategory = item.RestingStaffCategory,
                    RestingCrewPosId = item.RestingCrewPosId,
                    RestingCrewPos = item.RestingCrewPosCode ?? "N/A", // Apply "N/A" here

                    // Format the display string client-side
                    RestingStaffDisplay = !string.IsNullOrEmpty(item.RestingStaffFullName)
                                          ? $"{item.RestingStaffNumber} - {item.RestingStaffFullName} ({item.RestingStaffCategory ?? "N/A"})"
                                          : $"{item.RestingStaffNumber} (Details N/A)",

                    TakingOverStaffNumber = item.TakingOverStaffNumber,
                    TakingOverStaffFullName = item.TakingOverStaffFullName,
                    TakingOverStaffCategory = item.TakingOverStaffCategory,
                    TakingOverCrewPosId = item.TakingOverCrewPosId,
                    TakingOverCrewPos = item.TakingOverCrewPosCode ?? "N/A", // Apply "N/A" here

                    // Format the display string client-side
                    TakingOverStaffDisplay = !string.IsNullOrEmpty(item.TakingOverStaffFullName)
                                             ? $"{item.TakingOverStaffNumber} - {item.TakingOverStaffFullName} ({item.TakingOverStaffCategory ?? "N/A"})"
                                             : $"{item.TakingOverStaffNumber} (Details N/A)",

                    Location = item.Location,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                    TotalTime = item.TotalTime
                }).ToList();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error in GetFilteredInflightBreakData: {ex.ToString()}");
                throw; // Re-throw the exception after logging for proper error handling
                       // Alternatively, return an empty list: return new List<InflightRestSheetDisplayModel>();
            }
        }
    }
}