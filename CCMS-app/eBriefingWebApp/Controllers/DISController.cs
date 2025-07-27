using eBriefingWebApp.Helper;
using eBriefingWebApp.Interfaces;
using eBriefingWebApp.Models;
using eBriefingWebApp.ViewModels;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;

namespace eBriefingWebApp.Controllers
{
    public class DISController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public DISController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();

        // GET: eBriefing

         [Authorize(Roles = "CSM.Admins,CSM.IOC")]
        //[OutputCache(Duration = 900, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> Index(DateTime? Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                if (Block_Date == null)
                {
                    var blocks = db.OCB_Blocks.Where(a => DbFunctions.TruncateTime(a.Block_Date) == DbFunctions.TruncateTime(DateTime.Now) && a.OCB_Flights.Count > 0).ToList();
                    List<OCB_Flights> FlightList = new List<OCB_Flights>();
                    //List<DepInfoSheet> DISList = new List<DepInfoSheet>();
                    foreach(var blockData in blocks)
                    {
                        var flight = db.OCB_Flights.Where(a => a.BlockId == blockData.Id).ToList();
                        foreach(var fltData in flight)
                        {
                            var disdata = db.DepInfoSheets.Where(a => a.FlightId == fltData.Id).FirstOrDefault();
                            if(disdata != null)
                            {
                                //DISList.Add(disdata);
                                FlightList.Add(fltData);
                            }
                        }

                    }
                    
 
                    TempData["Block_Date"] = DateTime.Now;
                    return View(FlightList);
                }
                else
                {
                    var blocks = db.OCB_Blocks.Where(a => DbFunctions.TruncateTime(a.Block_Date) == DbFunctions.TruncateTime(Block_Date) && a.OCB_Flights.Count > 0).ToList();
                    List<OCB_Flights> FlightList = new List<OCB_Flights>();
                    //List<DepInfoSheet> DISList = new List<DepInfoSheet>();
                    foreach (var blockData in blocks)
                    {
                        var flight = db.OCB_Flights.Where(a => a.BlockId == blockData.Id).ToList();
                        foreach (var fltData in flight)
                        {
                            var disdata = db.DepInfoSheets.Where(a => a.FlightId == fltData.Id).FirstOrDefault();
                            if (disdata != null)
                            {
                                //DISList.Add(disdata);
                                FlightList.Add(fltData);
                            }
                        }
                    }
                        TempData["Block_Date"] = Block_Date;
                    return View(FlightList);
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            List<OCB_Flights> list = new List<OCB_Flights>();

            TempData["Block_Date"] = DateTime.Now;

            return View(list);
        }



        [System.Web.Mvc.HttpPost]
         [Authorize(Roles = "CSM.Admins,CSM.IOC")]
        public System.Web.Mvc.ActionResult Index(DateTime Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                return RedirectToAction("Index", "DIS", new { Block_Date = Block_Date });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            TempData["Block_Date"] = Block_Date;

            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("Index", "DIS", new { Block_Date = Block_Date });
        }


        [Authorize(Roles = "CSM.Admins,CSM.IOC")]
        //[OutputCache(Duration = 300, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> DISData(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

               
                var getFlights = db.OCB_Flights.Where(a => a.Id == Id).FirstOrDefault();
               
                    int fltNo = getFlights.Id;
                    var DISData = db.DepInfoSheets.Where(a => a.FlightId == fltNo).FirstOrDefault();
                    
                if (DISData == null)
                {
                    return RedirectToAction("", "DIS");
                }
              
               

                return View(DISData);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            

            return View();
        }



          [Authorize(Roles = "CSM.Admins,CSM.IOC")]
        public async Task<System.Web.Mvc.ActionResult> DISPrint(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var DIS = db.DepInfoSheets.Where(a => a.Id == Id).FirstOrDefault();

               

                return View(DIS);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("", "DIS");
        }

       

      
     
    }
}