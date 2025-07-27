using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Threading.Tasks;
using eBriefingWebApp.Interfaces;
using eBriefingWebApp.Helper;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights;
using System.Runtime.Remoting.Contexts;
using eBriefingWebApp.Models;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Graph.Models.Security;

namespace eBriefingWebApp.Controllers
{
    public class CrewFlightFormController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public CrewFlightFormController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();

       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> Index(DateTime? From_Block_Date, DateTime? To_Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            try
            {
                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }
                /*
                                if (Block_Date == null)
                                {
                                    var blocks = db.CrewFlightForms.Where(a => DbFunctions.TruncateTime(a.Submit_Date_and_Time) == DbFunctions.TruncateTime(DateTime.Now)).ToList();
                                    TempData["Block_Date"] = DateTime.Now;
                                    return View(blocks);
                                }
                                else
                                {
                                    var blocks = db.CrewFlightForms.Where(a => DbFunctions.TruncateTime(a.Submit_Date_and_Time) == DbFunctions.TruncateTime(Block_Date)).ToList();

                                    TempData["Block_Date"] = Block_Date;
                                    return View(blocks);
                                }*/

                if (From_Block_Date == null && To_Block_Date == null)
                {
                    var blocks = db.CrewFlightForms.Where(a => DbFunctions.TruncateTime(a.Submit_Date_and_Time) == DbFunctions.TruncateTime(DateTime.Now)).ToList();
                    TempData["From_Block_Date"] = DateTime.Now;
                    TempData["To_Block_Date"] = DateTime.Now;
                    return View(blocks);
                }
                else
                {
                    DateTime? fromDate = From_Block_Date != null ? From_Block_Date : DateTime.Now;
                    DateTime? toDate = To_Block_Date != null ? To_Block_Date : DateTime.Now;
                    if (toDate >= fromDate)
                    {
                        var blocks = db.CrewFlightForms.Where(a => DbFunctions.TruncateTime(a.Submit_Date_and_Time) >= DbFunctions.TruncateTime(fromDate)
                                         && DbFunctions.TruncateTime(a.Submit_Date_and_Time) <= DbFunctions.TruncateTime(toDate)).ToList();
                        TempData["From_Block_Date"] = fromDate;
                        TempData["To_Block_Date"] = toDate;
                        return View(blocks);
                    }
                    else
                    {
                        DateTime? newFromDate = toDate;
                        DateTime? newToDate = fromDate;
                        var blocks = db.CrewFlightForms.Where(a => DbFunctions.TruncateTime(a.Submit_Date_and_Time) >= DbFunctions.TruncateTime(newFromDate)
                                          && DbFunctions.TruncateTime(a.Submit_Date_and_Time) <= DbFunctions.TruncateTime(newToDate)).ToList();
                        TempData["From_Block_Date"] = newFromDate;
                        TempData["To_Block_Date"] = newToDate;
                        return View(blocks);
                    }
                }
                }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }
            CrewFlightForm list = new CrewFlightForm();

            return View(list);

        }
        [System.Web.Mvc.HttpPost]
       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public System.Web.Mvc.ActionResult Index(DateTime From_Block_Date, DateTime To_Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                return RedirectToAction("Index", "CrewFlightForm", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            TempData["From_Block_Date"] = From_Block_Date;
            TempData["To_Block_Date"] = To_Block_Date;

            return RedirectToAction("Index", "CrewFlightForm", new   { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
        }


        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CrewFlightFormInfo(int Id , DateTime From_Block_Date, DateTime To_Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                CrewFlightForm crewObj = db.CrewFlightForms.Where(a => a.Id == Id).FirstOrDefault();
                CrewFltForm crewFltForm = new CrewFltForm();
                crewFltForm.crewFlt = crewObj;
                SectionQuestionAnswer qna;
                CrewFltSectionObj crewFltSectionObj;
                List<SectionQuestionAnswer> qnaList ;
                List<CrewFltSectionObj> CrewFltSectionObjList = new List<CrewFltSectionObj>();

                if (crewObj != null)
                {

                    List<FlightFormSection> sections = db.FlightFormSections.Where(a => a.Flight_Form_Id == crewObj.Flight_Form_Id).ToList();

                    foreach (var section in sections)
                    {
                        
                        
                            crewFltSectionObj = new CrewFltSectionObj();
                            crewFltSectionObj.SectionId = section.Id;
                            crewFltSectionObj.SectionName = section.SectionTitle;
                            List<FlightFormSectionItem> sectionItemNums = db.FlightFormSectionItems.Where(a => a.Flight_Form_Section_Id == section.Id).ToList();
                            qnaList = new List<SectionQuestionAnswer>();
                            foreach (var sectionItem in sectionItemNums)
                            {

                                var results = (from a in db.CrewFlightFormAnswers
                                               join b in db.FlightFormSectionItems on a.Flight_Form_Section_Item_Id equals b.Id
                                               join c in db.FlightFormSections on b.Flight_Form_Section_Id equals c.Id
                                               where b.Id == sectionItem.Id && a.Crew_Flight_Form_Id == crewObj.Id
                                               select new
                                               {
                                                   CrewFlightFormId = a.Crew_Flight_Form_Id,
                                                   FlightFormSectionItemId = a.Flight_Form_Section_Item_Id,
                                                   Answer = a.Answer,
                                                   FlightFormSectionId = b.Flight_Form_Section_Id,
                                                   SectionTitle = c.SectionTitle,
                                                   ItemTitle = b.ItemTitle
                                               }).ToList();
                                //qnaList = new List<SectionQuestionAnswer>();
                                foreach (var result in results)
                                {

                                    qna = new SectionQuestionAnswer();
                                    qna.Question = result.ItemTitle;
                                    qna.Answer = result.Answer;
                                    qnaList.Add(qna);

                                }
                            }
                                crewFltSectionObj.qnalist = qnaList;
                                CrewFlightFormSectionComment secCommnet = db.CrewFlightFormSectionComments.Where(c => c.Flight_Form_Section_Id == section.Id && c.Crew_Flight_Form_Id == crewObj.Id).FirstOrDefault();
                                if (secCommnet != null)
                                {
                                    crewFltSectionObj.secComment = secCommnet.Comments;
                                }
                                CrewFltSectionObjList.Add(crewFltSectionObj);
                            
                        
                    }
                }
                crewFltForm.sections = CrewFltSectionObjList;
                TempData["From_Block_Date"] = From_Block_Date;
                TempData["To_Block_Date"] = To_Block_Date;

                return View(crewFltForm);
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }

            CrewFltForm list = new CrewFltForm();

            return View(list);
        
    }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> PrintCrewFlightFormInfo(int Id)
        {

            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                CrewFlightForm crewObj = db.CrewFlightForms.Where(a => a.Id == Id).FirstOrDefault();
                CrewFltForm crewFltForm = new CrewFltForm();
                crewFltForm.crewFlt = crewObj;
                SectionQuestionAnswer qna;
                CrewFltSectionObj crewFltSectionObj;
                List<SectionQuestionAnswer> qnaList;
                List<CrewFltSectionObj> CrewFltSectionObjList = new List<CrewFltSectionObj>();

                if (crewObj != null)
                {

                    List<FlightFormSection> sections = db.FlightFormSections.Where(a => a.Flight_Form_Id == crewObj.Flight_Form_Id).ToList();

                    foreach (var section in sections)
                    {


                        crewFltSectionObj = new CrewFltSectionObj();
                        crewFltSectionObj.SectionId = section.Id;
                        crewFltSectionObj.SectionName = section.SectionTitle;
                        List<FlightFormSectionItem> sectionItemNums = db.FlightFormSectionItems.Where(a => a.Flight_Form_Section_Id == section.Id).ToList();
                        qnaList = new List<SectionQuestionAnswer>();
                        foreach (var sectionItem in sectionItemNums)
                        {

                            var results = (from a in db.CrewFlightFormAnswers
                                           join b in db.FlightFormSectionItems on a.Flight_Form_Section_Item_Id equals b.Id
                                           join c in db.FlightFormSections on b.Flight_Form_Section_Id equals c.Id
                                           where b.Id == sectionItem.Id && a.Crew_Flight_Form_Id == crewObj.Id
                                           select new
                                           {
                                               CrewFlightFormId = a.Crew_Flight_Form_Id,
                                               FlightFormSectionItemId = a.Flight_Form_Section_Item_Id,
                                               Answer = a.Answer,
                                               FlightFormSectionId = b.Flight_Form_Section_Id,
                                               SectionTitle = c.SectionTitle,
                                               ItemTitle = b.ItemTitle
                                           }).ToList();
                            //qnaList = new List<SectionQuestionAnswer>();
                            foreach (var result in results)
                            {

                                qna = new SectionQuestionAnswer();
                                qna.Question = result.ItemTitle;
                                qna.Answer = result.Answer;
                                qnaList.Add(qna);

                            }
                        }
                        crewFltSectionObj.qnalist = qnaList;
                        CrewFlightFormSectionComment secCommnet = db.CrewFlightFormSectionComments.Where(c => c.Flight_Form_Section_Id == section.Id && c.Crew_Flight_Form_Id == crewObj.Id).FirstOrDefault();
                        if (secCommnet != null)
                        {
                            crewFltSectionObj.secComment = secCommnet.Comments;
                        }
                        CrewFltSectionObjList.Add(crewFltSectionObj);


                    }
                }
                crewFltForm.sections = CrewFltSectionObjList;

                return View(crewFltForm);
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }

            CrewFltForm list = new CrewFltForm();

            return View(list);

        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> OverallIndex(DateTime? Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            try
            {
                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                int currentMonth = 0;
                int currentYear = 0;
                if (Block_Date == null)
                {
                    DateTime now = DateTime.Now;

                    currentMonth = now.Month;
                    currentYear = now.Year;
                    TempData["Block_Date"] = DateTime.Now;
                }
                else
                {
                    currentMonth = Block_Date.Value.Month;
                    currentYear = Block_Date.Value.Year;
                    TempData["Block_Date"] = Block_Date;
                }

                var teamLeads = db.Appraisal_Team_Leads.ToList();
                OssCountPojo ossCountPojo;
                List<OssCountPojo> ossList = new List<OssCountPojo>();
                foreach(var tl in teamLeads)
             
                {
                     ossCountPojo = new OssCountPojo();
                    ossCountPojo.StaffNumber = tl.StaffNumber;
                    var StaffInfo= db.CrewDetails.Where(a=>a.StaffNumber==tl.StaffNumber).FirstOrDefault();

                    if(StaffInfo != null)
                    {
                        ossCountPojo.StaffName=StaffInfo.FullName;
                    }
                    
                    var CrewFlightforms = db.CrewFlightForms.Where(a =>a.StaffNumber==tl.StaffNumber &&  a.Submit_Date_and_Time.Month == currentMonth && a.Submit_Date_and_Time.Year == currentYear).ToList();
                 
                    if (CrewFlightforms != null ) {
                        ossCountPojo.OssCount = CrewFlightforms.Count();
                    }
                    ossList.Add( ossCountPojo );
                    
                }
                return View(ossList);

            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }
            Appraisal list = new Appraisal();

            return View(list);

        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> TLDataInfo(String Id , DateTime Month)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            try
            {
                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                int currentMonth = 0;
                int currentYear = 0;
                if (Month == null)
                {
                    DateTime now = DateTime.Now;

                    currentMonth = now.Month;
                    currentYear = now.Year;
                    TempData["Block_Date"] = DateTime.Now;
                }
                else
                {
                    currentMonth = Month.Month;
                    currentYear = Month.Year;
                    TempData["Block_Date"] = Month;
                }

                var CrewFlightforms = db.CrewFlightForms.Where(a => a.StaffNumber == Id && a.Submit_Date_and_Time.Month == currentMonth && a.Submit_Date_and_Time.Year == currentYear).ToList();
                var staffDetail = db.CrewDetails.Where(a=>a.StaffNumber == Id).FirstOrDefault();
                if (staffDetail != null) {
                    TempData["TLname"] = staffDetail.FullName;
                    TempData["TLStaffNo"] = Id;
                }
                else
                {
                    TempData["TLname"] = "";
                    TempData["TLStaffNo"] = Id;
                }
                    return View(CrewFlightforms);
                
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }
            Appraisal list = new Appraisal();

            return View(list);
        }
       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> CrewFlightFormInfoFromOverall(int Id, DateTime Month)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                CrewFlightForm crewObj = db.CrewFlightForms.Where(a => a.Id == Id).FirstOrDefault();
                CrewFltForm crewFltForm = new CrewFltForm();
                crewFltForm.crewFlt = crewObj;
                SectionQuestionAnswer qna;
                CrewFltSectionObj crewFltSectionObj;
                List<SectionQuestionAnswer> qnaList;
                List<CrewFltSectionObj> CrewFltSectionObjList = new List<CrewFltSectionObj>();

                if (crewObj != null)
                {

                    List<FlightFormSection> sections = db.FlightFormSections.Where(a => a.Flight_Form_Id == crewObj.Flight_Form_Id).ToList();

                    foreach (var section in sections)
                    {


                        crewFltSectionObj = new CrewFltSectionObj();
                        crewFltSectionObj.SectionId = section.Id;
                        crewFltSectionObj.SectionName = section.SectionTitle;
                        List<FlightFormSectionItem> sectionItemNums = db.FlightFormSectionItems.Where(a => a.Flight_Form_Section_Id == section.Id).ToList();
                        qnaList = new List<SectionQuestionAnswer>();
                        foreach (var sectionItem in sectionItemNums)
                        {

                            var results = (from a in db.CrewFlightFormAnswers
                                           join b in db.FlightFormSectionItems on a.Flight_Form_Section_Item_Id equals b.Id
                                           join c in db.FlightFormSections on b.Flight_Form_Section_Id equals c.Id
                                           where b.Id == sectionItem.Id && a.Crew_Flight_Form_Id == crewObj.Id
                                           select new
                                           {
                                               CrewFlightFormId = a.Crew_Flight_Form_Id,
                                               FlightFormSectionItemId = a.Flight_Form_Section_Item_Id,
                                               Answer = a.Answer,
                                               FlightFormSectionId = b.Flight_Form_Section_Id,
                                               SectionTitle = c.SectionTitle,
                                               ItemTitle = b.ItemTitle
                                           }).ToList();
                            //qnaList = new List<SectionQuestionAnswer>();
                            foreach (var result in results)
                            {

                                qna = new SectionQuestionAnswer();
                                qna.Question = result.ItemTitle;
                                qna.Answer = result.Answer;
                                qnaList.Add(qna);

                            }
                        }
                        crewFltSectionObj.qnalist = qnaList;
                        CrewFlightFormSectionComment secCommnet = db.CrewFlightFormSectionComments.Where(c => c.Flight_Form_Section_Id == section.Id && c.Crew_Flight_Form_Id == crewObj.Id).FirstOrDefault();
                        if (secCommnet != null)
                        {
                            crewFltSectionObj.secComment = secCommnet.Comments;
                        }
                        CrewFltSectionObjList.Add(crewFltSectionObj);


                    }
                }
                crewFltForm.sections = CrewFltSectionObjList;
                TempData["TL_Id"] = crewObj.StaffNumber;
                if (Month == null)
                {

                    TempData["Block_Date"] = DateTime.Now;
                }
                else
                {

                    TempData["Block_Date"] = Month;
                }
                return View(crewFltForm);
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }

            CrewFltForm list = new CrewFltForm();
          
            return View(list);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public System.Web.Mvc.ActionResult OverallIndex(DateTime Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                return RedirectToAction("OverallIndex", "CrewFlightForm", new { Block_Date = Block_Date });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            TempData["Block_Date"] = Block_Date;

            return RedirectToAction("Index", "CrewFlightForm", new { Block_Date = Block_Date });
        }
       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> OSSSectorSearch(DateTime? From_Block_Date, DateTime? To_Block_Date, int? orginSector, int? destSector)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }
                if (orginSector == null || destSector == null)
                {
                    {
                        ViewBag.OriginAirportCode = db.AirportCodes
                                    .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                    .ToList();

                        ViewBag.DestinationAirportCode = db.AirportCodes
                                            .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                            .ToList();
                    }

                    (TempData["From_Block_Date"]) = DateTime.Now;
                    (TempData["To_Block_Date"]) = DateTime.Now;
                    SectorOSSDataPojo sectorOssData = new SectorOSSDataPojo();
                    return View(sectorOssData);
                }
                else if (orginSector != null && destSector != null)
                {
                    var orginAirport = db.AirportCodes.Where(a => a.Id == orginSector).Select(a => a.Code).FirstOrDefault();
                    var destinationAirport = db.AirportCodes.Where(a => a.Id == destSector).Select(a => a.Code).FirstOrDefault();

                    var flightDetails = db.OCB_Flights.Where(a => a.Orig == orginAirport && a.Dest == destinationAirport && DbFunctions.TruncateTime(a.Flight_Date) >= DbFunctions.TruncateTime(From_Block_Date)
                                         && DbFunctions.TruncateTime(a.Flight_Date) <= DbFunctions.TruncateTime(To_Block_Date)).Select(a => a.Id).ToList();

                    var crewFlightForm = db.CrewFlightForms.Where(a => flightDetails.Contains(a.Flight_Id)).ToList();

                    SectorOSSDataPojo sectorOssData = new SectorOSSDataPojo();

                    ViewBag.OriginAirportCode = db.AirportCodes
                                    .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                    .ToList();

                    ViewBag.DestinationAirportCode = db.AirportCodes
                                        .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                        .ToList();
                    (TempData["From_Block_Date"]) = From_Block_Date;
                    (TempData["To_Block_Date"]) = To_Block_Date;
                    sectorOssData.orginSectorId = orginSector;
                    sectorOssData.dstnSectorId = destSector;
                    sectorOssData.crFlightForm = crewFlightForm;
                    return View(sectorOssData);

                }
                else
                {
                    ViewBag.OriginAirportCode = db.AirportCodes
                                    .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                    .ToList();

                    ViewBag.DestinationAirportCode = db.AirportCodes
                                        .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                        .ToList();
                    (TempData["From_Block_Date"]) = DateTime.Now;
                    (TempData["To_Block_Date"]) = DateTime.Now;
                    SectorOSSDataPojo crewOssData = new SectorOSSDataPojo();
                    //List<CrewFlightForm> osslist = new List<CrewFlightForm>();
                    //crewOssData.crFlightForm=osslist;
                    return View(crewOssData);
                }
            }
            catch (Exception ex)
            {
                return View();
            }
        }
        [System.Web.Mvc.HttpPost]
       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public System.Web.Mvc.ActionResult SectorSearchPost(DateTime From_Block_Date, DateTime To_Block_Date, string orginSectorId, string dstnSectorId)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            var orginSector = orginSectorId;
            var destSector = dstnSectorId;

            try
            {

                return RedirectToAction("OSSSectorSearch", "CrewFlightForm", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date, orginSector = orginSector, destSector = destSector });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }



            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("SectorSearch", "Appraisal", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date, orginSector = orginSector, destSector = destSector });
        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CrewFlightFormInfoFromSector(int Id, DateTime From_Block_Date, DateTime To_Block_Date, int Origin, int Dest)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                CrewFlightForm crewObj = db.CrewFlightForms.Where(a => a.Id == Id).FirstOrDefault();
                CrewFltForm crewFltForm = new CrewFltForm();
                crewFltForm.crewFlt = crewObj;
                SectionQuestionAnswer qna;
                CrewFltSectionObj crewFltSectionObj;
                List<SectionQuestionAnswer> qnaList;
                List<CrewFltSectionObj> CrewFltSectionObjList = new List<CrewFltSectionObj>();

                if (crewObj != null)
                {

                    List<FlightFormSection> sections = db.FlightFormSections.Where(a => a.Flight_Form_Id == crewObj.Flight_Form_Id).ToList();

                    foreach (var section in sections)
                    {


                        crewFltSectionObj = new CrewFltSectionObj();
                        crewFltSectionObj.SectionId = section.Id;
                        crewFltSectionObj.SectionName = section.SectionTitle;
                        List<FlightFormSectionItem> sectionItemNums = db.FlightFormSectionItems.Where(a => a.Flight_Form_Section_Id == section.Id).ToList();
                        qnaList = new List<SectionQuestionAnswer>();
                        foreach (var sectionItem in sectionItemNums)
                        {

                            var results = (from a in db.CrewFlightFormAnswers
                                           join b in db.FlightFormSectionItems on a.Flight_Form_Section_Item_Id equals b.Id
                                           join c in db.FlightFormSections on b.Flight_Form_Section_Id equals c.Id
                                           where b.Id == sectionItem.Id && a.Crew_Flight_Form_Id == crewObj.Id
                                           select new
                                           {
                                               CrewFlightFormId = a.Crew_Flight_Form_Id,
                                               FlightFormSectionItemId = a.Flight_Form_Section_Item_Id,
                                               Answer = a.Answer,
                                               FlightFormSectionId = b.Flight_Form_Section_Id,
                                               SectionTitle = c.SectionTitle,
                                               ItemTitle = b.ItemTitle
                                           }).ToList();
                            //qnaList = new List<SectionQuestionAnswer>();
                            foreach (var result in results)
                            {

                                qna = new SectionQuestionAnswer();
                                qna.Question = result.ItemTitle;
                                qna.Answer = result.Answer;
                                qnaList.Add(qna);

                            }
                        }
                        crewFltSectionObj.qnalist = qnaList;
                        CrewFlightFormSectionComment secCommnet = db.CrewFlightFormSectionComments.Where(c => c.Flight_Form_Section_Id == section.Id && c.Crew_Flight_Form_Id == crewObj.Id).FirstOrDefault();
                        if (secCommnet != null)
                        {
                            crewFltSectionObj.secComment = secCommnet.Comments;
                        }
                        CrewFltSectionObjList.Add(crewFltSectionObj);


                    }
                }
                crewFltForm.sections = CrewFltSectionObjList;
                ViewBag.FromDate = From_Block_Date;
                ViewBag.ToDate = To_Block_Date;
                ViewBag.OriginValue = Origin;
                ViewBag.DestValue = Dest;
                return View(crewFltForm);
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }

            CrewFltForm list = new CrewFltForm();

            return View(list);

        }
    }
}