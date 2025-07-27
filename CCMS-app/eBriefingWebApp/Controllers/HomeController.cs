using eBriefingWebApp.Helper;
using eBriefingWebApp.Interfaces;
using eBriefingWebApp.ViewModels;
using Microsoft.ApplicationInsights;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace eBriefingWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public HomeController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();

        // GET: Home
        //[Authorize(Roles = "CSM.Admins,CSM.Briefing,CSM.Appraisal,CSM.Support,CSM.Admins,CSM.GroundOps,CSM.Outstations,CSM.IOC,Transport.Supervisor,CSM.CrewLeave,CSM.CrewGrooming,CSM.DocumentApprover")]
        public async Task<ActionResult> Index()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            CSMEntities db = new CSMEntities();

            DashboardVM dash = new DashboardVM();

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                dash.BlocksCount = db.OCB_Blocks.Where(a => DbFunctions.TruncateTime(a.Block_Date) == DbFunctions.TruncateTime(DateTime.Now) && a.OCB_Flights.Count > 0).Count();
                dash.FlightsCount = db.OCB_Flights.Where(a => DbFunctions.TruncateTime(a.OCB_Blocks.Block_Date) == DbFunctions.TruncateTime(DateTime.Now)).Count();
                dash.CrewPosCount = db.OCB_CrewPos.Where(a => DbFunctions.TruncateTime(a.OCB_Blocks.Block_Date) == DbFunctions.TruncateTime(DateTime.Now)).Count();
                dash.NotificationsCount = db.OCB_Notification.Where(a => DbFunctions.TruncateTime(a.OCB_Flights.OCB_Blocks.Block_Date) == DbFunctions.TruncateTime(DateTime.Now)).Count();
                //dash.ApprisalsCount = db.CrewAppraisals.Where(a => DbFunctions.TruncateTime(a.OCB_Flights.OCB_Blocks.Block_Date) == DbFunctions.TruncateTime(DateTime.Now)).Count(); 
                dash.ApprisalsCount = db.CrewAppraisals.Where(ca => db.OCB_Blocks.Any(b => b.Block_Date == DbFunctions.TruncateTime(DateTime.Now) && b.Id == ca.BlockId)).Count();
                //dash.ApprisalsCount = 0;
                dash.FlightMealsCount = db.FlightMeals.Where(a => DbFunctions.TruncateTime(a.OCB_Flights.OCB_Blocks.Block_Date) == DbFunctions.TruncateTime(DateTime.Now)).Count();
                dash.SpecialMealsCount = db.SpecialMeals.Where(a => DbFunctions.TruncateTime(a.OCB_Flights.OCB_Blocks.Block_Date) == DbFunctions.TruncateTime(DateTime.Now)).Count();
                dash.SSRSCount = db.SSRs.Where(a => DbFunctions.TruncateTime(a.OCB_Flights.OCB_Blocks.Block_Date) == DbFunctions.TruncateTime(DateTime.Now)).Count();

            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
            }

            return View(dash);
        }

        //[Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> OutOfBase()
        {
            var telemetry = new TelemetryClient();

            List<OutOfBase> list = new List<OutOfBase>();

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
            }

            return View(list);
        }

        [HttpPost]
        //[Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> OutOfBase(DateTime FromDate, DateTime ToDate)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            var query = await db.OutOfBases.Where(a =>
            DbFunctions.TruncateTime(a.DateFrom) >= DbFunctions.TruncateTime(FromDate) &&
            DbFunctions.TruncateTime(a.DateTo) <= DbFunctions.TruncateTime(ToDate)).ToListAsync();

            ViewBag.FromDate = FromDate.ToString("dd-MMM-yyyy");
            ViewBag.ToDate = ToDate.ToString("dd-MMM-yyyy");

            return View(query);
        }

        public ActionResult Logout()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            HttpContext.GetOwinContext().Authentication.SignOut(OpenIdConnectAuthenticationDefaults.AuthenticationType);

            return RedirectToAction("Index", "Login");
        }

    }
}