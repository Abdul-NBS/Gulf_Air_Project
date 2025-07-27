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
    public class FAMController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public FAMController(IGetGraphUserService graphUserService)
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

                    //  var getFlights = db.OCB_Flights.Where(a => a.FAM_Form.Count > 0);
                    var getFlights = db.OCB_Flights.Where(a => DbFunctions.TruncateTime(a.Flight_Date) == DbFunctions.TruncateTime(DateTime.Now) && a.FAM_Form.Count > 0);
                    FFFormInfo ffForminfo;
                    List<FFFormInfo> ffInfoList = new List<FFFormInfo>();
                    foreach (var fltData in getFlights)
                    {
                        var ffList = db.FAM_Form.Where(a => a.Flight_Id == fltData.Id);
                        foreach (var ffData in ffList)
                        {
                            ffForminfo = new FFFormInfo();
                            ffForminfo.Flightid = fltData.Id;
                            ffForminfo.FlightNo = fltData.Flt_No;
                            ffForminfo.StaffNo = ffData.Staff_Number;
                            ffForminfo.fltDate = fltData.Flight_Date;
                            CrewDetail POSName = db.CrewDetails.Where(a => a.StaffNumber == ffData.Staff_Number).FirstOrDefault();
                            ffForminfo.StaffName = POSName.FullName;
                            ffForminfo.BlockId = Convert.ToInt32(fltData.BlockId);
                            ffInfoList.Add(ffForminfo);

                        }
                    }

                    TempData["Block_Date"] = DateTime.Now;
                    return View(ffInfoList);
                }
                else
                {
                    var getFlights = db.OCB_Flights.Where(a => DbFunctions.TruncateTime(a.Flight_Date) == Block_Date && a.FAM_Form.Count > 0);
                    FFFormInfo ffForminfo;
                    List<FFFormInfo> ffInfoList = new List<FFFormInfo>();
                    foreach (var fltData in getFlights)
                    {
                        var ffList = db.FAM_Form.Where(a => a.Flight_Id == fltData.Id);
                        foreach (var ffData in ffList)
                        {
                            ffForminfo = new FFFormInfo();
                            ffForminfo.Flightid = fltData.Id;
                            ffForminfo.FlightNo = fltData.Flt_No;
                            ffForminfo.StaffNo = ffData.Staff_Number;
                            ffForminfo.fltDate = fltData.Flight_Date;
                            ffForminfo.famId = ffData.Id;
                            CrewDetail POSName = db.CrewDetails.Where(a => a.StaffNumber == ffData.Staff_Number).FirstOrDefault();
                            ffForminfo.StaffName = POSName.FullName;
                            ffForminfo.BlockId = Convert.ToInt32(fltData.BlockId);
                            ffInfoList.Add(ffForminfo);

                        }
                    }

                    TempData["Block_Date"] = Block_Date;
                    return View(ffInfoList);


                    //
                    // return View(getFlights);

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
                return RedirectToAction("Index", "FAM", new { Block_Date = Block_Date });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            TempData["Block_Date"] = Block_Date;

            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("Index", "FAM", new { Block_Date = Block_Date });
        }


        [Authorize(Roles = "CSM.Admins,CSM.IOC")]
        //[OutputCache(Duration = 300, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> FAMFormData(int Id, DateTime? Block_Date,int famId)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

               
                    var FamData = db.FAM_Form.Where(a => a.Id == famId).FirstOrDefault();

                   

                return View(FamData);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            TempData["Block_Date"] = Block_Date;

            return View();
        }




        [Authorize(Roles = "CSM.Admins,CSM.IOC")]
        public async Task<System.Web.Mvc.ActionResult> FAMPrint(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var block = db.FAM_Form.Where(a => a.Id == Id).FirstOrDefault();

               

                return View(block);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("", "FAM");
        }

    }
        }
       