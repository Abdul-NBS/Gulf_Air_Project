using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using eBriefingWebApp.Helper;
using eBriefingWebApp.Interfaces;
using eBriefingWebApp.Models;
using eBriefingWebApp.ViewModels;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;
using FormCollection = System.Web.Mvc.FormCollection;

namespace eBriefingWebApp.Controllers
{
    public class CrewDataController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;
        // private readonly BlobServiceClient _blobServiceClient;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;


        public CrewDataController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
            // _blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=ebriefingaims;AccountKey=n33PmfYsfpNf0GepSGew03MnHy1LENUN/PdCdrb8iMq2yKO609R9eoH7b3TqaRIZu1NZ18XBV4Gi+AStnIWJ9Q==;EndpointSuffix=core.windows.net");
            var region = System.Configuration.ConfigurationManager.AppSettings["AWSRegion"];
            _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));
            _bucketName = System.Configuration.ConfigurationManager.AppSettings["S3BucketName"];
        }

        CSMEntities db = new CSMEntities();
       // [Authorize(Roles = "CSM.Admins")]
        public async Task<System.Web.Mvc.ActionResult> Index()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }
                CrewDataPojo data = new CrewDataPojo();
                 return View(data);
                
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            List<OCB_Blocks> list = new List<OCB_Blocks>();

            TempData["Block_Date"] = DateTime.Now;

            return View(list);
        }

        [System.Web.Mvc.HttpGet]
      //  [Authorize(Roles = "CSM.Admins")]
        public JsonResult GetStaffInfo(string StaffNumber)
        {
            // check if staffnumber is part of TL
            
                var query = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).Select(a => new
                {
                    a.StaffNumber,
                    a.FullName,
                    a.Category
                }).FirstOrDefault();

                return Json(query, JsonRequestBehavior.AllowGet);
            }

        [System.Web.Mvc.HttpGet]
       // [Authorize(Roles = "CSM.Admins")]
        public async Task<System.Web.Mvc.ActionResult> CrewDataConsolidated(CrewDataPojo model)
        {
            CrewDataPojo data = new CrewDataPojo();
            String StaffNumber = model.StaffNumber;

            var CrewDetail = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).FirstOrDefault();
            var GroomingAttribute = db.CrewGroomingAttributes.Where(a=>a.Employee_Number == StaffNumber).FirstOrDefault();
            var CrewWeightDetail = db.CrewWeightDetailLists.Where(a => a.Employee_Number == StaffNumber && (a.Is_Deleted == null || a.Is_Deleted == false)).OrderByDescending(a => a.Date_Weight).Take(1).FirstOrDefault();

            var crew_Appreciation = db.Crew_Appreciation.Where(a=>a.StaffNumber==StaffNumber && (a.IsDeleted == null || a.IsDeleted == false)).ToList();
            var crew_Warning = db.Crew_WarningLetter.Where(a=>a.StaffNumber == StaffNumber && (a.IsDeleted == null || a.IsDeleted == false)).ToList();
            var crew_commendation = db.Crew_Commendation.Where(a => a.StaffNumber == StaffNumber && (a.IsDeleted == null || a.IsDeleted == false)).ToList();

            var crewTest = await db.PreFlight_CrewTest.Where(a => a.OCB_CrewPos.StaffNumber == StaffNumber).OrderByDescending(a => a.OCB_Flights.Flight_Date).Take(5).ToListAsync();

            var crAppraisals = db.CrewAppraisals.Join(db.OCB_CrewPos, ca => ca.CrewPosId, ocp => ocp.Id, (ca, ocp) => new { CrewAppraisal = ca, CrewPos = ocp })
   .Where(x => x.CrewPos.StaffNumber == StaffNumber ).Select(x => x.CrewAppraisal).OrderByDescending(ca => ca.SubmittedDateTime).Take(5).ToList();


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
            String designation = "";
            //check id staff is part of Teamlead
            var tl = db.Appraisal_Team_Leads.Where(a=>a.StaffNumber== StaffNumber).FirstOrDefault();
            if (tl != null) {
                designation = CrewDetail.Category + "  (Team Lead)";
            }
            else
            {
                designation = CrewDetail.Category;
            }


            var imagepath = db.CrewPhotos.Where(a=>a.StaffNumber == StaffNumber).Select(b=>b.AttachmentPath).FirstOrDefault();
            data.StaffNumber = StaffNumber;
            data.WarningsList = crew_Warning;
            data.crew_Commendations = crew_commendation;
            data.crewAppreciation = crew_Appreciation;
            data.crewAppraisalDataPojos=crewAppraisalDataPojos;
            data.FullName=CrewDetail.FullName;
            data.Designation = designation;
            data.gender = CrewDetail.Sex;
            data.WeightDetailList = CrewWeightDetail;
            data.preFlight_CrewTest = crewTest;
            data.Grooming = GroomingAttribute;
            data.imagepath = imagepath;
            data.nation = CrewDetail.Nation;
            
            return View(data);
        }

















        }
}

    
