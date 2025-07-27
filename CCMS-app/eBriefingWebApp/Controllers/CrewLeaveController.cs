using eBriefingWebApp.Helper;
using eBriefingWebApp.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace eBriefingWebApp.Controllers
{
    public class CrewLeaveController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public CrewLeaveController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();
       
        [Authorize(Roles = "Transport.Supervisor,CSM.CrewLeave")]
        public async Task<System.Web.Mvc.ActionResult> CrewLeaveRequests(DateTime? From_Block_Date, DateTime? To_Block_Date)
        {

            if (From_Block_Date == null || To_Block_Date == null)
            {
                From_Block_Date = DateTime.Now.AddDays(-3);
                To_Block_Date = DateTime.Now;

            }

            if (From_Block_Date > To_Block_Date)
            {
                DateTime? tempDate = From_Block_Date;
                From_Block_Date = To_Block_Date;
                To_Block_Date = tempDate;
            }
            var leaveReq = db.CrewLeaveRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.SUBMITTED_TIME >= From_Block_Date && a.SUBMITTED_TIME <= To_Block_Date).ToList();
            TempData["From_Block_Date"] = From_Block_Date;
            TempData["To_Block_Date"] = To_Block_Date;
            return View(leaveReq);

        }

        [System.Web.Mvc.HttpPost]
       [Authorize(Roles = "Transport.Supervisor,CSM.CrewLeave")]
        public async Task<System.Web.Mvc.ActionResult> CrewLeaveRequests(DateTime From_Block_Date, DateTime To_Block_Date)
        {
            var telemetry = new TelemetryClient();
            try
            {
                return RedirectToAction("CrewLeaveRequests", "CrewLeave", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";
            }
            return RedirectToAction("CrewLeaveRequests", "CrewLeave", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
        }
        [Authorize(Roles = "Transport.Supervisor,CSM.CrewLeave")]
        public async Task<System.Web.Mvc.ActionResult> CrewLeaveToReview()
        {


            var leaveReq = db.CrewLeaveRequests.Where(a => a.IsActive == true && a.IsDeleted == false && (a.STATUS == "S" || a.STATUS == "P")).OrderByDescending(a => a.Id).ToList();

            return View(leaveReq);

        }
        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "Transport.Supervisor,CSM.CrewLeave")]
        public async Task<ActionResult> ToBeReviewed(int Id, string AdminComments)
        {
            try
            {
                using (var db = new eBriefingWebApp.CSMEntities()) // Use a using block for proper disposal
                {
                    var leaveRequest = await db.CrewLeaveRequests.FindAsync(Id); // Use FindAsync for asynchronous operation

                    if (leaveRequest == null)
                    {
                        return HttpNotFound(); // Or return a JSON error response
                    }
                    var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                    String StaffNumber = "";
                    if (getUser != null)
                    {
                        StaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                        Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    }

                    leaveRequest.STATUS = "P";
                    leaveRequest.ADMIN_REASON = AdminComments;
                    leaveRequest.Updated_Dt = DateTime.Now;
                    leaveRequest.Updated_By = StaffNumber;

                    await db.SaveChangesAsync(); // Use SaveChangesAsync for asynchronous operation
                }

                return Json(new { success = true }); // Return JSON for AJAX success
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine(ex.Message); // Or use a proper logging framework

                // Return a JSON error response with a user-friendly message (do NOT expose raw exception details in production)
                return Json(new { success = false, message = "An error occurred while approving the leave request." });
            }
        }
        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "Transport.Supervisor,CSM.CrewLeave")]
        public async Task<ActionResult> ApproveLeave(int Id, string AdminComments)
        {
            try
            {
                using (var db = new eBriefingWebApp.CSMEntities()) // Use a using block for proper disposal
                {
                    var leaveRequest = await db.CrewLeaveRequests.FindAsync(Id); // Use FindAsync for asynchronous operation

                    if (leaveRequest == null)
                    {
                        return HttpNotFound(); // Or return a JSON error response
                    }
                    var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                    String StaffNumber = "";
                    if (getUser != null)
                    {
                        StaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                        Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    }

                    leaveRequest.STATUS = "A";
                    leaveRequest.ADMIN_REASON = AdminComments;
                    leaveRequest.Updated_Dt = DateTime.Now;
                    leaveRequest.Updated_By = StaffNumber;

                    await db.SaveChangesAsync(); // Use SaveChangesAsync for asynchronous operation
                }

                return Json(new { success = true }); // Return JSON for AJAX success
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine(ex.Message); // Or use a proper logging framework

                // Return a JSON error response with a user-friendly message (do NOT expose raw exception details in production)
                return Json(new { success = false, message = "An error occurred while approving the leave request." });
            }
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "Transport.Supervisor,CSM.CrewLeave")]
        public async Task<ActionResult> RejectLeave(int Id, string AdminComments)
        {
            try
            {
                using (var db = new eBriefingWebApp.CSMEntities()) // Use a using block for proper disposal
                {
                    var leaveRequest = await db.CrewLeaveRequests.FindAsync(Id); // Use FindAsync for asynchronous operation

                    if (leaveRequest == null)
                    {
                        return HttpNotFound(); // Or return a JSON error response
                    }

                    var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                    String StaffNumber = "";
                    if (getUser != null)
                    {
                        StaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                        Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    }

                    leaveRequest.STATUS = "R";
                    leaveRequest.ADMIN_REASON = AdminComments;
                    leaveRequest.Updated_Dt = DateTime.Now;
                    leaveRequest.Updated_By = StaffNumber;

                    await db.SaveChangesAsync(); // Use SaveChangesAsync for asynchronous operation
                }

                return Json(new { success = true }); // Return JSON for AJAX success
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine(ex.Message); // Or use a proper logging framework

                // Return a JSON error response with a user-friendly message (do NOT expose raw exception details in production)
                return Json(new { success = false, message = "An error occurred while approving the leave request." });
            }
        }
        [Authorize(Roles = "Transport.Supervisor,CSM.CrewLeave")]
        public async Task<System.Web.Mvc.ActionResult> RosterPeriodData()
        {
            var rosterData = db.CrewLeaveRosterPeriods.Where(a=>a.IsActive == true && a.IsDeleted == false).FirstOrDefault();
           // (TempData["From_Block_Date"]) = rosterData.FROM_DATE;
          
            return View(rosterData);
        }

        [HttpPost]
        [Authorize(Roles = "Transport.Supervisor,CSM.CrewLeave")]
        public async Task<ActionResult> AddRosterDate(DateTime From_Block_Date, DateTime To_Block_Date)
        {
           
                try
                {
                    var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                    String StaffNumber = "";
                    if (getUser != null)
                    {
                        StaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                        Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    }
                    

                    var existingRosters = db.CrewLeaveRosterPeriods.Where(a => a.IsActive == true).ToList(); 


                    foreach (var existingRoster in existingRosters)
                    {
                        existingRoster.IsActive = false;
                        db.SaveChanges();
                    }
                    CrewLeaveRosterPeriod newRosterData = new CrewLeaveRosterPeriod();
                    newRosterData.IsActive = true;
                    newRosterData.IsDeleted = false;
                    newRosterData.FROM_DATE = From_Block_Date;
                    newRosterData.TO_DATE = To_Block_Date;
                    newRosterData.Updated_DT = DateTime.Now;
                    newRosterData.Updated_By = StaffNumber;
                    db.CrewLeaveRosterPeriods.Add(newRosterData);
                    db.SaveChanges();

                (TempData["Status"]) = "Updated";
                return RedirectToAction("RosterPeriodData", "CrewLeave");
                }
                catch (Exception ex) { 

                }

            return RedirectToAction("RosterPeriodData", "CrewLeave");
        }

        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<ActionResult> ManageAnnouncements()
        {

            try
            {
                CrewLeaveAnnouncement announcement = db.CrewLeaveAnnouncements.Where(a => a.IsActive == true).FirstOrDefault();
                if (announcement != null)
                {
                    return View(announcement);
                }

               
            }
            catch (Exception ex) {
                var telemetry = new TelemetryClient();
                telemetry.TrackException(ex);
            }
            CrewLeaveAnnouncement newAnnouncemnet = new CrewLeaveAnnouncement();

            return View(newAnnouncemnet);
        }

        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<ActionResult> UpdateAnnouncement(CrewLeaveAnnouncement crewLeaveAnnouncement)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            String StaffNumber = "";
            if (getUser != null)
            {
                StaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }
            if (ModelState.IsValid)
            {
                if (crewLeaveAnnouncement.Announcement != null)
                {
                    var pastannouncemnets = db.CrewLeaveAnnouncements.Where(a=>a.IsActive== true).ToList();

                    if (pastannouncemnets != null && pastannouncemnets.Count > 0)
                    {
                        foreach(var pastannouncement in pastannouncemnets)
                        {
                            pastannouncement.IsActive = false;
                            db.SaveChanges();
                        }
                    }

                    CrewLeaveAnnouncement newAnnouncement = new CrewLeaveAnnouncement
                    {
                        Announcement = crewLeaveAnnouncement.Announcement,
                        IsActive = true,
                        IsDeleted = false,
                        Updated_Dt = DateTime.Now,
                        Updated_By = StaffNumber
                    };
                    db.CrewLeaveAnnouncements.Add(newAnnouncement);
                    db.SaveChanges();


                    return RedirectToAction("CrewLeaveToReview", "CrewLeave"); // Redirect to refresh the form
                }
                else
                {
                    return RedirectToAction("CrewLeaveToReview", "CrewLeave");
                }
            }
            return RedirectToAction("CrewLeaveToReview", "CrewLeave");


            // If ModelState is not valid or announcement is null, return the view with errors



        }

    }
}