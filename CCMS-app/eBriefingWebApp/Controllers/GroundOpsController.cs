using eBriefingWebApp.Helper;
using eBriefingWebApp.Interfaces;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace eBriefingWebApp.Controllers
{
    public class GroundOpsController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public GroundOpsController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();

        // GET: GroundOps
        [Authorize(Roles = "CSM.Admins,CSM.GroundOps,CSM.Outstations")]
        public async Task<ActionResult> Index(DateTime? Block_DateFrom, DateTime? Block_DateTo)
        {
            DateTime today = DateTime.Now;

            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                DateTime? fromDate = Block_DateFrom ?? DateTime.Today;
                DateTime? toDate = Block_DateTo ?? DateTime.Today;

                var stationManagers = db.StationManagers
                    .Where(a => a.StaffNumber == getUser.OnPremisesSamAccountName)
                    .ToList();

                var flights = GetFlightsWithinDateRange(fromDate, toDate);

                if (stationManagers.Count > 0)
                {
                    flights = FilterFlightsByStationManager(flights, stationManagers);
                }

                TempData["Block_DateFrom"] = fromDate;
                TempData["Block_DateTo"] = toDate;

                return View(flights);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            List<OCB_Flights> list = new List<OCB_Flights>();

            TempData["Block_DateFrom"] = DateTime.Now;
            TempData["Block_DateTo"] = DateTime.Now;

            return View(list);
        }

        [Authorize(Roles = "CSM.Admins,CSM.GroundOps,CSM.Outstations")]
        [HttpPost]
        public ActionResult Index(DateTime Block_DateFrom, DateTime Block_DateTo)
        {
            var telemetry = new TelemetryClient();

            try
            {
                return RedirectToAction("Index", "GroundOps", new { Block_DateFrom = Block_DateFrom, Block_DateTo = Block_DateTo });
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";
            }

            TempData["Block_DateFrom"] = Block_DateFrom;
            TempData["Block_DateTo"] = Block_DateTo;

            return RedirectToAction("Index", "GroundOps", new { Block_Date = Block_DateFrom, Block_DateTo = Block_DateTo });
        }

        private List<OCB_Flights> GetFlightsWithinDateRange(DateTime? fromDate, DateTime? toDate)
        {
            return db.OCB_Flights
                .Where(a => DbFunctions.TruncateTime(a.Flight_Date) >= DbFunctions.TruncateTime(fromDate) &&
                            DbFunctions.TruncateTime(a.Flight_Date) <= DbFunctions.TruncateTime(toDate))
                .OrderBy(a => a.Flight_Date)
                .ToList();
        }

        // Helper method to filter flights based on station manager
        private List<OCB_Flights> FilterFlightsByStationManager(List<OCB_Flights> flights, List<StationManager> stationManagers)
        {
            return flights.Where(flight => stationManagers.Any(manager => manager.AirportCode == flight.Orig)).ToList();
        }

        [HttpPost]
        public async Task<ActionResult> StnMngr()
        {
            var telemetry = new TelemetryClient();
            //var StnManager
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }
                List<StationManager> StnMngr = db.StationManagers.ToList();
                return View(StnMngr);

            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";

            }
            return View();

        }
        [Authorize(Roles = "CSM.Admins,CSM.GroundOps,CSM.Outstations")]
        public async Task<ActionResult> PotableWaterPax(DateTime? Block_DateFrom, DateTime? Block_DateTo)
        {
            DateTime today = DateTime.Now;

            var telemetry = new TelemetryClient();

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                DateTime? fromDate = Block_DateFrom ?? DateTime.Today;
                DateTime? toDate = Block_DateTo ?? DateTime.Today;

                var stationManagers = db.StationManagers
                    .Where(a => a.StaffNumber == getUser.OnPremisesSamAccountName)
                    .ToList();

                var flights = GetFlightsWithinDateRange(fromDate, toDate);

                if (stationManagers.Count > 0)
                {
                    flights = FilterFlightsByStationManager(flights, stationManagers);
                }

                TempData["Block_DateFrom"] = fromDate;
                TempData["Block_DateTo"] = toDate;

                List<PotableWaterFlightPojo> potableWaterData = new List<PotableWaterFlightPojo>();
                //get pojo for portable water data
                PotableWaterFlightPojo potableWater;
                foreach (var flight in flights)
                {
                    potableWater = new PotableWaterFlightPojo();
                    var origin = flight.Orig;
                    var dest = flight.Dest;
                    DateTime? flightDate = null;
                    if (flight.Flight_Date.HasValue)
                    {
                        flightDate = flight.Flight_Date.Value.Date;
                    }
                    var flitNo = flight.Flt_No.PadLeft(4, '0');

                    int? actualCount = 0;
                    int? bookedcount = 0;
                    string seatCapacity = "N/A";
                    potableWater.Orig = origin;
                    potableWater.Dest = dest;
                    potableWater.EQP = flight.OCB_Blocks.Eqp;
                    potableWater.Flight_Date = flightDate;
                    potableWater.Flt_No = flight.Flt_No;

                    potableWater.potableData = flight.PotableWaters;
                    var otpFlightInfo = db.OtpFlightInfoes.FirstOrDefault(a =>
                                   a.actual_Departure_Airport == origin &&
                                   a.actual_Arrival_Airport == dest &&
                                   a.flight_Date == flightDate &&
                                   a.flight_Number == flitNo);

                    if (otpFlightInfo != null)
                    {
                        var passengerInfo = db.OtpPassengerDetails.FirstOrDefault(a =>
                            a.flight_Sequence_Number == otpFlightInfo.sequence_Number);

                        if (passengerInfo != null)
                        {
                            actualCount = passengerInfo.total;
                            bookedcount = (passengerInfo.bookedJ) + (passengerInfo.bookedY);
                            seatCapacity = passengerInfo.seatConfig;

                        }
                    }

                    potableWater.bookedCount = bookedcount;
                    potableWater.actualCount = actualCount;
                    potableWater.seatConfig = seatCapacity;

                    potableWaterData.Add(potableWater);
                }



                return View(potableWaterData);

                //return View(flights);
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";
            }

            List<OCB_Flights> list = new List<OCB_Flights>();

            TempData["Block_DateFrom"] = DateTime.Now;
            TempData["Block_DateTo"] = DateTime.Now;

            return View(list);
        }
        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.GroundOps,CSM.Outstations")]
        public ActionResult PotableWaterPax(DateTime Block_DateFrom, DateTime Block_DateTo)
        {
            var telemetry = new TelemetryClient();

            try
            {
                return RedirectToAction("PotableWaterPax", "GroundOps", new { Block_DateFrom = Block_DateFrom, Block_DateTo = Block_DateTo });
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";
            }

            TempData["Block_DateFrom"] = Block_DateFrom;
            TempData["Block_DateTo"] = Block_DateTo;

            return RedirectToAction("PotableWaterPax", "GroundOps", new { Block_Date = Block_DateFrom, Block_DateTo = Block_DateTo });
        }


        
    }

    public class PotableWaterFlightPojo
    {
        public ICollection<PotableWater> potableData { get; set; }

        public string Flt_No { get; set; }
        public Nullable<System.DateTime> Flight_Date { get; set; }
        public string Orig { get; set; }
        public string Dest { get; set; }

        public string EQP { get; set; }

        public int? bookedCount { get; set; }
        public int? actualCount { get; set; }
        public String seatConfig { get; set; }
    }
}