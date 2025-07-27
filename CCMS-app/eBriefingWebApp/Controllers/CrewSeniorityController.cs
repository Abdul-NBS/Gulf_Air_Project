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
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;
using FormCollection = System.Web.Mvc.FormCollection;

namespace eBriefingWebApp.Controllers
{
    public class CrewSeniorityController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;
        // private readonly BlobServiceClient _blobServiceClient;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;


        public CrewSeniorityController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
            // _blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=ebriefingaims;AccountKey=n33PmfYsfpNf0GepSGew03MnHy1LENUN/PdCdrb8iMq2yKO609R9eoH7b3TqaRIZu1NZ18XBV4Gi+AStnIWJ9Q==;EndpointSuffix=core.windows.net");
            var region = System.Configuration.ConfigurationManager.AppSettings["AWSRegion"];
            _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));
            _bucketName = System.Configuration.ConfigurationManager.AppSettings["S3BucketName"];
        }

        CSMEntities db = new CSMEntities();

        // GET: eBriefing

        [Authorize(Roles = "Transport.Supervisor")]
        //[OutputCache(Duration = 900, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> UploadCrewSeniority()
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
                TempData["Status"] = "Error Data";
            }


            return View();
        }


         [Authorize(Roles = "Transport.Supervisor")]
        //[OutputCache(Duration = 900, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> Index()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            List<CrewSeniority> srseniorities = new List<CrewSeniority>();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                var seniorities = db.CrewEffectiveSeniorities.Where(a => a.IsActive == true).ToList();

                CrewSeniority srseniority;
                foreach (var seniority in seniorities)
                {
                    srseniority = new CrewSeniority();
                    srseniority.CrewId = seniority.CrewId;
                    srseniority.crewName = seniority.PilotCrewDetail.FullName;
                    srseniority.group = seniority.GroupNo;
                    srseniority.groupSeniority = seniority.GroupSeniority;
                    srseniority.Id = seniority.ID;
                    srseniority.crewPosition = seniority.PilotCrewDetail.Category;
                    srseniorities.Add(srseniority);
                }

            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }


            return View(srseniorities);
        }





         [Authorize(Roles = "Transport.Supervisor")]
        //[OutputCache(Duration = 300, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> SeniorityData(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                var getSeniority = db.CrewEffectiveSeniorities.Where(a => a.ID == Id && a.IsActive == true).FirstOrDefault();



                CrewSeniority srseniority = new CrewSeniority();
                List<CrewPeriodSeniority> periodSeniorityList = new List<CrewPeriodSeniority>();
                srseniority.Id = Id;
                srseniority.crewName = getSeniority.PilotCrewDetail.FullName;
                srseniority.CrewId = getSeniority.CrewId;
                srseniority.group = getSeniority.GroupNo;
                srseniority.groupSeniority = getSeniority.GroupSeniority;
                srseniority.crewPosition = getSeniority.PilotCrewDetail.Category;
                srseniority.fleet = getSeniority.Fleet;
                srseniority.NetSeniority = getSeniority.NetSeniority;

                //get period data
                var periodSeniority = db.CrewEffectiveSeniorityPeriods.Where(a => a.CESID == Id && a.IsActive == true).ToList();
                CrewPeriodSeniority crewPeriodSeniority;
                foreach (var period in periodSeniority)
                {
                    crewPeriodSeniority = new CrewPeriodSeniority();
                    var periodDetail = db.Seniority_PeriodDetails.Where(a => a.ID == period.PeriodId).FirstOrDefault();
                    if (periodDetail != null)
                    {
                        crewPeriodSeniority.periodMonth = periodDetail.PeriodMonth;
                        crewPeriodSeniority.periodYear = periodDetail.PeriodYear;
                        crewPeriodSeniority.periodSeniority = period.PeriodSeniority;
                        String periodStr = crewPeriodSeniority.periodMonth.ToString() + "/" + crewPeriodSeniority.periodYear.ToString();
                        DateTime parsedDate = DateTime.ParseExact(periodStr, "M/yyyy", CultureInfo.InvariantCulture);
                        string formattedDate = parsedDate.ToString("MMM yyyy");
                        crewPeriodSeniority.Period = formattedDate;
                        crewPeriodSeniority.id = period.ID;
                        crewPeriodSeniority.CSEId = period.CESID;
                        periodSeniorityList.Add(crewPeriodSeniority);
                    }
                }

                srseniority.periodseniority = periodSeniorityList;



                return View(srseniority);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }



            return View();
        }
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> EditSeniorityData(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                var getSeniority = db.CrewEffectiveSeniorities.Where(a => a.ID == Id).FirstOrDefault();



                CrewSeniority srseniority = new CrewSeniority();
                List<CrewPeriodSeniority> periodSeniorityList = new List<CrewPeriodSeniority>();
                srseniority.Id = Id;
                srseniority.crewName = getSeniority.PilotCrewDetail.FullName;
                srseniority.CrewId = getSeniority.CrewId;
                srseniority.group = getSeniority.GroupNo;
                srseniority.groupSeniority = getSeniority.GroupSeniority;
                srseniority.crewPosition = getSeniority.PilotCrewDetail.Category;
                srseniority.NetSeniority = getSeniority.NetSeniority;

                //get period data
                var periodSeniority = db.CrewEffectiveSeniorityPeriods.Where(a => a.CESID == Id).ToList();
                CrewPeriodSeniority crewPeriodSeniority;
                foreach (var period in periodSeniority)
                {
                    crewPeriodSeniority = new CrewPeriodSeniority();
                    var periodDetail = db.Seniority_PeriodDetails.Where(a => a.ID == period.PeriodId).FirstOrDefault();
                    if (periodDetail != null)
                    {
                        crewPeriodSeniority.periodMonth = periodDetail.PeriodMonth;
                        crewPeriodSeniority.periodYear = periodDetail.PeriodYear;
                        crewPeriodSeniority.periodSeniority = period.PeriodSeniority;
                        String periodStr = crewPeriodSeniority.periodMonth.ToString() + "/" + crewPeriodSeniority.periodYear.ToString();
                        DateTime parsedDate = DateTime.ParseExact(periodStr, "M/yyyy", CultureInfo.InvariantCulture);
                        string formattedDate = parsedDate.ToString("MMM yyyy");
                        crewPeriodSeniority.Period = formattedDate;
                        crewPeriodSeniority.id = period.ID;
                        crewPeriodSeniority.CSEId = period.CESID;
                        periodSeniorityList.Add(crewPeriodSeniority);
                    }
                }



                //srseniority.periodseniority = periodSeniorityList;

                ViewBag.SeniorityParent = srseniority;


                return View(periodSeniorityList);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }



            return View();
        }
        [System.Web.Mvc.HttpPost]
       [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> EditSeniorityData(List<CrewPeriodSeniority> crewSeniorityPeriods, FormCollection form)
        {
            if (form is null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            int GroupNo = Convert.ToInt32(form["seniority_parent.GroupNo"]);
            int groupSeniority = Convert.ToInt32(form["seniority_parent.GroupSeniority"]);
            string fleet = form["seniority_parent.Fleet"];
            int netSeniority = Convert.ToInt32(form["seniority_parent.NetSeniority"]);
            var CSEid = crewSeniorityPeriods.FirstOrDefault().CSEId;
            // int cseid = Convert.ToInt32(form["CrewSeniority.id"]);

            var existingItem = db.CrewEffectiveSeniorities.FirstOrDefault(x => x.ID == CSEid);
            if (existingItem != null)
            {
                existingItem.GroupNo = GroupNo;
                existingItem.GroupSeniority = groupSeniority;
                existingItem.Fleet = fleet;
                existingItem.NetSeniority = netSeniority;
                db.SaveChanges();
            }

            foreach (var crewSeniorityPeriod in crewSeniorityPeriods)
            {
                var existingperiodItem = db.CrewEffectiveSeniorityPeriods.FirstOrDefault(x => x.ID == crewSeniorityPeriod.id && x.CESID == CSEid);
                if (existingperiodItem != null)
                {
                    existingperiodItem.PeriodSeniority = crewSeniorityPeriod.periodSeniority;
                    db.SaveChanges();
                }
            }


            return RedirectToAction("EditSeniorityData", "CrewSeniority", new { Id = CSEid });
        }
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> UploadExcel(bool? afterUpload)
        {
            if (afterUpload == null)
            {
                ViewBag.Status = "Upload your data file for loading senioirty data";
                SingleFileModel model = new SingleFileModel();
                return View(model);
            }
            else if ((bool)afterUpload)
            {
                ViewBag.AfterUpload = afterUpload;
                DateTime now = DateTime.Now;
                string outputFormat = "dd MMM yyyy hh:mm tt";
                // Calculate the time for the upcoming 12 PM
                DateTime today12PM = now.Date.AddHours(12);

                // If it's already past 12 PM, add a day to get the next 12 PM
                if (now > today12PM)
                {
                    today12PM = today12PM.AddDays(1);
                }
                string formattedDate = today12PM.ToString(outputFormat);
                ViewBag.Status = "Fie uploaded successfully and data will be loaded in the database after schduled job run - " + formattedDate;
                SingleFileModel model = new SingleFileModel();
                return View(model);
            }
            SingleFileModel model1 = new SingleFileModel();
            return View(model1);
        }


        [System.Web.Mvc.HttpPost]
       [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> UploadExcel(HttpPostedFileBase fileToUpload)
        {

            //return RedirectToAction("Index", "CrewSeniority");
            // var containerClient = _blobServiceClient.GetBlobContainerClient("crewseniority"); // Commented for AWS Migration
            // var blobClient = containerClient.GetBlobClient(fileToUpload.FileName); // Commented for AWS Migration
            // try // Commented for AWS Migration
            // { // Commented for AWS Migration
            //     using (var stream = fileToUpload.InputStream) // Commented for AWS Migration
            //     { // Commented for AWS Migration
            //         await blobClient.UploadAsync(stream); // Commented for AWS Migration
            //     } // Commented for AWS Migration
            // } // Commented for AWS Migration
            // catch (Exception e) // Commented for AWS Migration
            // { // Commented for AWS Migration


            // } // Commented for AWS Migration

            // --- AWS S3 Migration: S3 upload logic below ---
            string containerName = "crewseniority";
            string uniqueFileName = fileToUpload.FileName;
            string key = $"{containerName}/{uniqueFileName}"; // S3 'folders' are just key prefixes
            using (var fileTransferUtility = new TransferUtility(_s3Client))
            {
                await fileTransferUtility.UploadAsync(fileToUpload.InputStream, _bucketName, key);
            }
            string blobUrl = $"https://{_bucketName}.s3.amazonaws.com/{key}";


            return RedirectToAction("UploadExcel", "CrewSeniority", new { afterUpload = true });


        }
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> CrewSeniorityHistory(int CrewId, int CesId)
        {

            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            List<CrewSeniority> crSeniorityList = new List<CrewSeniority>();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                String crewId = CrewId.ToString();
                var getPastSeniority = db.CrewEffectiveSeniorities.Where(a => a.CrewId == crewId && a.IsActive == false).ToList();

                if (getPastSeniority == null || getPastSeniority.Count == 0)
                {
                    ViewBag.Status = "No past history";
                    return RedirectToAction("SeniorityData", "CrewSeniority", new { Id = CesId });
                }
                else
                {
                    foreach (var crew in getPastSeniority)
                    {

                        CrewSeniority srseniority = new CrewSeniority();
                        List<CrewPeriodSeniority> periodSeniorityList = new List<CrewPeriodSeniority>();
                        srseniority.Id = crew.ID;
                        srseniority.crewName = crew.PilotCrewDetail.FullName;
                        srseniority.CrewId = crew.CrewId;
                        srseniority.group = crew.GroupNo;
                        srseniority.groupSeniority = crew.GroupSeniority;
                        srseniority.crewPosition = crew.PilotCrewDetail.Category;
                        srseniority.fleet = crew.Fleet;
                        srseniority.NetSeniority = crew.NetSeniority;

                        //get period data
                        var periodSeniority = db.CrewEffectiveSeniorityPeriods.Where(a => a.CESID == crew.ID && a.IsActive == false).ToList();
                        CrewPeriodSeniority crewPeriodSeniority;
                        foreach (var period in periodSeniority)
                        {
                            crewPeriodSeniority = new CrewPeriodSeniority();
                            var periodDetail = db.Seniority_PeriodDetails.Where(a => a.ID == period.PeriodId).FirstOrDefault();
                            if (periodDetail != null)
                            {
                                crewPeriodSeniority.periodMonth = periodDetail.PeriodMonth;
                                crewPeriodSeniority.periodYear = periodDetail.PeriodYear;
                                crewPeriodSeniority.periodSeniority = period.PeriodSeniority;
                                String periodStr = crewPeriodSeniority.periodMonth.ToString() + "/" + crewPeriodSeniority.periodYear.ToString();
                                DateTime parsedDate = DateTime.ParseExact(periodStr, "M/yyyy", CultureInfo.InvariantCulture);
                                string formattedDate = parsedDate.ToString("MMM yyyy");
                                crewPeriodSeniority.Period = formattedDate;
                                crewPeriodSeniority.id = period.ID;
                                crewPeriodSeniority.CSEId = period.CESID;
                                periodSeniorityList.Add(crewPeriodSeniority);
                            }
                        }

                        srseniority.periodseniority = periodSeniorityList;
                        crSeniorityList.Add(srseniority);
                    }
                }

                return View(crSeniorityList);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }



            return View();


        }
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> LayoverBidData()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            List<Layover_Bids> layover_Bids = new List<Layover_Bids>();
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
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }


            return View(layover_Bids);
        }
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> EditBidData(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            Layover_Bids layover_Bids = null;
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                layover_Bids = db.Layover_Bids.Where(a => a.ID == Id).FirstOrDefault();
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }
            ViewBag.Category = db.Layover_Category
        .Select(category => new { ID = category.ID, CAT = category.CategoryName })
        .ToList();

            return View(layover_Bids);


        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> EditBidData(Layover_Bids layover_Bids)
        {
            var existingItem = db.Layover_Bids.FirstOrDefault(x => x.ID == layover_Bids.ID);
            if (existingItem != null)
            {
                existingItem.CAT_ID = layover_Bids.CAT_ID;
                existingItem.FC_BOING_BIDLIMIT = layover_Bids.FC_BOING_BIDLIMIT;
                existingItem.FC_AIRBUS_BIDLIMIT = layover_Bids.FC_AIRBUS_BIDLIMIT;
                existingItem.CM_BIDLIMIT = layover_Bids.CM_BIDLIMIT;
                existingItem.CS_BIDLIMIT = layover_Bids.CS_BIDLIMIT;
                existingItem.FG_FA_BIDLIMIT = layover_Bids.FG_FA_BIDLIMIT;

                db.SaveChanges();
            }
            return RedirectToAction("LayoverBidData", "CrewSeniority");
        }

        public async Task<System.Web.Mvc.ActionResult> AddNewBidData()
        {
            ViewBag.Category = db.Layover_Category
                                .Select(category => new { ID = category.ID, CAT = category.CategoryName })
                                .ToList();
            ViewBag.AirportCode = db.AirportCodes
                                  .Select(airport => new { ID = airport.Id, Airport_code = airport.Code })
                                  .ToList();
            Layover_Bids layover_Bids = new Layover_Bids();
            return View(layover_Bids);
        }


        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> AddNewBidData(Layover_Bids layover_Bids)
        {
            Layover_Bids newBid = new Layover_Bids
            {
                FC_AIRBUS_BIDLIMIT = layover_Bids.FC_AIRBUS_BIDLIMIT,
                FC_BOING_BIDLIMIT = layover_Bids.FC_BOING_BIDLIMIT,
                FG_FA_BIDLIMIT = layover_Bids.FG_FA_BIDLIMIT,
                STN_ID = layover_Bids.STN_ID,
                CAT_ID = layover_Bids.CAT_ID,
                CM_BIDLIMIT = layover_Bids.CM_BIDLIMIT,
                CS_BIDLIMIT = layover_Bids.CS_BIDLIMIT,
                IsActive = true,
                IsDeleted = false
            };


            db.Layover_Bids.Add(newBid);

            // Save changes to the database
            db.SaveChanges();

            return RedirectToAction("LayoverBidData", "CrewSeniority");
        }
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> DeleteBidData(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            Layover_Bids layover_Bids = null;
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                layover_Bids = db.Layover_Bids.Where(a => a.ID == Id).FirstOrDefault();
                db.Layover_Bids.Remove(layover_Bids);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }
            return RedirectToAction("LayoverBidData", "CrewSeniority");
        }
       [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> MaximumLayoverBid()
        {
            List<Maximum_Bids> maximum_Bids = db.Maximum_Bids.ToList();

            return View(maximum_Bids);
        }
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> EditMaxData(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            Maximum_Bids max_Bids = null;
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                max_Bids = db.Maximum_Bids.Where(a => a.ID == Id).FirstOrDefault();
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }


            return View(max_Bids);


        }
        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> EditMaxData(Maximum_Bids max_Bids)
        {
            var existingItem = db.Maximum_Bids.FirstOrDefault(x => x.ID == max_Bids.ID);
            if (existingItem != null)
            {

                existingItem.FC_BOING_MAX = max_Bids.FC_BOING_MAX;
                existingItem.FC_AIRBUS_MAX = max_Bids.FC_AIRBUS_MAX;
                existingItem.CM_MAX = max_Bids.CM_MAX;
                existingItem.CS_MAX = max_Bids.CS_MAX;
                existingItem.FG_FA_MAX = max_Bids.FG_FA_MAX;

                db.SaveChanges();
            }
            return RedirectToAction("MaximumLayoverBid", "CrewSeniority");
        }
       [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> LayoverPairingIndex()
        {

            List<Layover_Pairing> layoverPairings = db.Layover_Pairing.Where(a => a.IsActive == true && a.IsDeleted == false).ToList();


            return View(layoverPairings);
        }
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> DeletePairingData(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            Layover_Pairing layover_pairings = null;
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                layover_pairings = db.Layover_Pairing.Where(a => a.ID == Id).FirstOrDefault();
                layover_pairings.IsDeleted = true;
                //db.Layover_Pairing.Remove(layover_pairings);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }
            return RedirectToAction("LayoverPairingIndex", "CrewSeniority");
        }
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> AddNewPairingData()
        {
            ViewBag.Category = db.Pairing_Category
                                   .Select(category => new { ID = category.ID, CAT = category.CategoryName })
                                   .ToList();
            ViewBag.Fleet = db.Fleet_Category
                                  .Select(flt => new { ID = flt.ID, FltCd = flt.Fleet })
                                  .ToList();
            Layover_Pairing layover_pairing = new Layover_Pairing();
            return View(layover_pairing);
        }
        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> AddNewPairingData(Layover_Pairing layover_Pairing, FormCollection form)
        {
            if (form is null)
            {
                throw new ArgumentNullException(nameof(form));
            }
            DateTime Operation_date = DateTime.Now;
            if (form["Operation_Date"] != "")
            {
                 Operation_date = Convert.ToDateTime(form["Operation_Date"]);
            }
            else
            {
                 Operation_date = DateTime.Now;
            }
            int? Pair_Cat_id = layover_Pairing.Pair_Cat_ID;
            int addDays = 0;
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
            else if (Pair_Cat_id == 4)
            {
                addDays = 60;
            }
            DateTime next_oper_Dt = Operation_date.AddDays(addDays);
            Layover_Pairing new_Pairing = new Layover_Pairing();
            new_Pairing.Operation_Date = Operation_date;
            new_Pairing.Pair_Cat_ID = layover_Pairing.Pair_Cat_ID;
            new_Pairing.Fleet_Id = layover_Pairing.Fleet_Id;
            new_Pairing.Next_Operation = next_oper_Dt;
            new_Pairing.Pairing = layover_Pairing.Pairing;
            new_Pairing.IsActive = true;
            new_Pairing.IsDeleted = false;
            db.Layover_Pairing.Add(new_Pairing);

            // Save changes to the database
            db.SaveChanges();


            return RedirectToAction("LayoverPairingIndex", "CrewSeniority");


        }
        //[Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> GetRosterData()
        {
            List<Roster_Data> rosterData = db.Roster_Data.Where(a => a.IsActive == true && a.IsDeleted == false).ToList();


            return View(rosterData);
        }
       // [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> DeleteRosterData(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            Roster_Data rosterData = null;
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                rosterData = db.Roster_Data.Where(a => a.ID == Id).FirstOrDefault();
                rosterData.IsDeleted = true;
                //db.Layover_Pairing.Remove(layover_pairings);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }
            return RedirectToAction("GetRosterData", "CrewSeniority");
        }
      //  [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> NewRosterMonthData()
        {
            List<SelectListItem> months = new List<SelectListItem>();
            for (int i = 1; i <= 12; i++)
            {
                months.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
                });
            }
            ViewBag.Months = months;
            int currentYear = DateTime.Now.Year;

            // Create a list of the next 10 years
            List<SelectListItem> years = Enumerable.Range(currentYear, 10)
                .Select(year => new SelectListItem
                {
                    Text = year.ToString(),
                    Value = year.ToString()
                })
                .ToList();

            ViewBag.Years = years;
            List<SelectListItem> groupNo = new List<SelectListItem>();
            for (int i = 1; i <= 9; i++)
            {
                groupNo.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString()
                });
            }
            ViewBag.Groups = groupNo;
            Roster_Data roster_Data = new Roster_Data();
            return View(roster_Data);
        }
        [System.Web.Mvc.HttpPost]
     //   [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> NewRosterMonthData(Roster_Data rosterData, FormCollection form)
        {
            if (form is null)
            {
                throw new ArgumentNullException(nameof(form));
            }
            DateTime Pub_date = Convert.ToDateTime(form["Roster_Publication_Date"]);
            Roster_Data newRosterData = new Roster_Data();

            DateTimeFormatInfo dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat;
            String monthFormat = dateTimeFormat.GetAbbreviatedMonthName(Convert.ToInt32(rosterData.ROSTERMONTH));
            newRosterData.ROSTERMONTH = monthFormat;
            newRosterData.ROSTERYEAR = rosterData.ROSTERYEAR;
            newRosterData.ROSTER_PUB_DATE = Pub_date;
            newRosterData.HIGH_SENIORITY_GROUP = rosterData.HIGH_SENIORITY_GROUP;
            newRosterData.IsActive = true;
            newRosterData.IsDeleted = false;
            db.Roster_Data.Add(newRosterData);
            db.SaveChanges();


            return RedirectToAction("GetRosterData", "CrewSeniority");



        }
       [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> EditPairingData(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            Layover_Pairing layoverPairing = null;
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }


                layoverPairing = db.Layover_Pairing.Where(a => a.ID == Id).FirstOrDefault();
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }
            ViewBag.Category = db.Pairing_Category
                                .Select(category => new { ID = category.ID, CAT = category.CategoryName })
                                .ToList();
            ViewBag.Fleet = db.Fleet_Category
                                  .Select(flt => new { ID = flt.ID, FltCd = flt.Fleet })
                                  .ToList();

            return View(layoverPairing);
        }
        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "Transport.Supervisor")]
        public async Task<System.Web.Mvc.ActionResult> EditPairingData(Layover_Pairing layover_Pairing)
        {
            var existingItem = db.Layover_Pairing.FirstOrDefault(x => x.ID == layover_Pairing.ID);
            if (existingItem != null)
            {

                existingItem.Fleet_Id = layover_Pairing.Fleet_Id;
                existingItem.Pair_Cat_ID = layover_Pairing.Pair_Cat_ID;
                existingItem.Operation_Date = layover_Pairing.Operation_Date;
                existingItem.Next_Operation = layover_Pairing.Next_Operation;
               
                db.SaveChanges();
            }
            return RedirectToAction("LayoverPairingIndex", "CrewSeniority");
        }
    }

}

