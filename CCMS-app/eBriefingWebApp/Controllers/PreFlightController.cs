
using eBriefingWebApp.Helper;
using eBriefingWebApp.Interfaces;
using eBriefingWebApp.Models;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace eBriefingWebApp.Controllers
{
    public class PreFlightController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public PreFlightController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();

        // GET: PreFlight
        //[Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> Index()
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            return View();
        }

       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> Questions()
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            var query = db.PreFlight_TestQuestions.Where(a => a.PreFlight_Categories != null).ToList();

            return View(query);
        }

       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewQuestion()
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            PreFlight_TestQuestions form = new PreFlight_TestQuestions();

            ViewBag.FleetsList = db.Fleets.ToList();

            return View(form);
        }

        [HttpPost]
        //[Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewQuestion(PreFlight_TestQuestions form)
        {
            var telemetry = new TelemetryClient();

            try
            {
                ViewBag.FleetsList = db.Fleets.ToList();

                if (form.CategoryId != null && form.Question != null)
                {
                    db.PreFlight_TestQuestions.Add(new PreFlight_TestQuestions
                    {
                        Active = true,
                        CategoryId = form.CategoryId,
                        Question = form.Question,
                    });

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Added";

                    return RedirectToAction("AddNewQuestion", "PreFlight");
                }
                else
                {
                    TempData["Status"] = "Missing";
                }

            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";
            }

            return View(form);
        }

       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditQuestion(int Id)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            var query = db.PreFlight_TestQuestions.Where(a => a.Id == Id).FirstOrDefault();

            return View(query);
        }

        [HttpPost]
       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditQuestion(PreFlight_TestQuestions form)
        {
            var telemetry = new TelemetryClient();

            try
            {
                if (form != null)
                {
                    var question = db.PreFlight_TestQuestions.Where(a => a.Id == form.Id).FirstOrDefault();

                    if (question != null)
                    {
                        question.Question = form.Question;

                        await db.SaveChangesAsync();

                        TempData["Status"] = "Updated";

                        return RedirectToAction("Questions", "PreFlight");
                    }
                    else
                    {
                        TempData["Status"] = "Not Found";
                    }
                }
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";
            }

            return View(form);
        }

       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> ManageAnswers(int Id)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            var question = db.PreFlight_TestQuestions.Where(a => a.Id == Id).FirstOrDefault();

            ViewBag.QuestionTitle = question.Question;
            ViewBag.QuestionId = question.Id;

            var query = db.PreFlight_Answers.Where(a => a.QuestionID == Id).ToList();

            return View(query);
        }

        [HttpGet]
       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public JsonResult GetCategoriesList(int FleetId)
        {
            var getList = (from cat in db.PreFlight_Categories
                           where cat.FleetId == FleetId && cat.Fleet != null
                           select new
                           {
                               cat.Id,
                               cat.Category
                           }).ToList();

            return Json(getList, JsonRequestBehavior.AllowGet);
        }

       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewAnswer(int Id)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            PreFlight_Answers ans = new PreFlight_Answers();

            var query = db.PreFlight_TestQuestions.Where(a => a.Id == Id).FirstOrDefault();
            ViewBag.Answer = ClsAnswer.GetAnswers();

            ans.QuestionID = Id;
            ViewBag.QuestionTitle = query.Question;

            return View(ans);
        }

        [HttpPost]
       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewAnswer(PreFlight_Answers form)
        {
            var telemetry = new TelemetryClient();

            try
            {
                db.PreFlight_Answers.Add(new PreFlight_Answers
                {
                    Answer = form.Answer,
                    AnswerText = form.AnswerText,
                    QuestionID = form.QuestionID
                });

                await db.SaveChangesAsync();

                var query = db.PreFlight_TestQuestions.Where(a => a.Id == form.QuestionID).FirstOrDefault();
                ViewBag.Answer = ClsAnswer.GetAnswers();

                ViewBag.QuestionTitle = query.Question;

                TempData["Status"] = "Added";

                return RedirectToAction("AddNewAnswer", "PreFlight", new { Id = query.Id });
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";
            }

            return View(form);
        }

       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditAnswer(int Id)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            ViewBag.Answer = ClsAnswer.GetAnswers();

            var query = db.PreFlight_Answers.Where(a => a.ID == Id).FirstOrDefault();

            return View(query);
        }

        [HttpPost]
       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditAnswer(PreFlight_Answers form)
        {
            var telemetry = new TelemetryClient();

            try
            {
                var query = db.PreFlight_Answers.Where(a => a.ID == form.ID).FirstOrDefault();

                if (query != null)
                {
                    query.AnswerText = form.AnswerText;
                    query.Answer = form.Answer;

                    await db.SaveChangesAsync();

                    ViewBag.Answer = ClsAnswer.GetAnswers();

                    TempData["Status"] = "Saved";

                    return RedirectToAction("ManageAnswers", "PreFlight", new { Id = query.QuestionID });
                }
                else
                {
                    TempData["Status"] = "Not Found";
                }

            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";
            }

            return View(form);
        }

      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> DeleteAnswer(int Id)
        {
            var telemetry = new TelemetryClient();

            try
            {
                var query = db.PreFlight_Answers.Where(a => a.ID == Id).FirstOrDefault();

                if (query != null)
                {
                    db.PreFlight_Answers.Remove(query);

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Deleted";

                    return RedirectToAction("ManageAnswers", "PreFlight", new { Id = query.QuestionID });
                }
                else
                {
                    TempData["Status"] = "Not Found";
                }

            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";

            }

            return View();
        }

      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> CrewTestHistory()
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            List<PreFlight_CrewTest> history = new List<PreFlight_CrewTest>();

            return View(history);
        }

        [HttpPost]
      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> CrewTestHistory(string StaffNumber)
        {
            var telemetry = new TelemetryClient();

            try
            {
                if (StaffNumber != null)
                {
                    var query = await db.PreFlight_CrewTest.Where(a => a.OCB_CrewPos.StaffNumber == StaffNumber).OrderByDescending(a => a.OCB_Flights.Flight_Date).ToListAsync();

                    ViewBag.CrewSummery = new ClsFlightSummery
                    {
                        TotalCorrect = db.PreFlight_TestLog.Where(a => a.PreFlight_CrewTest.OCB_CrewPos.StaffNumber == StaffNumber
                    && a.PreFlight_Answers.Answer == true).Count(),
                        TotalWrong = db.PreFlight_TestLog.Where(a => a.PreFlight_CrewTest.OCB_CrewPos.StaffNumber == StaffNumber
                        && a.PreFlight_Answers.Answer == false).Count(),
                        TotalPass = query.Where(a => a.IsFaildTest == false).Count(),
                        TotalFail = query.Where(a => a.IsFaildTest == true).Count(),
                        TotalFlights = query.Count()
                    };

                    return View(query);
                }
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";
            }

            List<PreFlight_CrewTest> history = new List<PreFlight_CrewTest>();

            return View(history);
        }

       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> CrewTestDetail(int Id)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            var query = await db.PreFlight_CrewTest.Where(a => a.Id == Id).FirstOrDefaultAsync();

            return View(query);

        }

      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> FlightTestHistory()
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            List<PreFlight_CrewTest> history = new List<PreFlight_CrewTest>();

            return View(history);
        }

        [HttpPost]
      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> FlightTestHistory(string FlightNumber)
        {
            var telemetry = new TelemetryClient();

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                if (FlightNumber != null)
                {
                    var query = await db.PreFlight_CrewTest.Where
                        (a => a.OCB_Flights.Flt_No == FlightNumber).OrderByDescending(a => a.OCB_Flights.Flight_Date).ToListAsync();

                    return View(query);
                }
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Data"] = "Error";
            }

            List<PreFlight_CrewTest> history = new List<PreFlight_CrewTest>();

            return View(history);
        }

      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> FlightTestDetail(int Id)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            var query = await db.OCB_Flights.Where
                    (a => a.Id == Id).FirstOrDefaultAsync();

            return View(query);
        }

      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AllCrewWithNoTest()
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            var testList = db.PreFlight_CrewTest.Where(a => DbFunctions.TruncateTime(a.TestDateTime) == DbFunctions.TruncateTime(DateTime.Now)).ToList();
            var crewList = db.OCB_CrewPos.Where(a => a.PosId != null && DbFunctions.TruncateTime(a.OCB_Blocks.Block_Date) == DbFunctions.TruncateTime(DateTime.Now)).ToList();
            var exList = db.PreFlight_TestExlude.Where(a => DbFunctions.TruncateTime(a.CommentDate) == DbFunctions.TruncateTime(DateTime.Now)).ToList();

            //Check Also Flight Id with test exlude
            var query = crewList.Where(a => !testList.Any(p2 => p2.CrewPosId == a.Id) &&
            !exList.Any(p2 => p2.CrewPosId == a.Id) || testList.Any(p2 => p2.CrewPosId == a.Id && p2.IsFaildTest == true)).Where(a => a.OCB_Blocks.Block_Date.Value.Date == DateTime.Now.Date).OrderBy(a => a.BlockId).ToList();

            TempData["Flight_Date"] = DateTime.Now.ToString("dd-MMM-yyyy");

            return View(query);
        }

        [HttpPost]
       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public ActionResult AllCrewWithNoTest(DateTime Flight_Date)
        {
            var testList = db.PreFlight_CrewTest.Where(a => DbFunctions.TruncateTime(a.OCB_Flights.Flight_Date) == DbFunctions.TruncateTime(Flight_Date)).ToList();
            var crewList = db.OCB_CrewPos.Where(a => a.PosId != null).ToList();
            var exList = db.PreFlight_TestExlude.ToList();

            //Check Also Flight Id with test exlude
            var query = crewList.Where(a => !testList.Any(p2 => p2.CrewPosId == a.Id) &&
            !exList.Any(p2 => p2.CrewPosId == a.Id) || testList.Any(p2 => p2.CrewPosId == a.Id && p2.IsFaildTest == true)).Where(a => a.OCB_Blocks.Block_Date.Value.Date == Flight_Date.Date).OrderBy(a => a.BlockId).ToList();

            TempData["Flight_Date"] = Flight_Date;

            return View(query);
        }

     //   [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddExceptionMessage(int Id, int FlightId)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            ViewBag.FlightId = FlightId;

            var query = db.OCB_CrewPos.Where(a => a.Id == Id).FirstOrDefault();

            return View(query);
        }

        [HttpPost]
      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddExceptionMessage(PreFlight_TestExlude form)
        {
            var telemetry = new TelemetryClient();

            try
            {
                var query = db.PreFlight_TestExlude.Where(a => a.CrewPosId == form.id).FirstOrDefault();

                if (query == null)
                {
                    db.PreFlight_TestExlude.Add(new PreFlight_TestExlude
                    {
                        CommentDate = DateTime.Now,
                        Comments = form.Comments,
                        CrewPosId = form.id,
                        FlightId = form.FlightId,
                    });

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Added";

                    return RedirectToAction("AllCrewWithNoTest", "PreFlight");
                }
                else
                {
                    TempData["Status"] = "Exist";
                }
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";
            }

            return View(form);
        }

     //   [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AllCrewFailedTest(DateTime? From_Block_Date, DateTime? To_Block_Date)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            List<PreFlight_CrewTest> list = new List<PreFlight_CrewTest>();
            if(From_Block_Date == null && To_Block_Date == null)
            {
                From_Block_Date = DateTime.Now.AddDays(-3);
                To_Block_Date = DateTime.Now;
            }

            var failedList = db.PreFlight_CrewTest.Where(a => a.IsFaildTest == true && a.AllowSecondChance == false && a.PreFlight_FailedComments.Count == 0 &&
            DbFunctions.TruncateTime(a.TestDateTime) >= DbFunctions.TruncateTime(From_Block_Date) && DbFunctions.TruncateTime(a.TestDateTime) <= DbFunctions.TruncateTime(To_Block_Date)).ToList();

            foreach (var item in failedList)
            {
                var comment = db.PreFlight_FailedComments.Where(a => a.CrewTestId == item.Id).FirstOrDefault();


                list.Add(new PreFlight_CrewTest
                {
                    AllowSecondChance = item.AllowSecondChance,
                    DidSecondChance = item.DidSecondChance,
                    FlightId = item.FlightId,
                    Id = item.Id,
                    IsFaildTest = item.IsFaildTest,
                    CrewPosId = item.CrewPosId,
                    TestDateTime = item.TestDateTime,
                    OCB_Flights = item.OCB_Flights,
                    //PreFlight_Comments = item.PreFlight_Comments,
                    //PreFlight_FailedComments = item.PreFlight_FailedComments,
                    PreFlight_TestLog = item.PreFlight_TestLog,
                    OCB_CrewPos = item.OCB_CrewPos
                });
            }
            TempData["From_Block_Date"] = From_Block_Date;
            TempData["To_Block_Date"] = To_Block_Date;
            return View(list);
        }

        [System.Web.Mvc.HttpPost]
        public System.Web.Mvc.ActionResult AllCrewFailedTest(DateTime From_Block_Date, DateTime To_Block_Date)
        {
            var telemetry = new TelemetryClient();

            try
            {
                return RedirectToAction("AllCrewFailedTest", "PreFlight", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";
            }

            TempData["From_Block_Date"] = From_Block_Date;
            TempData["To_Block_Date"] = To_Block_Date;

            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("AllCrewFailedTest", "PreFlight", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
        }


        //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public ActionResult AddFailedComment(int Id)
        {
            var query = db.PreFlight_CrewTest.Where(a => a.Id == Id).FirstOrDefault();

            return View(query);
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddFailedComment(PreFlight_FailedComments form)
        {
            var telemetry = new TelemetryClient();

            try
            {
                db.PreFlight_FailedComments.Add(new PreFlight_FailedComments
                {
                    CommentDate = DateTime.Now,
                    Comments = form.Comments,
                    CrewTestId = form.CrewTestId,
                });

                await db.SaveChangesAsync();

                TempData["Status"] = "Added";

                return RedirectToAction("AllCrewFailedTest", "PreFlight");
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";
            }

            return View(form);
        }

      // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddToSecondChance(int Id, int FlightId)
        {
            var telemetry = new TelemetryClient();

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = db.PreFlight_CrewTest.Where(a => a.Id == Id && a.FlightId == FlightId).FirstOrDefault();

                if (query != null)
                {
                    query.AllowSecondChance = true;

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Updated";
                }
                else
                {
                    var chkCrew = db.OCB_CrewPos.Where(a => a.Id == Id).FirstOrDefault();

                    if (chkCrew != null)
                    {
                        db.PreFlight_CrewTest.Add(new PreFlight_CrewTest
                        {
                            AllowSecondChance = true,
                            CrewPosId = chkCrew.Id,
                            DidSecondChance = false,
                            FlightId = FlightId,
                            IsFaildTest = false,
                            TestDateTime = chkCrew.OCB_Blocks.Block_Date
                        });

                        await db.SaveChangesAsync();

                        TempData["Status"] = "Updated";
                    }
                    else
                    {
                        TempData["Status"] = "Not Found";
                    }
                }
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";
            }

            return RedirectToAction("AllCrewFailedTest", "PreFlight");
        }

      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> TestSecondChance()
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            var query = await db.PreFlight_CrewTest.Where(a => (a.AllowSecondChance == true && a.DidSecondChance == false) || (a.DidSecondChance == true && DbFunctions.TruncateTime(a.TestDateTime) == DbFunctions.TruncateTime(DateTime.Now))).ToListAsync();

            return View(query);
        }

      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> CancelSecondChance(int Id)
        {
            var telemetry = new TelemetryClient();

            try
            {
                var query = db.PreFlight_CrewTest.Where(a => a.Id == Id).FirstOrDefault();

                if (query != null)
                {
                    query.AllowSecondChance = false;

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Updated";
                }
                else
                {
                    TempData["Status"] = "Not Found";
                }
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error";
            }

            return RedirectToAction("TestSecondChance", "PreFlight");
        }

      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> ExceptionReport()
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            List<PreFlight_TestExlude> list = new List<PreFlight_TestExlude>();

            return View(list);
        }

        [HttpPost]
      //  [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> ExceptionReport(DateTime FromDate, DateTime ToDate)
        {
            var query = await db.PreFlight_TestExlude.Where(a =>
            DbFunctions.TruncateTime(a.CommentDate) >= DbFunctions.TruncateTime(FromDate) &&
            DbFunctions.TruncateTime(a.CommentDate) <= DbFunctions.TruncateTime(ToDate)).ToListAsync();

            ViewBag.FromDate = FromDate.ToString("dd-MMM-yyyy");
            ViewBag.ToDate = ToDate.ToString("dd-MMM-yyyy");

            return View(query);
        }

        public  ActionResult DeactivateQtn(int QtnId)
        {
            var query = db.PreFlight_TestQuestions.Where(a => a.Id == QtnId).FirstOrDefault();

            if (query != null) // Important: Check if the question was found
            {
                query.Active = false;
                db.SaveChanges(); // Save the changes to the database
            }
            TempData["Status"] = "Deactivated";

            
            return RedirectToAction("Questions", "PreFlight");

            
        }

        public ActionResult ActivateQtn(int QtnId)
        {
            var query = db.PreFlight_TestQuestions.Where(a => a.Id == QtnId).FirstOrDefault();
            if (query != null) // Important: Check if the question was found
            {
                query.Active = true;
                db.SaveChanges(); // Save the changes to the database
            }


            return RedirectToAction("Questions", "PreFlight");


        
    }
    }
}