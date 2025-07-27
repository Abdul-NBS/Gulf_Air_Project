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
    public class eBriefingController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public eBriefingController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        //[OutputCache(Duration = 900, VaryByParam = "none")]
        public async Task<ActionResult> Index()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var blocks = db.OCB_Blocks.Where(a => DbFunctions.TruncateTime(a.Block_Date) == DbFunctions.TruncateTime(DateTime.Now)).ToList();

                return View(blocks);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            OCB_Blocks list = new OCB_Blocks();

            return View(list);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        //[OutputCache(Duration = 900, VaryByParam = "Id")]
        public async Task<ActionResult> BlockDetail(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = db.OCB_Blocks.Where(a => a.Id == Id).FirstOrDefault();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("", "eBriefing");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        //[OutputCache(Duration = 900, VaryByParam = "Id")]
        public async Task<ActionResult> FlightDetail(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = db.OCB_Flights.Where(a => a.Id == Id).FirstOrDefault();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("", "eBriefing");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> ManageNotification()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.OCB_Notification.ToListAsync();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("", "eBriefing");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewNotification()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var getFlights = await db.OCB_Flights.OrderBy(a => a.Flight_Date).ThenBy(a => a.OCB_Blocks.Start_Time).Where(a => DbFunctions.TruncateTime(a.Flight_Date) >= DbFunctions.TruncateTime(DateTime.Now)).ToListAsync();

                List<OCB_Flights> flights = new List<OCB_Flights>();

                foreach (var item in getFlights)
                {
                    flights.Add(new OCB_Flights
                    {
                        Id = item.Id,
                        Flt_No = "Flight#: " + item.Flt_No + " - Date: " + item.Flight_Date.Value.ToString("dd MMM yyyy")
                    });
                }

                ViewBag.FleetsList = flights;

            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            OCB_Notification form = new OCB_Notification();

            return View(form);
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewNotification(OCB_Notification form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                db.OCB_Notification.Add(new OCB_Notification
                {
                    Comments = form.Comments,
                    FlightId = form.FlightId,
                    guid = Guid.NewGuid().ToString(),
                    is_deleted = false,
                    Last_Update = DateTime.Now,
                });

                await db.SaveChangesAsync();

                TempData["Status"] = "Added";

                return RedirectToAction("ManageNotification", "eBriefing");
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            try
            {
                var getFlights = await db.OCB_Flights.OrderBy(a => a.Flight_Date).Where(a => DbFunctions.TruncateTime(a.Flight_Date) >= DbFunctions.TruncateTime(DateTime.Now)).ToListAsync();

                List<OCB_Flights> flights = new List<OCB_Flights>();

                foreach (var item in getFlights)
                {
                    flights.Add(new OCB_Flights
                    {
                        Id = item.Id,
                        Flt_No = "Flight#: " + item.Flt_No + " - Date: " + item.Flight_Date.Value.ToString("dd MMM yyyy")
                    });
                }

                ViewBag.FleetsList = flights;
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return View(form);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> DeleteNotification(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.OCB_Notification.Where(a => a.Id == Id).FirstOrDefaultAsync();

                if (query != null)
                {
                    db.OCB_Notification.Remove(query);

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Deleted";
                }
                else
                {
                    TempData["Status"] = "Not Found";
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("ManageNotification", "eBriefing");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditNotification(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var getFlights = await db.OCB_Flights.OrderBy(a => a.Flight_Date).Where(a => DbFunctions.TruncateTime(a.Flight_Date) >= DbFunctions.TruncateTime(DateTime.Now)).ToListAsync();

                List<OCB_Flights> flights = new List<OCB_Flights>();

                foreach (var item in getFlights)
                {
                    flights.Add(new OCB_Flights
                    {
                        Id = item.Id,
                        Flt_No = "Flight#: " + item.Flt_No + " - Date: " + item.Flight_Date.Value.ToString("dd MMM yyyy")
                    });
                }

                ViewBag.FleetsList = flights;

                var query = await db.OCB_Notification.Where(a => a.Id == Id).FirstOrDefaultAsync();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("ManageNotification", "eBriefing");
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditNotification(OCB_Notification form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getFlights = await db.OCB_Flights.OrderBy(a => a.Flight_Date).Where(a => DbFunctions.TruncateTime(a.Flight_Date) >= DbFunctions.TruncateTime(DateTime.Now)).ToListAsync();

                List<OCB_Flights> flights = new List<OCB_Flights>();

                foreach (var item in getFlights)
                {
                    flights.Add(new OCB_Flights
                    {
                        Id = item.Id,
                        Flt_No = "Flight#: " + item.Flt_No + " - Date: " + item.Flight_Date.Value.ToString("dd MMM yyyy")
                    });
                }

                ViewBag.FleetsList = flights;

                var query = await db.OCB_Notification.Where(a => a.Id == form.Id).FirstOrDefaultAsync();

                if (query != null)
                {
                    query.Comments = form.Comments;

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Updated";

                    return RedirectToAction("ManageNotification", "eBriefing");
                }
                else
                {
                    TempData["Status"] = "Not Found";
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View(form);
        }
    }
}