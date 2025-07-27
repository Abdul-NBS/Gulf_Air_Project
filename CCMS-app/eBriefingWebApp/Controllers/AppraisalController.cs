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
using System.Web.WebPages;
using Azure.Storage.Blobs.Models;
using System.Security.Cryptography;
using System.Web.UI.WebControls;


namespace eBriefingWebApp.Controllers
{
    public class AppraisalController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public AppraisalController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();

        // GET: Appraisal
       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
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
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
            }

            return View();
        }

      // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> CrewAppraisals(DateTime? From_Block_Date, DateTime? To_Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                List<CrewAppraisal> crewAppraisals = new List<CrewAppraisal>();
                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                if (From_Block_Date == null && To_Block_Date == null)
                {
                    crewAppraisals = db.CrewAppraisals.Where(a => DbFunctions.TruncateTime(a.SubmittedDateTime) == DbFunctions.TruncateTime(DateTime.Now)).ToList();
                    TempData["From_Block_Date"] = DateTime.Now;
                    TempData["To_Block_Date"] = DateTime.Now;
                    return View(crewAppraisals);
                }
                else
                {
                    DateTime? fromDate = From_Block_Date != null ? From_Block_Date : DateTime.Now;
                    DateTime? toDate = To_Block_Date != null ? To_Block_Date : DateTime.Now;
                    if (toDate >= fromDate)
                    {
                        crewAppraisals = db.CrewAppraisals.Where(a => DbFunctions.TruncateTime(a.SubmittedDateTime) >= DbFunctions.TruncateTime(fromDate)
                                         && DbFunctions.TruncateTime(a.SubmittedDateTime) <= DbFunctions.TruncateTime(toDate)).ToList();
                        TempData["From_Block_Date"] = fromDate;
                        TempData["To_Block_Date"] = toDate;
                        return View(crewAppraisals);
                    }
                    else
                    {
                        DateTime? newFromDate = toDate;
                        DateTime? newToDate = fromDate;
                        crewAppraisals = db.CrewAppraisals.Where(a => DbFunctions.TruncateTime(a.SubmittedDateTime) >= DbFunctions.TruncateTime(newFromDate)
                                         && DbFunctions.TruncateTime(a.SubmittedDateTime) <= DbFunctions.TruncateTime(newToDate)).ToList();
                        TempData["From_Block_Date"] = newFromDate;
                        TempData["To_Block_Date"] = newToDate;
                        return View(crewAppraisals);
                    }
                }

                //var query = await db.CrewAppraisals.ToListAsync();

               // return View(query);
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }

            Appraisal list = new Appraisal();

            return View(list);
        }

        // date by sort for Crew Appraisals
        [System.Web.Mvc.HttpPost]
        public System.Web.Mvc.ActionResult CrewAppraisals(DateTime From_Block_Date, DateTime To_Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                return RedirectToAction("CrewAppraisals", "Appraisal", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            TempData["From_Block_Date"] = From_Block_Date;
            TempData["To_Block_Date"] = To_Block_Date;

            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("CrewAppraisals", "Appraisal", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
        }










        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> ManageAppraisals()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.Appraisals.ToListAsync();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

                TempData["Status"] = "Error Data";
            }

            Appraisal list = new Appraisal();

            return View(list);
        }

       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewAppraisal()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            Appraisal form = new Appraisal();

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                form.Is_CSM = false;
                form.Is_Crew = false;
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
            }

            return View(form);
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewAppraisal(FormCollection form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                bool Is_CSM = Convert.ToBoolean(form["Is_CSM"] == null ? "False" : "True");
                bool Is_Crew = Convert.ToBoolean(form["Is_Crew"] == null ? "False" : "True");
                string Title = form["Title"];

                db.Appraisals.Add(new Appraisal
                {
                    Title = Title,
                    Is_Crew = Is_Crew,
                    Is_CSM = Is_CSM,
                    Is_Active = true,
                    guid = Guid.NewGuid().ToString(),
                    is_deleted = false,
                    Last_Update = DateTime.Now
                });

                await db.SaveChangesAsync();

                TempData["Status"] = "Added";
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

                TempData["Status"] = "Error";
            }

            return View();
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditAppraisal(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var getAppraisal = await db.Appraisals.Where(a => a.Id == Id).FirstOrDefaultAsync();

                return View(getAppraisal);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("ManageAppraisals", "Appraisal");
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditAppraisal(FormCollection form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                int Id = Convert.ToInt32(form["Id"]);
                bool Is_CSM = Convert.ToBoolean(form["Is_CSM"] == null ? "False" : "True");
                bool Is_Crew = Convert.ToBoolean(form["Is_Crew"] == null ? "False" : "True");
                string Title = form["Title"];

                var getAppraisal = await db.Appraisals.Where(a => a.Id == Id).FirstOrDefaultAsync();

                if(getAppraisal != null)
                {
                    getAppraisal.Is_Crew = Is_Crew;
                    getAppraisal.Is_CSM = Is_CSM;
                    getAppraisal.Title = Title;

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Saved";

                    return RedirectToAction("ManageAppraisals", "Appraisal");
                }
                else
                {
                    TempData["Status"] = "Not Found";
                    return View(getAppraisal);
                }
                
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("ManageAppraisals", "Appraisal");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> ManageSections(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.Appraisals.Where(a => a.Id == Id).FirstOrDefaultAsync();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("ManageAppraisals", "Appraisal");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewSection(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.Appraisals.Where(a => a.Id == Id).FirstOrDefaultAsync();

                AppraisalSection section = new AppraisalSection();
                section.Appraisal = new Appraisal();

                section.Appraisal.Title = query.Title;
                section.Appraisal.Id = query.Id;

                var GetOrderNum = query.AppraisalSections.OrderByDescending(a => a.OrderNumber).FirstOrDefault();

                if(GetOrderNum != null)
                {
                    section.OrderNumber = GetOrderNum.OrderNumber + 1;
                }

                return View(section);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("ManageAppraisals", "Appraisal");
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewSection(FormCollection form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                int AppraisalId = Convert.ToInt32(form["AppraisalId"]);
                bool HasComment = Convert.ToBoolean(form["HasComment"] == null ? "False" : "True");
                string Title = form["Title"];
                int OrderNumber = Convert.ToInt32(form["OrderNumber"]);

                db.AppraisalSections.Add(new AppraisalSection
                {
                    AppraisalId = AppraisalId,
                    HasComment = HasComment,
                    Title = Title,
                    OrderNumber = OrderNumber,
                    guid = Guid.NewGuid().ToString(),
                    is_deleted = false,
                    Last_Update = DateTime.Now
                });

                await db.SaveChangesAsync();

                TempData["Status"] = "Added";

                return RedirectToAction("AddNewSection", "Appraisal");
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("AddNewSection", "Appraisal");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditSection(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var section = await db.AppraisalSections.Where(a => a.Id == Id).FirstOrDefaultAsync();

                return View(section);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("ManageAppraisals", "Appraisal");
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditSection(FormCollection form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                int Id = Convert.ToInt32(form["Id"]);
                int AppraisalId = Convert.ToInt32(form["AppraisalId"]);
                bool HasComment = Convert.ToBoolean(form["HasComment"] == null ? "False" : "True");
                string Title = form["Title"];
                int OrderNumber = Convert.ToInt32(form["OrderNumber"]);

                var getSection = db.AppraisalSections.Where(a => a.Id == Id).FirstOrDefault();

                if (getSection != null)
                {
                    getSection.HasComment = HasComment;
                    getSection.Title = Title;
                    getSection.OrderNumber = OrderNumber;

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Saved";
                }
                else
                {
                    TempData["Status"] = "Not Found";
                }

                return RedirectToAction("ManageSections", "Appraisal");
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("ManageSections", "Appraisal");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> ManageQuestions(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.AppraisalSections.Where(a => a.Id == Id).FirstOrDefaultAsync();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("ManageQuestions", "Appraisal");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddQuestion(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var query = await db.AppraisalSections.Where(a => a.Id == Id).FirstOrDefaultAsync();

                AppraisalSectionQuestion question = new AppraisalSectionQuestion();
                question.AppraisalSection = new AppraisalSection();

                question.AppraisalSection.Title = query.Title;
                question.AppraisalSection.Id = query.Id;

                var GetOrderNum = query.AppraisalSectionQuestions.OrderByDescending(a => a.OrderNumber).FirstOrDefault();

                if (GetOrderNum != null)
                {
                    question.OrderNumber = GetOrderNum.OrderNumber + 1;
                }

                return View(question);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("ManageSections", "Appraisal");
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddQuestion(FormCollection form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                int SectionId = Convert.ToInt32(form["SectionId"]);
                bool HasComment = Convert.ToBoolean(form["HasComment"] == null ? "False" : "True");
                string Title = form["Title"];
                int OrderNumber = Convert.ToInt32(form["OrderNumber"]);

                db.AppraisalSectionQuestions.Add(new AppraisalSectionQuestion
                {
                    Question = Title,
                    AppraisalSectionId = SectionId,
                    HasComment = HasComment,
                    OrderNumber = OrderNumber,
                    guid = Guid.NewGuid().ToString(),
                    is_deleted = false,
                    Last_Update = DateTime.Now
                });

                await db.SaveChangesAsync();

                TempData["Status"] = "Added";

                return RedirectToAction("AddQuestion", "Appraisal");
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("AddQuestion", "Appraisal");
        }

       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditQuestion(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var question = await db.AppraisalSectionQuestions.Where(a => a.Id == Id).FirstOrDefaultAsync();

                return View(question);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("ManageQuestions", "Appraisal");
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> EditQuestion(FormCollection form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                int Id = Convert.ToInt32(form["Id"]);
                bool HasComment = Convert.ToBoolean(form["HasComment"] == null ? "False" : "True");
                string Title = form["Question"];
                int OrderNumber = Convert.ToInt32(form["OrderNumber"]);

                var getQuestion = db.AppraisalSectionQuestions.Where(a => a.Id == Id).FirstOrDefault();

                if (getQuestion != null)
                {
                    getQuestion.HasComment = HasComment;
                    getQuestion.Title = Title;
                    getQuestion.OrderNumber = OrderNumber;

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Saved";
                }
                else
                {
                    TempData["Status"] = "Not Found";
                }

                return RedirectToAction("ManageQuestions", "Appraisal");
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("ManageQuestions", "Appraisal");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> ManageAnswers(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.AppraisalSectionQuestions.Where(a => a.Id == Id).FirstOrDefaultAsync();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("ManageQuestions", "Appraisal");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewAnswer(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.AppraisalSectionQuestions.Where(a => a.Id == Id).FirstOrDefaultAsync();

                AppraisalQuestionAnswer answer = new AppraisalQuestionAnswer();
                answer.AppraisalSectionQuestion = new AppraisalSectionQuestion();

                answer.AppraisalSectionQuestion.Title = query.Title;
                answer.AppraisalSectionQuestion.Id = query.Id;

                var GetOrderNum = query.AppraisalQuestionAnswers.OrderByDescending(a => a.OrderNum).FirstOrDefault();

                if (GetOrderNum != null)
                {
                    answer.OrderNum = GetOrderNum.OrderNum + 1;
                }

                return View(answer);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("ManageQuestions", "Appraisal");
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> AddNewAnswer(FormCollection form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                int QuestionId = Convert.ToInt32(form["QuestionId"]);
                string Answer = form["Answer"];
                string Description = form["Description"];
                int OrderNum = Convert.ToInt32(form["OrderNum"]);

                db.AppraisalQuestionAnswers.Add(new AppraisalQuestionAnswer
                {
                    Answer = Answer,
                    Description = Description,
                    QuestionId = QuestionId,
                    OrderNum = OrderNum,
                    guid = Guid.NewGuid().ToString(),
                    is_deleted = false,
                    Last_Update = DateTime.Now
                });

                await db.SaveChangesAsync();

                TempData["Status"] = "Added";

                return RedirectToAction("AddNewAnswer", "Appraisal");
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("AddNewAnswer", "Appraisal");
        }
       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CrewAppraisalInfo(int Id , DateTime From_Block_Date , DateTime To_Block_Date )
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }
               
                CrewAppraisal crewObj = db.CrewAppraisals.Where(a => a.Id == Id).FirstOrDefault();
               
                AppraisalInfoObj aprlInfo = new AppraisalInfoObj();
                aprlInfo.crewAppraisal = crewObj;
           
                var result = from e in db.CrewAppraisalEvals
                             join q in db.AppraisalSectionQuestions on e.QuestionId equals q.Id
                             join s in db.AppraisalSections on q.AppraisalSectionId equals s.Id
                             where e.CrewAppraisalId == crewObj.Id
                             select new
                             {
                                 crewAppraisalId = e.CrewAppraisalId,
                                 Comments = e.Comments,
                                 QuestionAnswerId = e.AnswerId,
                                 SectionId = e.SectionId,
                                 Question = q.Question,
                                 Title = s.Title
                             };
                CrewEvalScoreInfo scores;
                List<CrewEvalScoreInfo> evalList = new List<CrewEvalScoreInfo>();
                foreach (var item in result)
                {
                    scores = new CrewEvalScoreInfo();
                    scores.qnAnswerId = item.QuestionAnswerId;
                    scores.Question = item.Question;
                    scores.comments = item.Comments;
                    scores.sectionId = (int)item.SectionId;

                    AppraisalQuestionAnswer qnAnswer = db.AppraisalQuestionAnswers.Where(a => a.Id == scores.qnAnswerId).FirstOrDefault();
                    scores.scorePoint = qnAnswer.Score;
                    scores.scoreDesc = qnAnswer.Answer;
                    
                    evalList.Add(scores);
                    
                }
                aprlInfo.CrewEvalScorelist = evalList;
                TempData["From_Block_Date"] = From_Block_Date;
                TempData["To_Block_Date"] = To_Block_Date;
                return View(aprlInfo);
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }

            Appraisal list = new Appraisal();

            return View(list);
        
    }
       // [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> PrintAppraisalInfo(int Id)
        {
            
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                CrewAppraisal crewObj = db.CrewAppraisals.Where(a => a.Id == Id).FirstOrDefault();
                
                AppraisalInfoObj aprlInfo = new AppraisalInfoObj();
                aprlInfo.crewAppraisal = crewObj;

                var result = from e in db.CrewAppraisalEvals
                             join q in db.AppraisalSectionQuestions on e.QuestionId equals q.Id
                             join s in db.AppraisalSections on q.AppraisalSectionId equals s.Id
                             where e.CrewAppraisalId == crewObj.Id
                             select new
                             {
                                 crewAppraisalId = e.CrewAppraisalId,
                                 Comments = e.Comments,
                                 QuestionAnswerId = e.AnswerId,
                                 SectionId = e.SectionId,
                                 Question = q.Question,
                                 Title = s.Title
                             };
                CrewEvalScoreInfo scores;
                List<CrewEvalScoreInfo> evalList = new List<CrewEvalScoreInfo>();
                foreach (var item in result)
                {
                    scores = new CrewEvalScoreInfo();
                    scores.qnAnswerId = item.QuestionAnswerId;
                    scores.Question = item.Question;
                    scores.comments = item.Comments;
                    scores.sectionId = (int)item.SectionId;

                    AppraisalQuestionAnswer qnAnswer = db.AppraisalQuestionAnswers.Where(a => a.Id == scores.qnAnswerId).FirstOrDefault();
                    scores.scorePoint = qnAnswer.Score;
                    scores.scoreDesc = qnAnswer.Answer;

                    
                    evalList.Add(scores);

                }
                aprlInfo.CrewEvalScorelist = evalList;
                return View(aprlInfo);
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }

            Appraisal list = new Appraisal();

            return View(list);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public System.Web.Mvc.ActionResult CrewAppraisalsList(DateTime Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                return RedirectToAction("CrewAppraisalsList", "Appraisal", new { Block_Date = Block_Date });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            TempData["Block_Date"] = Block_Date;

            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("Index", "OCB", new { Block_Date = Block_Date });
        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> ViewCrewData(String Id, DateTime? Month)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

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
                    currentMonth = Month.Value.Month;
                    currentYear = Month.Value.Year;
                    TempData["Block_Date"] = Month;
                }
                var appraisals = db.CrewAppraisals.Where(ca => db.OCB_CrewPos.Any(cp => cp.StaffNumber == Id && cp.Id == ca.CrewPosId) && ca.SubmittedDateTime.HasValue
                                 && ca.SubmittedDateTime.Value.Month == currentMonth && ca.SubmittedDateTime.Value.Year == currentYear).ToList();
                CrewAppraisalData crewAppraisalData = new CrewAppraisalData();
                List<AppraisalPojo> pojoList = new List<AppraisalPojo>();
                AppraisalPojo appraisalPojo;
                var CrewDetail = db.PilotCrewDetails.Where(b => b.StaffNumber == Id).FirstOrDefault();
                crewAppraisalData.StaffNo = CrewDetail.StaffNumber;
                crewAppraisalData.StaffName = CrewDetail.FullName;
                //foreach(var app in appraisals)
                for (int i = 0; i < appraisals.Count; i++)
                {
                    appraisalPojo = new AppraisalPojo();
                    appraisalPojo.Id = appraisals[i].Id;
                    var AppraiserId = appraisals[i].AppraiserStaffNumber;
                    var AppraiserDetails = db.PilotCrewDetails.Where(a=>a.StaffNumber== AppraiserId).FirstOrDefault();
                    if (AppraiserDetails != null)
                    {
                        appraisalPojo.appraiserId = AppraiserDetails.StaffNumber;
                        appraisalPojo.appraiserName = AppraiserDetails.FullName;
                    }
                    int? blockNo = appraisals[i].BlockId;
                    var blockDetails = db.OCB_Blocks.Where(a => a.Id == blockNo).FirstOrDefault();
                    if (blockDetails != null)
                    {
                        appraisalPojo.BlockNo = blockDetails.Block_No;
                    }
                    var appid = appraisals[i].Id;
                    var evals = db.CrewAppraisalEvals.Where(a => a.CrewAppraisalId == appid).ToList();
                    int qtnCount = evals.Count();
                    decimal totalCrewScore = 0;
                    foreach (var evalu in evals)
                    {
                        var scoreValue = db.AppraisalQuestionAnswers.Where(a => a.Id == evalu.AnswerId).Select(a => a.Score).FirstOrDefault();
                        totalCrewScore += scoreValue.Value;
                    }
                    decimal overAllPersonalAverage = totalCrewScore / qtnCount;
                    //totalCrewScore += overAllPersonalAverage;
                    decimal roundedDecimal1 = Math.Round(overAllPersonalAverage, 2);
                    appraisalPojo.TotalScore = roundedDecimal1;
                    appraisalPojo.submitteddate = appraisals[i].SubmittedDateTime;
                    pojoList.Add(appraisalPojo);
                }
                crewAppraisalData.pojoAppraisals = pojoList;
                int blockCount = db.CrewAppraisals.Where(ca => db.OCB_CrewPos.Any(cp => cp.StaffNumber == Id && cp.Id == ca.CrewPosId) && ca.SubmittedDateTime.HasValue
                                 && ca.SubmittedDateTime.Value.Month == currentMonth && ca.SubmittedDateTime.Value.Year == currentYear).Select(a => a.BlockId).Distinct().ToList().Count;

                crewAppraisalData.totalFlight = blockCount;
                return View(crewAppraisalData);
            }catch(Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }
            return View();
        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult>  CrewAppraisalsList(DateTime? Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }
            }catch(Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }
           
                int currentMonth;
                int currentYear;
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
                int blockCount = db.CrewAppraisals.Where(a => a.SubmittedDateTime.HasValue
                                 && a.SubmittedDateTime.Value.Month == currentMonth && a.SubmittedDateTime.Value.Year == currentYear).Select(a => a.BlockId).Distinct().ToList().Count;

            int oabCount = db.CrewAppraisals.Where(a => a.SubmittedDateTime.HasValue
                                && a.SubmittedDateTime.Value.Month == currentMonth && a.SubmittedDateTime.Value.Year == currentYear).ToList().Count;


            var averageScore = (from ca in db.CrewAppraisals
                                    join cae in db.CrewAppraisalEvals on ca.Id equals cae.CrewAppraisalId
                                    join aqa in db.AppraisalQuestionAnswers on cae.AnswerId equals aqa.Id
                                    select aqa.Score).Average();
                double? overAllAverage = 0;
                overAllAverage = averageScore;


                var averageMonthlyScore = (from ca in db.CrewAppraisals
                                           join cae in db.CrewAppraisalEvals on ca.Id equals cae.CrewAppraisalId
                                           join aqa in db.AppraisalQuestionAnswers on cae.AnswerId equals aqa.Id
                                           where ca.SubmittedDateTime.Value.Month == currentMonth &&
                                           ca.SubmittedDateTime.Value.Year == currentYear
                                           select aqa.Score).Average();
                double? overAllMonthlyAverage = 0;
                overAllMonthlyAverage = averageMonthlyScore;

                //get Staffwise Data

                /*var staffappraisals = db.CrewAppraisals.Where(a => a.SubmittedDateTime.HasValue
                                 && a.SubmittedDateTime.Value.Month == currentMonth && a.SubmittedDateTime.Value.Year == currentYear).ToList();


                List<string> distinctStaffNumbers = staffappraisals.Select(stffDetail => db.OCB_CrewPos.Where(a => a.Id == stffDetail.CrewPosId).Select(a => a.StaffNumber).FirstOrDefault())
                                                      .Distinct()
                                                      .ToList();
                List<MonthlyCrewAppraisalData> monthlyCrewdataList = new List<MonthlyCrewAppraisalData>();
                MonthlyCrewAppraisalData monthlyCrewdata;
                for (int i = 0; i < distinctStaffNumbers.Count; i++)
                {
                    monthlyCrewdata = new MonthlyCrewAppraisalData();
                    String staffNo = distinctStaffNumbers[i];
                    var StaffName = db.PilotCrewDetails.Where(a => a.StaffNumber == staffNo).Select(a => a.FullName).FirstOrDefault();
                    monthlyCrewdata.StaffName = StaffName;
                    monthlyCrewdata.StaffNo = distinctStaffNumbers[i];

                    var posList = db.OCB_CrewPos.Where(a => a.StaffNumber == staffNo).ToList();

                    var crewAppraisals = db.CrewAppraisals.Where(ca => db.OCB_CrewPos.Any(ocb => ocb.StaffNumber == staffNo && ocb.Id == ca.CrewPosId)).ToList();

                    int blkCount = 0;
                    if (crewAppraisals != null)
                    {
                        blkCount = crewAppraisals.Select(a => a.BlockId).Distinct().ToList().Count();
                    }
                
                 

                    monthlyCrewdata.totalFlight = blkCount;
                    var CrewappraisalCount = crewAppraisals.Count();
                    double scoreCrewCount = 0;
                    double overAllPersonalAverage = 0;
                    //foreach (var crewApp in appraislList)
                    for (int k = 0; k < crewAppraisals.Count; k++)
                    {
                        var appId = crewAppraisals[k].Id;
                        var evaluations = db.CrewAppraisalEvals.Where(a => a.CrewAppraisalId == appId).ToList();
                        int qtnCount = evaluations.Count();
                        int totalCrewScore = 0;
                        //foreach (var evalu in evaluations)
                        if (evaluations != null && qtnCount > 0)
                        {

                            double averageCrewScore = evaluations.Select(e => db.AppraisalQuestionAnswers
                                                      .Where(a => a.Id == e.AnswerId)
                                                      .Select(a => a.Score)
                                                      .FirstOrDefault())
                                     .Average(score => score ?? 0); // Handle null scores

                            // Update overall average
                            overAllPersonalAverage = averageCrewScore;
                        }
                        // overAllPersonalAverage = totalCrewScore / qtnCount;
                        scoreCrewCount += overAllPersonalAverage;

                    }
                    double overAllScoreAverage = 0;
                    if (scoreCrewCount != 0)
                    {
                        overAllScoreAverage = scoreCrewCount / CrewappraisalCount;
                    }
                    double overAllScoreAveragerounded = 0;
                    if (overAllScoreAverage != null && overAllScoreAverage != 0)
                    {
                        overAllScoreAveragerounded = Math.Round((double)overAllScoreAverage, 2);
                    }
                    monthlyCrewdata.monthlyScoreAverage = overAllScoreAveragerounded;
                    monthlyCrewdataList.Add(monthlyCrewdata);

                }*/

                MonthlyAppraisalData monthlyAppraisalData = new MonthlyAppraisalData();
                monthlyAppraisalData.totalFlight = blockCount;
            monthlyAppraisalData.OABCount = oabCount;
                double overAllAveragerounded = 0;
                double monthlyAverageAveragerounded = 0;
            double overAllAverageroundedPercent = 0;
            double monthlyAverageAverageroundedPercent = 0;
                if (overAllAverage != null && overAllAverage != 0)
                {
                    overAllAveragerounded = Math.Round((double)overAllAverage, 2);
                overAllAverageroundedPercent = overAllAveragerounded / 5 * 100;
                }
                if (overAllMonthlyAverage != null && overAllMonthlyAverage != 0)
                {
                    monthlyAverageAveragerounded = Math.Round((double)overAllMonthlyAverage, 2);
                monthlyAverageAverageroundedPercent = monthlyAverageAveragerounded / 5 * 100;
            }
                //get TL wise data

                var TLList = db.Appraisal_Team_Leads.ToList();
            List<MonthlyLeadAppraisalData> monthlyLeadAppraisalDatas = new List<MonthlyLeadAppraisalData>();
                MonthlyLeadAppraisalData monthlyLeadAppraisalData;
                for (int t = 0; t < TLList.Count; t++)
                {
                monthlyLeadAppraisalData = new MonthlyLeadAppraisalData();
                monthlyLeadAppraisalData.Id = TLList[t].StaffNumber;
                var staffNo = TLList[t].StaffNumber;
                var StaffInfo = db.CrewDetails.Where(a=>a.StaffNumber == staffNo).FirstOrDefault();
                if (StaffInfo != null)
                {
                    monthlyLeadAppraisalData.StaffName = StaffInfo.FullName;
                }

                int obaCount = db.CrewAppraisals.Where(a => a.AppraiserStaffNumber == staffNo && a.SubmittedDateTime.HasValue
                                && a.SubmittedDateTime.Value.Month == currentMonth && a.SubmittedDateTime.Value.Year == currentYear).ToList().Count;
                monthlyLeadAppraisalData.count= obaCount;

                int fltCount = db.CrewAppraisals.Where(ca => ca.AppraiserStaffNumber == staffNo
                                 && ca.SubmittedDateTime.Value.Month == currentMonth && ca.SubmittedDateTime.Value.Year == currentYear).
                                 Select(a => a.BlockId).Distinct().ToList().Count;
                monthlyLeadAppraisalData.flightCount = fltCount;
                monthlyLeadAppraisalDatas.Add(monthlyLeadAppraisalData);

            }       

                monthlyAppraisalData.overallAverage = overAllAverageroundedPercent;
                monthlyAppraisalData.monthlyAverage = monthlyAverageAverageroundedPercent;
                //monthlyAppraisalData.monthlyCrewAppraisalDatas = monthlyCrewdataList;
            monthlyAppraisalData.monthlyLeadAppraisalDatas = monthlyLeadAppraisalDatas;
                monthlyAppraisalData.year = currentYear;
                monthlyAppraisalData.month = currentMonth;


                return View(monthlyAppraisalData);
            }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> ViewAppraisalData(int Id,DateTime Month)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                CrewAppraisal crewObj = db.CrewAppraisals.Where(a => a.Id == Id).FirstOrDefault();

                AppraisalInfoObj aprlInfo = new AppraisalInfoObj();
                aprlInfo.crewAppraisal = crewObj;

                var result = from e in db.CrewAppraisalEvals
                             join q in db.AppraisalSectionQuestions on e.QuestionId equals q.Id
                             join s in db.AppraisalSections on q.AppraisalSectionId equals s.Id
                             where e.CrewAppraisalId == crewObj.Id
                             select new
                             {
                                 crewAppraisalId = e.CrewAppraisalId,
                                 Comments = e.Comments,
                                 QuestionAnswerId = e.AnswerId,
                                 SectionId = e.SectionId,
                                 Question = q.Question,
                                 Title = s.Title
                             };
                CrewEvalScoreInfo scores;
                List<CrewEvalScoreInfo> evalList = new List<CrewEvalScoreInfo>();
                foreach (var item in result)
                {
                    scores = new CrewEvalScoreInfo();
                    scores.qnAnswerId = item.QuestionAnswerId;
                    scores.Question = item.Question;
                    scores.comments = item.Comments;
                    scores.sectionId = (int)item.SectionId;

                    AppraisalQuestionAnswer qnAnswer = db.AppraisalQuestionAnswers.Where(a => a.Id == scores.qnAnswerId).FirstOrDefault();
                    scores.scorePoint = qnAnswer.Score;
                    scores.scoreDesc = qnAnswer.Answer;

                    evalList.Add(scores);

                }
                aprlInfo.CrewEvalScorelist = evalList;
                if (Month == null)
                {
                                     
                    TempData["Block_Date"] = DateTime.Now;
                }
                else
                {
                    
                    TempData["Block_Date"] = Month;
                }
                return View(aprlInfo);
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
        public async Task<System.Web.Mvc.ActionResult> ViewLeadData(String Id, DateTime? Month)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

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
                    currentMonth = Month.Value.Month;
                    currentYear = Month.Value.Year;
                    TempData["Block_Date"] = Month;
                }
                var appraisals = db.CrewAppraisals.Where(ca =>ca.AppraiserStaffNumber == Id  && ca.SubmittedDateTime.HasValue
                                 && ca.SubmittedDateTime.Value.Month == currentMonth && ca.SubmittedDateTime.Value.Year == currentYear).ToList();
                CrewAppraisalData crewAppraisalData = new CrewAppraisalData();
                List<AppraisalPojo> pojoList = new List<AppraisalPojo>();
                AppraisalPojo appraisalPojo;
                var CrewDetail = db.PilotCrewDetails.Where(b => b.StaffNumber == Id).FirstOrDefault();
                crewAppraisalData.StaffNo = CrewDetail.StaffNumber;
                crewAppraisalData.StaffName = CrewDetail.FullName;
                //foreach(var app in appraisals)
                for (int i = 0; i < appraisals.Count; i++)
                {
                    appraisalPojo = new AppraisalPojo();
                    appraisalPojo.Id = appraisals[i].Id;
                    var AppraiserId = appraisals[i].AppraiserStaffNumber;
                    var AppraiserDetails = db.CrewDetails.Where(a => a.StaffNumber == AppraiserId).FirstOrDefault();
                    if (AppraiserDetails != null)
                    {
                        appraisalPojo.appraiserId = AppraiserDetails.StaffNumber;
                        appraisalPojo.appraiserName = AppraiserDetails.FullName;
                    }
                    int? blockNo = appraisals[i].BlockId;
                    var blockDetails = db.OCB_Blocks.Where(a => a.Id == blockNo).FirstOrDefault();
                    if (blockDetails != null)
                    {
                        appraisalPojo.BlockNo = blockDetails.Block_No;
                    }
                    var appid = appraisals[i].Id;
                    var evals = db.CrewAppraisalEvals.Where(a => a.CrewAppraisalId == appid).ToList();
                    int qtnCount = evals.Count();
                    decimal totalCrewScore = 0;
                    foreach (var evalu in evals)
                    {
                        var scoreValue = db.AppraisalQuestionAnswers.Where(a => a.Id == evalu.AnswerId).Select(a => a.Score).FirstOrDefault();
                        totalCrewScore += scoreValue.Value;
                    }
                    decimal overAllPersonalAverage = totalCrewScore / qtnCount;
                    //totalCrewScore += overAllPersonalAverage;
                    decimal roundedDecimal1 = Math.Round(overAllPersonalAverage, 2);
                    appraisalPojo.TotalScore = roundedDecimal1;
                    appraisalPojo.submitteddate = appraisals[i].SubmittedDateTime;

                    //apraisee details 
                    var apraiseeposId = appraisals[i].CrewPosId;
                    var crewStaff = db.OCB_CrewPos.Where(a => a.Id == apraiseeposId).FirstOrDefault();  
                    var StaffNo = crewStaff.StaffNumber;
                    var StaffInfo = db.CrewDetails.Where(a => a.StaffNumber == StaffNo).FirstOrDefault();
                    appraisalPojo.appraiseeStaffNo = StaffNo;
                    appraisalPojo.appraiseeStaffName = StaffInfo.FullName;
                    pojoList.Add(appraisalPojo);
                }
                crewAppraisalData.pojoAppraisals = pojoList;
                int blockCount = db.CrewAppraisals.Where(ca => ca.AppraiserStaffNumber == Id
                                 && ca.SubmittedDateTime.Value.Month == currentMonth && ca.SubmittedDateTime.Value.Year == currentYear).Select(a => a.BlockId).Distinct().ToList().Count;

                crewAppraisalData.totalFlight = blockCount;
                return View(crewAppraisalData);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }
            return View();
        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> SectorSearch(DateTime? From_Block_Date, DateTime? To_Block_Date,int? orginSector , int? destSector,int? staffNo)
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
                    ViewBag.OriginAirportCode = db.AirportCodes
                                      .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                      .ToList();

                    ViewBag.DestinationAirportCode = db.AirportCodes
                                        .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                        .ToList();
                    ViewBag.TLlist = db.Appraisal_Team_Leads
                                       .Select(tl => new { ID = tl.ID, StaffNo = tl.StaffData })
                                       .ToList();
                    (TempData["From_Block_Date"]) = DateTime.Now;
                    (TempData["To_Block_Date"]) = DateTime.Now;
                    SectorAppDataPojo sectorAppDataPojo = new SectorAppDataPojo();
                    return View(sectorAppDataPojo);
                }
                else if (orginSector != null && destSector != null)
                {
                    var orginAirport = db.AirportCodes.Where(a => a.Id == orginSector).Select(a => a.Code).FirstOrDefault();
                    var destinationAirport = db.AirportCodes.Where(a => a.Id == destSector).Select(a => a.Code).FirstOrDefault();

                    var flightDetails = db.OCB_Flights.Where(a => a.Orig == orginAirport && a.Dest == destinationAirport && DbFunctions.TruncateTime(a.Flight_Date) >= DbFunctions.TruncateTime(From_Block_Date)
                                         && DbFunctions.TruncateTime(a.Flight_Date) <= DbFunctions.TruncateTime(To_Block_Date)).Select(a => a.BlockId).ToList();

                    var crAppraisals = db.CrewAppraisals.Where(block => flightDetails.Contains(block.BlockId)).ToList();
                    int flightList = 0;
                    int oabCount = 0;
                    CrewAppraisalDataPojo appraisalPojo;
                    List<CrewAppraisalDataPojo> crewAppraisalDataPojos = new List<CrewAppraisalDataPojo>();
                    if (crAppraisals != null)
                    {
                        if (staffNo != 0)
                    {
                        String StaffNum = staffNo.ToString();
                        var crewdb = db.Appraisal_Team_Leads.Where(a=>a.ID == staffNo).Select(a => a.StaffNumber).FirstOrDefault();
                        if (crewdb != null) // Check if staffNumberSelected has a value
                        {
                            crAppraisals = crAppraisals.Where(appraisal => appraisal.AppraiserStaffNumber == crewdb).ToList(); // Assuming StaffNumber is a property
                        }
                    }
                 
                   
                    
                    
                         flightList = crAppraisals.Select(a=>a.BlockId).Distinct().Count();
                         oabCount = crAppraisals.Count();

                      
                        for (int i = 0; i < crAppraisals.Count; i++)
                        {
                            appraisalPojo = new CrewAppraisalDataPojo();
                            appraisalPojo.Id = crAppraisals[i].Id;
                            var AppraiserId = crAppraisals[i].AppraiserStaffNumber;
                            var AppraiserDetails = db.CrewDetails.Where(a => a.StaffNumber == AppraiserId).FirstOrDefault();
                            if (AppraiserDetails != null)
                            {
                                appraisalPojo.appraiserName = AppraiserDetails.StaffNumber;
                                appraisalPojo.appraiserName = AppraiserDetails.FullName;
                            }
                            var ocbPos = crAppraisals[i].CrewPosId;
                            var posDetail = db.OCB_CrewPos.Where(a => a.Id == ocbPos).Select(a=>a.StaffNumber).FirstOrDefault();
                            appraisalPojo.crewStaffNo = posDetail;
                            if(posDetail!=null){
                                var crewName = db.CrewDetails.Where(a=>a.StaffNumber == posDetail).FirstOrDefault();
                                appraisalPojo.crewStaffName = crewName.FullName;
                            }
                            int? blockNo = crAppraisals[i].BlockId;
                            var blockDetails = db.OCB_Blocks.Where(a => a.Id == blockNo).FirstOrDefault();
                            if (blockDetails != null)
                            {
                                appraisalPojo.BlockName = blockDetails.Block_No;
                            }
                            var appid = crAppraisals[i].Id;
                            var evals = db.CrewAppraisalEvals.Where(a => a.CrewAppraisalId == appid).ToList();
                            int qtnCount = evals.Count();
                            decimal totalCrewScore = 0;
                            foreach (var evalu in evals)
                            {
                                var scoreValue = db.AppraisalQuestionAnswers.Where(a => a.Id == evalu.AnswerId).Select(a => a.Score).FirstOrDefault();
                                totalCrewScore += scoreValue.Value;
                            }
                            decimal overAllPersonalAverage = totalCrewScore / qtnCount;
                            //totalCrewScore += overAllPersonalAverage;
                            decimal roundedDecimal1 = Math.Round(overAllPersonalAverage, 2);
                            decimal overallPersonalPercent = 0;
                            if (roundedDecimal1 != null && roundedDecimal1 != 0)
                            {
                                overallPersonalPercent = Math.Round(roundedDecimal1 / 5 * 100, 2);
                            }
                            appraisalPojo.TotalScore = overallPersonalPercent;
                            appraisalPojo.submittedDate = crAppraisals[i].SubmittedDateTime;
                            crewAppraisalDataPojos.Add(appraisalPojo);
                        }
                    }
                    SectorAppDataPojo sectorPojo = new SectorAppDataPojo();
                    sectorPojo.orginSectorId = orginSector;
                    sectorPojo.dstnSectorId = destSector;
                    sectorPojo.crewAppraisalDatas = crewAppraisalDataPojos;
                    sectorPojo.totalFlight = flightList;
                    sectorPojo.totalOAB = oabCount;

                var averageScore = (from ca in db.CrewAppraisals
                                                           join cae in db.CrewAppraisalEvals on ca.Id equals cae.CrewAppraisalId
                                                           join aqa in db.AppraisalQuestionAnswers on cae.AnswerId equals aqa.Id
                                                           where flightDetails.Contains(ca.BlockId) // The crucial addition
                                                           select aqa.Score).Average();
                    (TempData["From_Block_Date"]) = From_Block_Date;
                    (TempData["To_Block_Date"]) = To_Block_Date;
                    ViewBag.OriginAirportCode = db.AirportCodes
                                     .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                     .ToList();

                    ViewBag.DestinationAirportCode = db.AirportCodes
                                        .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                        .ToList();
                    ViewBag.TLlist = db.Appraisal_Team_Leads
                                       .Select(tl => new { ID = tl.ID, StaffNo = tl.StaffData })
                                       .ToList();
                    double percentOverall = 0;
                    if (averageScore != null && averageScore != 0)
                    {
                        percentOverall = Math.Round((double)(averageScore / 5 * 100), 2);
                    }
                    sectorPojo.percentOverall = percentOverall;
                    if (staffNo != null)
                    {
                        sectorPojo.TLIdCd = (int)staffNo;
                    }
                    return View(sectorPojo);
                }
                else
                {
                    ViewBag.OriginAirportCode = db.AirportCodes
                                      .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                      .ToList();

                    ViewBag.DestinationAirportCode = db.AirportCodes
                                        .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                        .ToList();

                    ViewBag.TLlist = db.Appraisal_Team_Leads
                                       .Select(tl => new { ID = tl.ID, StaffNo = tl.StaffData })
                                       .ToList();

                    

                    SectorAppDataPojo sectorAppDataPojo = new SectorAppDataPojo();
                    return View(sectorAppDataPojo);
                }



                }catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }
           

            return View();


        }
        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public  System.Web.Mvc.ActionResult SectorSearch(int orginSector, int destSector)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                return RedirectToAction("SectorSearch", "Appraisal", new { orginSector = orginSector, destSector = destSector });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

           

            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("SectorSearch", "Appraisal", new { orginSector = orginSector, destSector = destSector });
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public System.Web.Mvc.ActionResult SectorSearchPost(DateTime From_Block_Date, DateTime To_Block_Date,SectorAppDataPojo pojo)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            var orginSector = pojo.orginSectorId;
            var destSector = pojo.dstnSectorId;
            var staffNo = pojo.TLIdCd;
            try
            {
               
                return RedirectToAction("SectorSearch", "Appraisal", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date, orginSector = orginSector, destSector = destSector, staffNo = staffNo });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }



            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("SectorSearch", "Appraisal", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date, orginSector = orginSector, destSector = destSector, staffNo = staffNo });
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CrewAppraisalInfoFromSector(int Id,DateTime FromDate ,DateTime Todate ,int Origin,int Dest, int Tlid)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                CrewAppraisal crewObj = db.CrewAppraisals.Where(a => a.Id == Id).FirstOrDefault();

                AppraisalInfoObj aprlInfo = new AppraisalInfoObj();
                aprlInfo.crewAppraisal = crewObj;

                var result = from e in db.CrewAppraisalEvals
                             join q in db.AppraisalSectionQuestions on e.QuestionId equals q.Id
                             join s in db.AppraisalSections on q.AppraisalSectionId equals s.Id
                             where e.CrewAppraisalId == crewObj.Id
                             select new
                             {
                                 crewAppraisalId = e.CrewAppraisalId,
                                 Comments = e.Comments,
                                 QuestionAnswerId = e.AnswerId,
                                 SectionId = e.SectionId,
                                 Question = q.Question,
                                 Title = s.Title
                             };
                CrewEvalScoreInfo scores;
                List<CrewEvalScoreInfo> evalList = new List<CrewEvalScoreInfo>();
                foreach (var item in result)
                {
                    scores = new CrewEvalScoreInfo();
                    scores.qnAnswerId = item.QuestionAnswerId;
                    scores.Question = item.Question;
                    scores.comments = item.Comments;
                    scores.sectionId = (int)item.SectionId;

                    AppraisalQuestionAnswer qnAnswer = db.AppraisalQuestionAnswers.Where(a => a.Id == scores.qnAnswerId).FirstOrDefault();
                    scores.scorePoint = qnAnswer.Score;
                    scores.scoreDesc = qnAnswer.Answer;

                    evalList.Add(scores);

                }
                aprlInfo.CrewEvalScorelist = evalList;
                TempData["FromDate"] = FromDate;
                TempData["ToDate"] = Todate;
                TempData["Origin"] = Origin;
                TempData["Dest"] = Dest;
                TempData["Tlid"] = Tlid;
                return View(aprlInfo);
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
        public async Task<System.Web.Mvc.ActionResult> ViewAppraisalDataByLead(int Id, String Appriaser_Id, DateTime? Month)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                CrewAppraisal crewObj = db.CrewAppraisals.Where(a => a.Id == Id).FirstOrDefault();

                AppraisalInfoObj aprlInfo = new AppraisalInfoObj();
                aprlInfo.crewAppraisal = crewObj;

                var result = from e in db.CrewAppraisalEvals
                             join q in db.AppraisalSectionQuestions on e.QuestionId equals q.Id
                             join s in db.AppraisalSections on q.AppraisalSectionId equals s.Id
                             where e.CrewAppraisalId == crewObj.Id
                             select new
                             {
                                 crewAppraisalId = e.CrewAppraisalId,
                                 Comments = e.Comments,
                                 QuestionAnswerId = e.AnswerId,
                                 SectionId = e.SectionId,
                                 Question = q.Question,
                                 Title = s.Title
                             };
                CrewEvalScoreInfo scores;
                List<CrewEvalScoreInfo> evalList = new List<CrewEvalScoreInfo>();
                foreach (var item in result)
                {
                    scores = new CrewEvalScoreInfo();
                    scores.qnAnswerId = item.QuestionAnswerId;
                    scores.Question = item.Question;
                    scores.comments = item.Comments;
                    scores.sectionId = (int)item.SectionId;

                    AppraisalQuestionAnswer qnAnswer = db.AppraisalQuestionAnswers.Where(a => a.Id == scores.qnAnswerId).FirstOrDefault();
                    scores.scorePoint = qnAnswer.Score;
                    scores.scoreDesc = qnAnswer.Answer;

                    evalList.Add(scores);

                }
                aprlInfo.CrewEvalScorelist = evalList;
                if (Month == null)
                {

                    TempData["Block_Date"] = DateTime.Now;
                }
                else
                {

                    TempData["Block_Date"] = Month;
                }

                TempData["Appriaser_Id"] = Appriaser_Id;
                TempData["Month"] = Month;
                return View(aprlInfo);
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
        public async Task<System.Web.Mvc.ActionResult> CrewOBASearch(DateTime? From_Block_Date, DateTime? To_Block_Date, int? CrewStaffNumber)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }
                String staffNo = CrewStaffNumber.ToString();
                if (staffNo == null || staffNo == "")
                {
                    
                    (TempData["From_Block_Date"]) = DateTime.Now;
                    (TempData["To_Block_Date"]) = DateTime.Now;
                    ViewBag.CrewStaffNumber = CrewStaffNumber;
                    ViewBag.CrewStaffNumber = CrewStaffNumber;
                    CrewAppDataPojo crewAppDataPojo = new CrewAppDataPojo();
                    return View(crewAppDataPojo);
                }
                else if (staffNo != null)
                {
                    if (To_Block_Date < From_Block_Date)
                    {
                        // Swap the dates
                        DateTime tempDate = (DateTime)From_Block_Date;
                        From_Block_Date = To_Block_Date;
                        To_Block_Date = tempDate;

                        // Optionally set a flag or message to indicate the dates were swapped
                        ViewBag.DatesSwapped = true; // Or ViewBag.Message = "Dates were swapped.";
                    }


                    var crAppraisals =  db.CrewAppraisals.Join(db.OCB_CrewPos,ca => ca.CrewPosId,ocp => ocp.Id,(ca, ocp) => new { CrewAppraisal = ca, CrewPos = ocp }) 
    .Where(x => x.CrewPos.StaffNumber == staffNo && x.CrewAppraisal.SubmittedDateTime >= From_Block_Date && x.CrewAppraisal.SubmittedDateTime <= To_Block_Date).Select(x => x.CrewAppraisal).ToList();

                   
                    int flightList = 0;
                    int oabCount = 0;
                    CrewAppraisalDataPojo appraisalPojo;
                    List<CrewAppraisalDataPojo> crewAppraisalDataPojos = new List<CrewAppraisalDataPojo>();
                    if (crAppraisals != null)
                    {                     
                                          


                        for (int i = 0; i < crAppraisals.Count; i++)
                        {
                            appraisalPojo = new CrewAppraisalDataPojo();
                            appraisalPojo.Id = crAppraisals[i].Id;
                            var AppraiserId = crAppraisals[i].AppraiserStaffNumber;
                            var AppraiserDetails = db.CrewDetails.Where(a => a.StaffNumber == AppraiserId).FirstOrDefault();
                            if (AppraiserDetails != null)
                            {
                                appraisalPojo.appraiserName = AppraiserDetails.StaffNumber;
                                appraisalPojo.appraiserName = AppraiserDetails.FullName;
                            }
                            var ocbPos = crAppraisals[i].CrewPosId;
                            var posDetail = db.OCB_CrewPos.Where(a => a.Id == ocbPos).Select(a => a.StaffNumber).FirstOrDefault();
                            appraisalPojo.crewStaffNo = posDetail;
                            if (posDetail != null)
                            {
                                var crewName = db.CrewDetails.Where(a => a.StaffNumber == posDetail).FirstOrDefault();
                                appraisalPojo.crewStaffName = crewName.FullName;
                            }
                            int? blockNo = crAppraisals[i].BlockId;
                            var blockDetails = db.OCB_Blocks.Where(a => a.Id == blockNo).FirstOrDefault();
                            if (blockDetails != null)
                            {
                                appraisalPojo.BlockName = blockDetails.Block_No;
                            }
                            var appid = crAppraisals[i].Id;
                            var evals = db.CrewAppraisalEvals.Where(a => a.CrewAppraisalId == appid).ToList();
                            int qtnCount = evals.Count();
                            decimal totalCrewScore = 0;
                            foreach (var evalu in evals)
                            {
                                var scoreValue = db.AppraisalQuestionAnswers.Where(a => a.Id == evalu.AnswerId).Select(a => a.Score).FirstOrDefault();
                                totalCrewScore += scoreValue.Value;
                            }
                            decimal overAllPersonalAverage = totalCrewScore / qtnCount;
                            //totalCrewScore += overAllPersonalAverage;
                            decimal roundedDecimal1 = Math.Round(overAllPersonalAverage, 2);
                            decimal overallPersonalPercent = 0;
                            if (roundedDecimal1 != null && roundedDecimal1 != 0)
                            {
                                overallPersonalPercent = Math.Round(roundedDecimal1 / 5 * 100, 2);
                            }
                            appraisalPojo.TotalScore = overallPersonalPercent;
                            appraisalPojo.submittedDate = crAppraisals[i].SubmittedDateTime;
                            crewAppraisalDataPojos.Add(appraisalPojo);
                        }
                    }
                    CrewAppDataPojo crewPojo = new CrewAppDataPojo();

                    crewPojo.crewAppraisalDatas = crewAppraisalDataPojos;
                    crewPojo.totalFlight = flightList;
                    crewPojo.totalOAB = oabCount;
                    crewPojo.crewId = staffNo;
                    ViewBag.CrewStaffNumber = CrewStaffNumber;
                    (TempData["From_Block_Date"]) = From_Block_Date;
                    (TempData["To_Block_Date"]) = To_Block_Date;
                  
            
                   
                    return View(crewPojo);
                }
                else
                {
                    ViewBag.OriginAirportCode = db.AirportCodes
                                      .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                      .ToList();

                    ViewBag.DestinationAirportCode = db.AirportCodes
                                        .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                        .ToList();

                    ViewBag.TLlist = db.Appraisal_Team_Leads
                                       .Select(tl => new { ID = tl.ID, StaffNo = tl.StaffData })
                                       .ToList();



                    SectorAppDataPojo sectorAppDataPojo = new SectorAppDataPojo();
                    return View(sectorAppDataPojo);
                }



            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }


            return View();


        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public System.Web.Mvc.ActionResult CrewOBASearchPost(DateTime From_Block_Date, DateTime To_Block_Date, String CrewStaffNumber)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            
            try
            {

                return RedirectToAction("CrewOBASearch", "Appraisal", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date, CrewStaffNumber = CrewStaffNumber });
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }



            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("CrewOBASearch", "Appraisal", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date, CrewStaffNumber = CrewStaffNumber });
        }
        //CrewAppraisalInfoFromCrewSearch

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CrewAppraisalInfoFromCrewSearch(int Id, DateTime Fromdate, DateTime Todate, string crewStaffNo)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                CrewAppraisal crewObj = db.CrewAppraisals.Where(a => a.Id == Id).FirstOrDefault();

                AppraisalInfoObj aprlInfo = new AppraisalInfoObj();
                aprlInfo.crewAppraisal = crewObj;

                var result = from e in db.CrewAppraisalEvals
                             join q in db.AppraisalSectionQuestions on e.QuestionId equals q.Id
                             join s in db.AppraisalSections on q.AppraisalSectionId equals s.Id
                             where e.CrewAppraisalId == crewObj.Id
                             select new
                             {
                                 crewAppraisalId = e.CrewAppraisalId,
                                 Comments = e.Comments,
                                 QuestionAnswerId = e.AnswerId,
                                 SectionId = e.SectionId,
                                 Question = q.Question,
                                 Title = s.Title
                             };
                CrewEvalScoreInfo scores;
                List<CrewEvalScoreInfo> evalList = new List<CrewEvalScoreInfo>();
                foreach (var item in result)
                {
                    scores = new CrewEvalScoreInfo();
                    scores.qnAnswerId = item.QuestionAnswerId;
                    scores.Question = item.Question;
                    scores.comments = item.Comments;
                    scores.sectionId = (int)item.SectionId;

                    AppraisalQuestionAnswer qnAnswer = db.AppraisalQuestionAnswers.Where(a => a.Id == scores.qnAnswerId).FirstOrDefault();
                    scores.scorePoint = qnAnswer.Score;
                    scores.scoreDesc = qnAnswer.Answer;

                    evalList.Add(scores);

                }
                aprlInfo.CrewEvalScorelist = evalList;
                TempData["FromDate"] = Fromdate;
                TempData["ToDate"] = Todate;
                
                TempData["crewStaffNo"] = crewStaffNo;
                return View(aprlInfo);
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error Data";
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS

            }

            Appraisal list = new Appraisal();

            return View(list);

        }
    }
}