using eBriefingWebApp.Helper;
using eBriefingWebApp.Interfaces;
using eBriefingWebApp.Models;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace eBriefingWebApp.Controllers
{
    public class PilotController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public PilotController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();

        public async Task<ActionResult> CrewEffectiveSeniority()
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            String StaffNumber = "";
            //  StaffNumber = getUser.OnPremisesSamAccountName;

            // Session["FullName"] = getUser.GivenName + " " + getUser.Surname;


            if (getUser != null)
            {
                StaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;

                var givenName = getUser.GivenName ?? string.Empty;
                var surname = getUser.Surname ?? string.Empty;
                Session["FullName"] = givenName + " " + surname;
            }
            else
            {
                StaffNumber = string.Empty;
                Session["FullName"] = string.Empty;
            }

            // get seniority bid

            StaffNumber = "813044";
            if (!string.IsNullOrEmpty(StaffNumber))
            {
                CrewSeniority crwSeniority = new CrewSeniority();
                var seniority = db.CrewEffectiveSeniorities.Where(a => a.CrewId == StaffNumber).FirstOrDefault();
                if (seniority != null)
                {
                    List<CrewPeriodSeniority> periodSeniorityList = new List<CrewPeriodSeniority>();
                    CrewPeriodSeniority crewPeriodSeniority;
                    if (seniority != null)
                    {
                        crwSeniority.CrewId = seniority.CrewId;
                        crwSeniority.group = seniority.GroupNo;
                        crwSeniority.groupSeniority = seniority.GroupSeniority;
                        crwSeniority.crewName = seniority.PilotCrewDetail.FullName;
                        crwSeniority.crewPosition = seniority.PilotCrewDetail.Category;
                        crwSeniority.fleet = seniority.Fleet;
                        crwSeniority.NetSeniority = seniority.NetSeniority;
                        var periodSeniority = db.CrewEffectiveSeniorityPeriods.Where(a => a.CESID == seniority.ID).ToList();

                        foreach (var period in periodSeniority)
                        {
                            var periodDetail = db.Seniority_PeriodDetails.Where(a => a.ID == period.PeriodId).FirstOrDefault();
                            if (periodDetail != null)
                            {
                                crewPeriodSeniority = new CrewPeriodSeniority();
                                crewPeriodSeniority.periodMonth = periodDetail.PeriodMonth;
                                crewPeriodSeniority.periodYear = periodDetail.PeriodYear;
                                crewPeriodSeniority.periodSeniority = period.PeriodSeniority;
                                String periodStr = crewPeriodSeniority.periodMonth.ToString() + "/" + crewPeriodSeniority.periodYear.ToString();
                                DateTime parsedDate = DateTime.ParseExact(periodStr, "M/yyyy", CultureInfo.InvariantCulture);
                                string formattedDate = parsedDate.ToString("MMM yyyy");
                                crewPeriodSeniority.Period = formattedDate;
                                periodSeniorityList.Add(crewPeriodSeniority);



                            }
                        }

                        crwSeniority.periodseniority = periodSeniorityList;
                    }

                    return View(crwSeniority);
                }
                else
                {
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        public async Task<System.Web.Mvc.ActionResult> LayoverBidData()
        {
            var telemetry = new TelemetryClient();
            List<Layover_Bids> layover_Bids = new List<Layover_Bids>();
            List<Maximum_Bids> maxBids = new List<Maximum_Bids>();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                layover_Bids = db.Layover_Bids.Where(a => a.IsActive == true).ToList();



            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";
            }


            return View(layover_Bids);
        }

        public async Task<System.Web.Mvc.ActionResult> LayoverPairingIndex()
        {
            var telemetry = new TelemetryClient();
            try
            {
                List<Layover_Pairing> layoverPairings = db.Layover_Pairing.Where(a => a.IsActive == true && a.IsDeleted == false).ToList();
                return View(layoverPairings);
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";
            }
            return View();
        }
        public async Task<System.Web.Mvc.ActionResult> FltSepDaysCalculator(int id)
        {
            var telemetry = new TelemetryClient();
            try
            {

                Layover_Pairing data = db.Layover_Pairing.Where(a => a.ID == id).FirstOrDefault();
                // Layover_Pairing layover_pairing = new Layover_Pairing();
                return View(data);
            }
            catch (Exception ex)
            {
            }
            return null;
        }
        [System.Web.Mvc.HttpPost]
        public async Task<System.Web.Mvc.ActionResult> FltSepDaysCalculator(Layover_Pairing pairingData, FormCollection form)
        {
            var telemetry = new TelemetryClient();
            try
            {
                DateTime Opr_date = Convert.ToDateTime(form["Operation_Date"]);
                int addDays = 0;
                Layover_Pairing data = db.Layover_Pairing.Where(a => a.ID == pairingData.ID).FirstOrDefault();
                int? Pair_Cat_id = data.Pair_Cat_ID;
                if (Pair_Cat_id == 1)
                {
                    addDays = 90;
                }
                else if (Pair_Cat_id == 2)
                {
                    addDays = 90;
                }
                else if (Pair_Cat_id == 3)
                {
                    addDays = 60;
                }

                DateTime next_oper_Dt = Opr_date.AddDays(addDays);
                string formattedDate = next_oper_Dt.ToString("MM/dd/yyyy");

                ViewBag.NextOpr = formattedDate;
                string formattedDateOpr = Opr_date.ToString("MM/dd/yyyy");
                ViewBag.OprDate = formattedDateOpr;
                // Layover_Pairing layover_pairing = new Layover_Pairing();
                return View(data);
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        public async Task<System.Web.Mvc.ActionResult> CrewLeaveRequest(DateTime? From_Block_Date, DateTime? To_Block_Date, int? CrewStaffNumber)
        {

            CrewLeaveRequest request = new CrewLeaveRequest();
            var publishedRoster = db.CrewLeaveRosterPeriods.Where(a => a.IsActive == true).FirstOrDefault();
            ViewBag.RosterFromDate = publishedRoster.FROM_DATE;
            ViewBag.RosterToDate = publishedRoster.TO_DATE;
            ViewBag.MinRequestDate = DateTime.Now.AddDays(3);
            return View(request);
        }
        [System.Web.Mvc.HttpPost]
        public async Task<System.Web.Mvc.ActionResult> RequestLeave(CrewLeaveRequest req)
        {
            var telemetry = new TelemetryClient();
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            String StaffNumber = "";
            if (getUser != null)
            {
                StaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;

                var givenName = getUser.GivenName ?? string.Empty;
                var surname = getUser.Surname ?? string.Empty;
                Session["FullName"] = givenName + " " + surname;
            }
            else
            {
                StaffNumber = string.Empty;
                Session["FullName"] = string.Empty;
            }

            if (!string.IsNullOrEmpty(StaffNumber))
            {
                CrewLeaveRequest newReq = new CrewLeaveRequest();
                newReq.StaffNumber = StaffNumber;
                newReq.FROM_DATE = req.FROM_DATE;
                newReq.TO_DATE = req.TO_DATE;
                newReq.LEAVE_REASON = req.LEAVE_REASON;
                newReq.SUBMITTED_TIME = DateTime.Now;
                newReq.STATUS = "S";
                newReq.IsActive = true;
                newReq.IsDeleted = false;
                db.CrewLeaveRequests.Add(newReq);
                db.SaveChanges();

            }

            return RedirectToAction("CrewLeaveRequests", "CrewLeave");


        }

        public async Task<System.Web.Mvc.ActionResult> CrewLeaveRequests()
        {
            var telemetry = new TelemetryClient();
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            String StaffNumber = "";
            if (getUser != null)
            {
                StaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;

                var givenName = getUser.GivenName ?? string.Empty;
                var surname = getUser.Surname ?? string.Empty;
                Session["FullName"] = givenName + " " + surname;
            }
            else
            {
                StaffNumber = string.Empty;
                Session["FullName"] = string.Empty;
            }

            var leaveReq = db.CrewLeaveRequests.Where(a => a.StaffNumber == StaffNumber && a.IsActive == true && a.IsDeleted == false)
     .OrderByDescending(a => a.Id) // Order by Id descending
     .ToList();

            return View(leaveReq);

        }




    }
}