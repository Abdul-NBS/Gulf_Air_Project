// AWS S3 Migration: The following usings and logic are added for AWS S3 integration as part of Azure Blob to S3 migration.
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
using Amazon;

namespace eBriefingWebApp.Controllers
{
    public class CGAPController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;
        // private readonly BlobServiceClient _blobServiceClient; // Commented for AWS Migration
        private readonly IAmazonS3 _s3Client; // AWS S3 Migration: S3 client for AWS integration
        private readonly string _bucketName; // AWS S3 Migration: S3 bucket name from config


        public CGAPController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
            // _blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=ebriefingaims;AccountKey=n33PmfYsfpNf0GepSGew03MnHy1LENUN/PdCdrb8iMq2yKO609R9eoH7b3TqaRIZu1NZ18XBV4Gi+AStnIWJ9Q==;EndpointSuffix=core.windows.net"); // Commented for AWS Migration
            var region = System.Configuration.ConfigurationManager.AppSettings["AWSRegion"];
            _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)); // AWS S3 Migration: Uses region from config
            _bucketName = System.Configuration.ConfigurationManager.AppSettings["S3BucketName"];
        }

        CSMEntities db = new CSMEntities();




        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        //[OutputCache(Duration = 900, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> Index(int? IsCheck)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            Crew_Grooming crew_Grooming = new Crew_Grooming();
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
            if (IsCheck == null)
            {
                TempData["Status"] = "Loadstaff";
                return View(crew_Grooming);
            }
            else if (IsCheck == 1)
            {
                TempData["Status"] = "NoCrew";
                return View(crew_Grooming);
            }
            else
            {
                return View(crew_Grooming);
            }
        }
        // [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        /*   public async Task<System.Web.Mvc.ActionResult> CrewGroomingData(String Id)
           {
               var telemetry = new TelemetryClient();
               Crew_Grooming crew_Grooming = new Crew_Grooming();
               try
               {
                    var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                   String LoggedStaffNumber = "";
                    if (getUser != null)
                    {
                        LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                         Session["FullName"] = getUser.GivenName + " " + getUser.Surname;

                    }
                   // get attributes data
                   var data = db.CrewGroomingAttributes.Where(a => a.Employee_Number == Id).FirstOrDefault();


                   var latestWeight = db.CrewWeightDetailLists.Where(a => a.Employee_Number == Id && (a.Is_Deleted == null || a.Is_Deleted == false)).OrderByDescending(a => a.Date_From).FirstOrDefault();

                   CrewGroomingPojo groomingdata = new CrewGroomingPojo();
                   if (data != null)
                   {
                       groomingdata.StaffNo = data.Employee_Number;
                       groomingdata.staffName = data.Employee_Name;
                       if (data.Height.HasValue)
                       {
                           var Crewdetail = db .CrewDetails.Where(a=>a.StaffNumber == Id ).FirstOrDefault();
                           groomingdata.height = (decimal)data.Height;
                           decimal height = (decimal)data.Height;
                           double minweight = 0;
                           double maxweight = 0;
                           if (Crewdetail != null)
                           {
                               var crewGender = Crewdetail.Sex;
                               var heightWeightRatio = db.WeightMinMaxSettings.Where(a => a.height == height && a.gender == crewGender && a.isactive == true && a.isdeleted == false).FirstOrDefault();
                               var variancePercent = heightWeightRatio.WeightVarianceSetting.variance_percentage;
                               minweight = Math.Round((double)(heightWeightRatio.min_weight * (1 - (variancePercent / 100))));
                               maxweight = Math.Round((double)(heightWeightRatio.max_weight * (1 + (variancePercent / 100))));

                           }
                           else
                           {
                               minweight = 18.5 * (double)height * (double)height;
                               maxweight = 24.9 * (double)height * (double)height;
                           }


                           groomingdata.minweight = minweight;
                           groomingdata.maxweight = maxweight;

                       }
                       groomingdata.gender = data.Gender;
                       groomingdata.nation = data.Nationality;
                       if (data.Eye_Color_Cd.HasValue) // Check if Eye_Color_Cd has a value
                       {
                           groomingdata.eyecolor = data.Eye_Color_Cd.Value;
                           ViewBag.EyeColorList = new SelectList(db.Crew_Grooming_EyeColor.ToList(), "ID", "Eye_Clr", groomingdata.eyecolor);
                       }
                       else
                       {
                           ViewBag.EyeColorList = new SelectList(db.Crew_Grooming_EyeColor.ToList(), "ID", "Eye_Clr"); // No selected value
                       }

                       // Hair Color Dropdown
                       if (data.Hair_Color_Cd.HasValue) // Check if Hair_Color_Cd has a value
                       {
                           groomingdata.haircolor = data.Hair_Color_Cd.Value;
                           ViewBag.HairColorList = new SelectList(db.Crew_Grooming_HairColor.ToList(), "ID", "Hair_Clr", groomingdata.haircolor);
                       }
                       else
                       {
                           ViewBag.HairColorList = new SelectList(db.Crew_Grooming_HairColor.ToList(), "ID", "Hair_Clr"); // No selected value
                       }
                   }
                   else
                   {
                       ViewBag.EyeColorList = new SelectList(db.Crew_Grooming_EyeColor.ToList(), "ID", "Eye_Clr");
                       ViewBag.HairColorList = new SelectList(db.Crew_Grooming_HairColor.ToList(), "ID", "Hair_Clr");
                       var CrewDetail = db.CrewDetails.Where(a => a.StaffNumber == Id).FirstOrDefault();
                       if (CrewDetail != null) {
                           groomingdata.staffName = CrewDetail.FullName;
                           groomingdata.nation=CrewDetail.Nation;
                           groomingdata.category = CrewDetail.Category;
                           groomingdata.StaffNo = Id;
                           groomingdata.gender = CrewDetail.Sex;
                       }

                   }


                       if (latestWeight != null)
                       {
                               if (latestWeight.Weight_Status_Id != null)
                               {
                                groomingdata.weightStat = db.Crew_Grooming_WeightStatus.Where(a => a.ID == latestWeight.Weight_Status_Id).Select(a => a.Weight_Status).FirstOrDefault();
                                }
                               if (latestWeight.Weight != null)
                               {
                                   groomingdata.weight = (decimal)latestWeight.Weight;
                               }
                           groomingdata.remarks = latestWeight.Remarks;
                           groomingdata.created = latestWeight.Date_Weight;
                       }


                       // Eye Color Dropdown


                       TempData["LoggedUser"] = LoggedStaffNumber;
                       TempData["CrewNo"] = Id;
                       var imagepath = db.CrewPhotos.Where(a => a.StaffNumber == Id).Select(b => b.AttachmentPath).FirstOrDefault();
                       groomingdata.imagepath = imagepath;
                        groomingdata.StaffNo = Id;
                       return View(groomingdata);


               }
               catch (Exception ex)
               {
                   // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                   TempData["Status"] = "Error Data";
               }


               return View();
           }*/



        [System.Web.Mvc.HttpGet]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> GetStaffInfo(string StaffNumber)
        {

            var groomingAttributeDate = db.CrewGroomingAttributes.Where(a => a.Employee_Number == StaffNumber).FirstOrDefault();

            var groomingWeightList = db.CrewWeightDetailLists.Where(a => a.Employee_Number == StaffNumber).ToList();

            if (groomingAttributeDate != null && groomingWeightList != null && groomingWeightList.Count > 0)
            {
                return RedirectToAction("CrewGroomingData", "CGAP", new { Id = StaffNumber });
            }
            else
            {
                // check if entered staff is a crew
                var crewDetail = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).FirstOrDefault();
                if (crewDetail != null)
                {
                    return RedirectToAction("AddNewGroomingInfo", "CGAP", new { Id = StaffNumber });
                }

                else
                {
                    return RedirectToAction("Index", "CGAP");
                }
            }



        }


        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public JsonResult UpdateWeight(DateTime EntryDate, double Weight, string Remarks, DateTime NextReviewDate, string StaffNumber, string LoggedUser) // Add StaffNumber parameter
        {
            var Crewdetail = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).FirstOrDefault();
            double minweight = 0;
            double maxweight = 0;
            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            DateTime combinedEnteredDateTime = EntryDate.Add(currentTime);
            var height = db.CrewGroomingAttributes.Where(a => a.Employee_Number == StaffNumber).Select(a => a.Height).FirstOrDefault();
            if (Crewdetail != null)
            {
                var crewGender = Crewdetail.Sex;
                var heightWeightRatio = db.WeightMinMaxSettings.Where(a => a.height == height && a.gender == crewGender && a.isactive == true && a.isdeleted == false).FirstOrDefault();
                var variancePercent = heightWeightRatio.WeightVarianceSetting.variance_percentage;
                // var variancePercent = heightWeightRatio.WeightVarianceSetting.variance_percentage;
                // Convert decimal? to double explicitly
                minweight = Math.Round((double)(heightWeightRatio.min_weight.Value * (1 - (variancePercent / 100))));
                maxweight = Math.Round((double)(heightWeightRatio.max_weight.Value * (1 + (variancePercent / 100))));

            }
            else
            {
                minweight = 18.5 * (double)height * (double)height;
                maxweight = 24.9 * (double)height * (double)height;
            }
            var latestWeightData = db.CrewWeightDetailLists.Where(a => a.Employee_Number == StaffNumber).OrderByDescending(a => a.Date_Weight).FirstOrDefault();
            decimal weightDiff;
            if (latestWeightData != null)
            {
                weightDiff = (decimal)((decimal)Weight - latestWeightData.Weight);
            }
            else
            {
                weightDiff = 0;
            }
            CrewWeightDetailList newRec = new CrewWeightDetailList();
            newRec.Employee_Number = StaffNumber;
            if (Crewdetail != null)
            {
                newRec.Employee_Name = Crewdetail.FullName;
            }
            newRec.Weight = (decimal?)Weight;
            newRec.Remarks = Remarks;
            newRec.Date_From = combinedEnteredDateTime;
            newRec.Date_Weight = combinedEnteredDateTime;
            newRec.Gain_Loss = weightDiff;
            newRec.Date_To = NextReviewDate;
            newRec.Updated_By = LoggedUser;
            newRec.Is_Deleted = false;

            if (Weight > maxweight)
            {
                newRec.Weight_Status_Id = db.Crew_Grooming_WeightStatus.Where(a => a.Weight_Status == "Over Weight").Select(a => a.ID).FirstOrDefault();
            }
            else if (Weight < minweight)
            {
                newRec.Weight_Status_Id = db.Crew_Grooming_WeightStatus.Where(a => a.Weight_Status == "Under Weight").Select(a => a.ID).FirstOrDefault();
            }
            else
            {
                newRec.Weight_Status_Id = db.Crew_Grooming_WeightStatus.Where(a => a.Weight_Status == "Healthy Weight").Select(a => a.ID).FirstOrDefault();
            }
            db.CrewWeightDetailLists.Add(newRec);
            db.SaveChanges(); // Save the new record


            return Json(new { success = true });
        }


        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public JsonResult UpdateGroomingAttributes(int Eye_Color_Cd, int Hair_Color_Cd, decimal Height, string StaffNumber, string LoggedUser)
        {
            try
            {
                double minweight = 0;
                double maxweight = 0;
                var crewGrooming = db.CrewGroomingAttributes.FirstOrDefault(c => c.Employee_Number == StaffNumber);
                if (crewGrooming != null && crewGrooming.Height != Height)
                {
                    var latestWeight = db.CrewWeightDetailLists.Where(a => a.Employee_Number == StaffNumber && (a.Is_Deleted == null || a.Is_Deleted == false)).OrderByDescending(a => a.Date_From).FirstOrDefault();
                    if (latestWeight != null)
                    {
                        var Crewdetail = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).FirstOrDefault();
                        if (Crewdetail != null)
                        {
                            var crewGender = Crewdetail.Sex;
                            var heightWeightRatio = db.WeightMinMaxSettings.Where(a => a.height == Height && a.gender == crewGender && a.isactive == true && a.isdeleted == false).FirstOrDefault();
                            var variancePercent = heightWeightRatio.WeightVarianceSetting.variance_percentage;
                            minweight = Math.Round((double)(heightWeightRatio.min_weight * (1 - (variancePercent / 100))));
                            maxweight = Math.Round((double)(heightWeightRatio.max_weight * (1 + (variancePercent / 100))));

                        }
                        else
                        {
                            minweight = 18.5 * (double)Height * (double)Height;
                            maxweight = 24.9 * (double)Height * (double)Height;
                        }

                        if (latestWeight.Weight.HasValue && (double)latestWeight.Weight.Value > maxweight)
                        {
                            latestWeight.Weight_Status_Id = db.Crew_Grooming_WeightStatus.Where(a => a.Weight_Status == "Over Weight").Select(a => a.ID).FirstOrDefault();
                        }
                        else if (latestWeight.Weight.HasValue && (double)latestWeight.Weight.Value < minweight)
                        {
                            latestWeight.Weight_Status_Id = db.Crew_Grooming_WeightStatus.Where(a => a.Weight_Status == "Under Weight").Select(a => a.ID).FirstOrDefault();
                        }
                        else
                        {
                            latestWeight.Weight_Status_Id = db.Crew_Grooming_WeightStatus.Where(a => a.Weight_Status == "Healthy Weight").Select(a => a.ID).FirstOrDefault();
                        }

                        db.SaveChanges();
                    }

                }

                crewGrooming.Eye_Color_Cd = Eye_Color_Cd;
                crewGrooming.Hair_Color_Cd = Hair_Color_Cd;
                crewGrooming.Height = Height;
                crewGrooming.Updated_By = LoggedUser;
                db.SaveChanges(); // Save changes within the using block





                return Json(new { success = true }); // Return JSON success
                                                     // The context is disposed of automatically here
            }
            catch (Exception ex)
            {
                // Log the exception (using your preferred logging method)
                System.Diagnostics.Debug.WriteLine($"Error updating grooming attributes: {ex.Message}"); // Example logging
                                                                                                         // Consider using your telemetry client here if you have one
                                                                                                         // telemetry.TrackException(ex);

                return Json(new { success = false, message = "Error updating grooming attributes." }); // Return JSON error
            }
        }
        [System.Web.Mvc.HttpGet]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public JsonResult GetWeightHistory(string StaffNumber)
        {

            var weightHistory = db.CrewWeightDetailLists
     .Where(c => c.Employee_Number == StaffNumber && (c.Is_Deleted == null || c.Is_Deleted == false))
     .OrderByDescending(c => c.Date_Weight) // Sort by Date_Weight in descending order
     .ToList();

            List<object> formattedHistory = new List<object>();

            // Loop through all items
            for (int i = 0; i < weightHistory.Count; i++) // Fix: Loop until weightHistory.Count
            {
                var currentItem = weightHistory[i];
                var updatedUser = currentItem.Updated_By;

                // Calculate LossOrGain if there is a next item


                var formattedItem = new
                {
                    EntryDate = currentItem.Date_Weight.HasValue
                        ? currentItem.Date_Weight.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
                        : "",
                    Weight = currentItem.Weight,
                    Remarks = currentItem.Remarks,
                    LossOrGain = currentItem.Gain_Loss, // Include calculated LossOrGain
                    UpdatedCrewId = updatedUser,
                    Id = currentItem.Id
                };

                formattedHistory.Add(formattedItem);
            }

            return Json(formattedHistory, JsonRequestBehavior.AllowGet);

            // return Json(formattedHistory, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> AddNewGroomingInfo(string Id)
        {
            var telemetry = new TelemetryClient();
            CrewGroomingPojo crew_Grooming = new CrewGroomingPojo();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                var imagePath = db.CrewPhotos.Where(a => a.StaffNumber == Id).Select(b => b.AttachmentPath).FirstOrDefault();
                String LoggedStaffNumber = "";
                if (getUser != null)
                {
                    LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                ViewBag.EyeColor = db.Crew_Grooming_EyeColor
                                  .Select(eyecolor => new { ID = eyecolor.ID, Eyecolor = eyecolor.Eye_Clr })
                                  .ToList();
                ViewBag.HairColor = db.Crew_Grooming_HairColor
                                 .Select(haircolor => new { ID = haircolor.ID, Haircolor = haircolor.Hair_Clr })
                                 .ToList();

                var crewDetail = db.CrewDetails.Where(a => a.StaffNumber == Id).FirstOrDefault();

                var crewgender = crewDetail.Sex;
                // Get height options based on gender
                // First get the raw data from database
                // First get the distinct heights from database
                var heightValues = db.WeightMinMaxSettings
                    .Where(x => x.gender == crewgender && x.isactive == true && x.isdeleted == false)
                    .Select(x => x.height)  // Use lowercase 'height' to match entity
                    .Distinct()
                    .ToList();

                // Then create the formatted options in memory
                var heightOptions = heightValues
                    .OrderBy(h => h)
                    .Select(h => new
                    {
                        Height = h,
                        DisplayText = h.HasValue ? h.Value.ToString("0.00") + "m" : "N/A"
                    })
                    .ToList();

                ViewBag.HeightOptions = new SelectList(heightOptions, "Height", "DisplayText");

                TempData["LoggedUser"] = LoggedStaffNumber;
                TempData["CrewNo"] = Id;
                crew_Grooming.StaffNo = Id;
                crew_Grooming.updatedUser = LoggedStaffNumber;
                crew_Grooming.imagepath = imagePath;
                crew_Grooming.gender = crewgender;  // for testing 523142
                return View(crew_Grooming);
            }
            catch (Exception ex)
            {
                // Handle exception
            }
            return View();
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> AddNewGroomingInfo(CrewGroomingPojo crew_Grooming)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            String LoggedStaffNumber = "";
            if (getUser != null)
            {
                LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;

            }
            int Weight_Status_Id = 0;
            var height = crew_Grooming.height;

            // check if height is in the agreed range 
            var existingSetting = db.WeightMinMaxSettings
                        .FirstOrDefault(w => w.height == crew_Grooming.height && w.gender == crew_Grooming.gender);

            if (existingSetting != null)
            {

                double minWeight = (double)existingSetting.min_weight;
                double maxWeight = (double)existingSetting.max_weight;

                if (Convert.ToDouble(crew_Grooming.weight) > maxWeight)
                {
                    Weight_Status_Id = db.Crew_Grooming_WeightStatus.Where(a => a.Weight_Status == "Over Weight").Select(a => a.ID).FirstOrDefault();
                }
                else if (Convert.ToDouble(crew_Grooming.weight) < minWeight)
                {
                    Weight_Status_Id = db.Crew_Grooming_WeightStatus.Where(a => a.Weight_Status == "Under Weight").Select(a => a.ID).FirstOrDefault();
                }
                else
                {
                    Weight_Status_Id = db.Crew_Grooming_WeightStatus.Where(a => a.Weight_Status == "Healthy Weight").Select(a => a.ID).FirstOrDefault();
                }

                var eyeColor = db.Crew_Grooming_EyeColor.Where(a => a.ID == crew_Grooming.eyecolor).Select(b => b.Eye_Clr).FirstOrDefault();

                var hairColor = db.Crew_Grooming_HairColor.Where(a => a.ID == crew_Grooming.haircolor).Select(b => b.Hair_Clr).FirstOrDefault();

                var CrewDetail = db.CrewDetails.Where(a => a.StaffNumber == crew_Grooming.StaffNo).FirstOrDefault();

                // update attribute table
                if (CrewDetail != null)
                {
                    CrewGroomingAttribute crewGroomingAttribute = new CrewGroomingAttribute
                    {
                        Employee_Number = crew_Grooming.StaffNo,
                        Employee_Name = CrewDetail.FullName,
                        Eye_Color = eyeColor,
                        Hair_Color = hairColor,
                        Height = crew_Grooming.height,
                        Updated_Date = DateTime.Now,
                        Eye_Color_Cd = crew_Grooming.eyecolor,
                        Hair_Color_Cd = crew_Grooming.haircolor,
                        Nationality = CrewDetail.Nation,
                        Gender = CrewDetail.Sex,
                        Updated_By = LoggedStaffNumber

                    };


                    // UPDATE CREWWEIGHTTABLE
                    CrewWeightDetailList crewWeight = new CrewWeightDetailList
                    {
                        Employee_Number = crew_Grooming.StaffNo,
                        Employee_Name = CrewDetail.FullName,
                        Weight = crew_Grooming.weight,
                        Remarks = crew_Grooming.remarks,
                        Weight_Status_Id = Weight_Status_Id,
                        Date_Weight = DateTime.Now,
                        Date_From = DateTime.Now,
                        Date_To = DateTime.Now,
                        Is_Deleted = false,
                        Updated_By = LoggedStaffNumber
                    };

                    //CrewRemarks

                    CrewGroomingRemark crewReamrk = new CrewGroomingRemark
                    {
                        Remarks = crew_Grooming.imageRemarks,
                        StaffNo = crew_Grooming.StaffNo,
                        isDeleted = false,
                        CreatedDate = DateTime.Now,
                        CreatedBy = LoggedStaffNumber,
                        isActive = true

                    };


                    db.CrewGroomingAttributes.Add(crewGroomingAttribute);
                    db.CrewWeightDetailLists.Add(crewWeight);
                    db.CrewGroomingRemarks.Add(crewReamrk);
                    db.SaveChanges();
                }

                return RedirectToAction("CrewGroomingData", "CGAP", new { Id = crew_Grooming.StaffNo });
            }
            else
            {
                return null;
            }
        }
        [System.Web.Mvc.HttpGet]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> DeleteWeightHistory(int Id)
        {
            try
            {
                var weightHistory = db.CrewWeightDetailLists.Find(Id);

                weightHistory.Is_Deleted = true;

                db.SaveChanges();

                return RedirectToAction("CrewGroomingData", "CGAP", new { Id = weightHistory.Employee_Number });

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message); // Or use a proper logger

            }
            return null;
        }

        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> PrintGroomingInfo(string Id)
        {
            CrewGroomingPojo crewGroomingPojo = new CrewGroomingPojo();

            var crewGroomingAttribute = db.CrewGroomingAttributes.Where(a => a.Employee_Number == Id).FirstOrDefault();
            var weightHistory = db.CrewWeightDetailLists.Where(c => c.Employee_Number == Id && (c.Is_Deleted == null || c.Is_Deleted == false)).OrderByDescending(c => c.Date_Weight).ToList();
            var latestWeight = db.CrewWeightDetailLists.Where(c => c.Employee_Number == Id && (c.Is_Deleted == null || c.Is_Deleted == false)).OrderByDescending(c => c.Date_Weight).FirstOrDefault();


            // var StaffDetail = db.CrewDetails.Where(a=>a.StaffNumber == Id).FirstOrDefault();

            //var updatedUser = db.CrewDetails.Where(a=>a.StaffNumber == crewGroomingLatest.Updated_By).Select(a=>a.FullName).FirstOrDefault();

            crewGroomingPojo.StaffNo = Id;
            if (crewGroomingAttribute == null)
            {
                var StaffDetail = db.CrewDetails.Where(a => a.StaffNumber == Id).FirstOrDefault();
                if (StaffDetail != null)
                {
                    crewGroomingPojo.staffName = StaffDetail.FullName;
                    crewGroomingPojo.gender = StaffDetail.Sex;
                    crewGroomingPojo.nation = StaffDetail.Nation;
                }
            }
            double minweight = 0;
            double maxweight = 0;
            if (crewGroomingAttribute != null)
            {
                crewGroomingPojo.staffName = crewGroomingAttribute.Employee_Name;
                crewGroomingPojo.gender = crewGroomingAttribute.Gender;
                crewGroomingPojo.nation = crewGroomingAttribute.Nationality;
                crewGroomingPojo.height = (decimal)crewGroomingAttribute.Height;


                var height = crewGroomingAttribute.Height;
                if (height != null || height == 0)
                {
                    var crewGender = crewGroomingAttribute.Gender;
                    var heightWeightRatio = db.WeightMinMaxSettings
                               .Where(a => a.height == height && a.gender == crewGender && a.isactive == true && a.isdeleted == false)
                               .FirstOrDefault();
                    if (heightWeightRatio != null && heightWeightRatio.WeightVarianceSetting != null)
                    {
                        var variancePercent = heightWeightRatio.WeightVarianceSetting.variance_percentage;
                        minweight = Math.Round((double)(heightWeightRatio.min_weight * (1 - (variancePercent / 100))));
                        maxweight = Math.Round((double)(heightWeightRatio.max_weight * (1 + (variancePercent / 100))));
                    }
                }

                crewGroomingPojo.minweight = Math.Round(minweight, 2); // Round to 2 decimal places
                crewGroomingPojo.maxweight = Math.Round(minweight, 2);

                if (crewGroomingAttribute.Hair_Color_Cd != null)
                {
                    var haircolor = db.Crew_Grooming_HairColor.Where(a => a.ID == crewGroomingAttribute.Hair_Color_Cd).FirstOrDefault();
                    if (haircolor != null)
                    {
                        crewGroomingPojo.haircolorStr = haircolor.Hair_Clr;
                    }
                }
                if (crewGroomingAttribute.Eye_Color_Cd != null)
                {
                    var eyeColor = db.Crew_Grooming_EyeColor.Where(a => a.ID == crewGroomingAttribute.Eye_Color_Cd).FirstOrDefault();
                    if (eyeColor != null)
                    {
                        crewGroomingPojo.eyecolorStr = eyeColor.Eye_Clr;
                    }
                }
                // crewGroomingPojo.eyecolorStr = db.Crew_Grooming_EyeColor.Where(a => a.ID == crewGroomingAttribute.Eye_Color_Cd).Select(a => a.Eye_Clr).FirstOrDefault();
                // crewGroomingPojo.haircolorStr = db.Crew_Grooming_HairColor.Where(a => a.ID == crewGroomingAttribute.Hair_Color_Cd).Select(a => a.Hair_Clr).FirstOrDefault();
            }



            if (latestWeight != null)
            {
                if (latestWeight.Weight != null)
                {
                    crewGroomingPojo.weight = (decimal)latestWeight.Weight;
                }
                crewGroomingPojo.remarks = latestWeight.Remarks;
                crewGroomingPojo.created = latestWeight.Date_Weight;
                var weightStat = db.Crew_Grooming_WeightStatus.Where(a => a.ID == latestWeight.Weight_Status_Id).FirstOrDefault();
                if (weightStat != null)
                {
                    crewGroomingPojo.weightStat = weightStat.Weight_Status;
                }
            }
            // var crewGroomingHistory = db.Crew_Grooming.Where(a => a.StaffNumber == Id &&  a.IsDeleted == false).OrderByDescending(a=>a.Weight_Entry_Date).ToList ();


            List<CrewWeightHistoryPojo> formattedHistory = new List<CrewWeightHistoryPojo>();





            // Loop through all items
            for (int i = 0; i < weightHistory.Count; i++) // Fix: Loop until weightHistory.Count
            {
                var currentItem = weightHistory[i];
                var updatedUser = currentItem.Updated_By;
                decimal lossorgain = 0;
                if (currentItem.Gain_Loss != null)
                {
                    lossorgain = (decimal)currentItem.Gain_Loss;
                }
                String weightStatus = "";
                if (currentItem.Weight_Status_Id != null)
                {
                    var weightStatObj = db.Crew_Grooming_WeightStatus.Where(a => a.ID == currentItem.Weight_Status_Id).FirstOrDefault();
                    if (weightStatObj != null)
                    {
                        weightStatus = weightStatObj.Weight_Status;
                    }
                }

                // Calculate LossOrGain if there is a next item

                CrewWeightHistoryPojo formattedItem = new CrewWeightHistoryPojo
                {
                    Weight_Entry_Date = currentItem.Date_Weight,
                    Weight = currentItem.Weight,
                    Remarks = currentItem.Remarks,
                    weightstat = weightStatus,
                    lossorgain = lossorgain,
                    Updated_By = currentItem.Updated_By,
                    Id = currentItem.Id
                };

                formattedHistory.Add(formattedItem);
            }

            crewGroomingPojo.historylist = formattedHistory;

            //ger general remarks 

            var latestRemark = db.CrewGroomingRemarks.Where(a => a.StaffNo == Id).OrderByDescending(a => a.CreatedDate).FirstOrDefault();
            if (latestRemark != null)
            {
                var remark = latestRemark.Remarks;
                crewGroomingPojo.imageRemarks = remark;
            }

            // get past remarks 

            var remarks = db.CrewGroomingRemarks
                             .Where(r => r.StaffNo == Id && r.isActive == true && r.isDeleted == false)
                             .OrderByDescending(r => r.CreatedDate)
                             .ToList();

            crewGroomingPojo.latestRemarksList = remarks;

            return View(crewGroomingPojo);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]// Very important: this attribute specifies that it's a POST action
        public async Task<System.Web.Mvc.ActionResult> IndexPost(string StaffNumber)
        {
            // StaffNumber will contain the value entered in the input field
            if (!string.IsNullOrEmpty(StaffNumber))
            {

                var groomingAttributeDate = db.CrewGroomingAttributes.Where(a => a.Employee_Number == StaffNumber).FirstOrDefault();

                var groomingWeightList = db.CrewWeightDetailLists.Where(a => a.Employee_Number == StaffNumber).ToList();

                if (groomingAttributeDate != null || (groomingWeightList != null && groomingWeightList.Count > 0))
                {
                    return RedirectToAction("CrewGroomingData", "CGAP", new { Id = StaffNumber });
                }

                else
                {
                    // check if entered staff is a crew
                    var crewDetail = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).FirstOrDefault();
                    if (crewDetail != null)
                    {
                        return RedirectToAction("AddNewGroomingInfo", "CGAP", new { Id = StaffNumber });
                    }

                    else
                    {
                        return RedirectToAction("Index", "CGAP", new { IsCheck = 1 });
                    }
                }


            }
            return null;
        }

        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> IndexAppLetter(int? IsCheck)
        {
            var telemetry = new TelemetryClient();
            Crew_Appreciation crew_Appreciation = new Crew_Appreciation();
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
                TempData["Status"] = "Error Data";
            }
            if (IsCheck == null)
            {
                TempData["Status"] = "Loadstaff";
                return View(crew_Appreciation);
            }
            else if (IsCheck == 1)
            {
                TempData["Status"] = "NoCrew";
                return View(crew_Appreciation);
            }
            else
            {
                return View(crew_Appreciation);
            }
        }

        [System.Web.Mvc.HttpGet]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]// Very important: this attribute specifies that it's a POST action
        public async Task<System.Web.Mvc.ActionResult> AppriciationData(string StaffNumber)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            String LoggedStaffNumber = "";
            if (getUser != null)
            {
                LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;

            }
            // StaffNumber will contain the value entered in the input field

            var crew_Appreciation = db.Crew_Appreciation.Where(a => a.StaffNumber == StaffNumber && (a.IsDeleted == null || a.IsDeleted == false)).ToList();

            if (crew_Appreciation != null && crew_Appreciation.Count > 0)
            {



                TempData["LoggedUser"] = LoggedStaffNumber;
                TempData["CrewUser"] = StaffNumber;
                return View(crew_Appreciation);

            }

            else
            {
                var crewDetail = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).FirstOrDefault();
                if (crewDetail != null)
                {
                    List<Crew_Appreciation> appList = new List<Crew_Appreciation>();
                    TempData["LoggedUser"] = LoggedStaffNumber;
                    TempData["CrewUser"] = StaffNumber;
                    return View(appList);

                }
                else
                {
                    return RedirectToAction("IndexAppLetter", "CGAP", new { IsCheck = 1 });
                }

            }




        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public JsonResult AddCrewAppreciation(DateTime entryDate, string subject, string description, string staffNumber, string loggedUser, HttpPostedFileBase attachment)
        {
            try
            {
                string blobUrl = null;
                if (attachment != null && attachment.ContentLength > 0)
                {
                    // string fileName = Path.GetFileName(attachment.FileName);
                    // get connection string from settings

                    string originalFileName = Path.GetFileNameWithoutExtension(attachment.FileName);
                    // Get file extension
                    string fileExtension = Path.GetExtension(attachment.FileName);
                    // Create unique filename with timestamp
                    string uniqueFileName = $"{staffNumber}_{originalFileName}_{DateTime.Now:yyyyMMddHHmmssfff}{fileExtension}";

                    string containerName = "crewappreciationletters"; // Replace with your container name
                    // BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName); // Commented for AWS Migration
                    // containerClient.CreateIfNotExists(); // Commented for AWS Migration



                    // BlobClient blobClient = containerClient.GetBlobClient(uniqueFileName); // Commented for AWS Migration

                    // using (Stream uploadFileStream = attachment.InputStream) // Commented for AWS Migration
                    // { // Commented for AWS Migration
                    //     blobClient.Upload(uploadFileStream, true); // Commented for AWS Migration
                    // } // Commented for AWS Migration

                    // blobUrl = blobClient.Uri.ToString(); // Commented for AWS Migration
                    // --- AWS S3 Migration: S3 upload logic below ---
                    string key = $"{containerName}/{uniqueFileName}"; // S3 'folders' are just key prefixes
                    using (var fileTransferUtility = new TransferUtility(_s3Client))
                    {
                        fileTransferUtility.UploadAsync(attachment.InputStream, _bucketName, key).Wait();
                    }
                    blobUrl = $"https://{_bucketName}.s3.amazonaws.com/{key}";
                }
                var CrewDetail = db.CrewDetails.Where(a => a.StaffNumber == staffNumber).FirstOrDefault();
                String FullName = "";
                if (CrewDetail != null)
                {
                    FullName = CrewDetail.FullName;
                }
                var newAppreciation = new Crew_Appreciation
                {
                    AppEntryDate = entryDate,
                    AppEntrySubject = subject,
                    AppEntryDesc = description,
                    StaffNumber = staffNumber,
                    StaffName = FullName,
                    Updated_By = loggedUser,
                    IsActive = true,
                    IsDeleted = false,
                    AttachmentPath = blobUrl // Store the blob URL
                };

                db.Crew_Appreciation.Add(newAppreciation);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> ViewAppData(int Id)
        {
            Crew_Appreciation crew_Appreciation = new Crew_Appreciation();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    var crewAppreciation = db.Crew_Appreciation.Where(a => a.Id == Id).FirstOrDefault();
                    return View(crewAppreciation);
                }

            }
            catch (Exception ex)
            {
                return View();
            }

            return View();

        }

        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> DeleteAppData(int Id)
        {
            Crew_Appreciation crew_Appreciation = new Crew_Appreciation();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    var crewAppreciation = db.Crew_Appreciation.Where(a => a.Id == Id).FirstOrDefault();
                    crewAppreciation.IsDeleted = true;
                    db.SaveChanges();
                    return RedirectToAction("AppriciationData", "CGAP", new { StaffNumber = crewAppreciation.StaffNumber });
                }

            }
            catch (Exception ex)
            {
                return View();
            }

            return View();

        }

        // Crew Commendadtion methods
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> IndexCommLetter(int? IsCheck)
        {
            var telemetry = new TelemetryClient();
            Crew_Commendation crew_Appreciation = new Crew_Commendation();
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
                TempData["Status"] = "Error Data";
            }
            if (IsCheck == null)
            {
                TempData["Status"] = "Loadstaff";
                return View(crew_Appreciation);
            }
            else if (IsCheck == 1)
            {
                TempData["Status"] = "NoCrew";
                return View(crew_Appreciation);
            }
            else
            {
                return View(crew_Appreciation);
            }
        }

        [System.Web.Mvc.HttpGet]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]// Very important: this attribute specifies that it's a POST action
        public async Task<System.Web.Mvc.ActionResult> CommendationData(string StaffNumber)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            String LoggedStaffNumber = "";
            if (getUser != null)
            {
                LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;

            }
            // StaffNumber will contain the value entered in the input field

            var crew_Commendation = db.Crew_Commendation.Where(a => a.StaffNumber == StaffNumber && (a.IsDeleted == null || a.IsDeleted == false)).ToList();

            if (crew_Commendation != null && crew_Commendation.Count > 0)
            {



                TempData["LoggedUser"] = LoggedStaffNumber;
                TempData["CrewUser"] = StaffNumber;
                return View(crew_Commendation);

            }

            else
            {
                var crewDetail = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).FirstOrDefault();
                if (crewDetail != null)
                {
                    List<Crew_Commendation> commList = new List<Crew_Commendation>();
                    TempData["LoggedUser"] = LoggedStaffNumber;
                    TempData["CrewUser"] = StaffNumber;
                    return View(commList);

                }
                else
                {
                    return RedirectToAction("IndexCommLetter", "CGAP", new { IsCheck = 1 });
                }

            }




        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public JsonResult AddCrewCommendation(DateTime entryDate, string subject, string description, string staffNumber, string loggedUser, HttpPostedFileBase attachment)
        {
            try
            {
                string blobUrl = null;
                if (attachment != null && attachment.ContentLength > 0)
                {
                    // string fileName = Path.GetFileName(attachment.FileName);
                    // get connection string from settings
                    string originalFileName = Path.GetFileNameWithoutExtension(attachment.FileName);
                    // Get file extension
                    string fileExtension = Path.GetExtension(attachment.FileName);
                    // Create unique filename with timestamp
                    string uniqueFileName = $"{staffNumber}_{originalFileName}_{DateTime.Now:yyyyMMddHHmmssfff}{fileExtension}";


                    string containerName = "crewcommendationletters"; // Replace with your container name
                    // BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName); // Commented for AWS Migration
                    // containerClient.CreateIfNotExists(); // Commented for AWS Migration



                    // BlobClient blobClient = containerClient.GetBlobClient(uniqueFileName); // Commented for AWS Migration

                    // using (Stream uploadFileStream = attachment.InputStream) // Commented for AWS Migration
                    // { // Commented for AWS Migration
                    //     blobClient.Upload(uploadFileStream, true); // Commented for AWS Migration
                    // } // Commented for AWS Migration

                    // blobUrl = blobClient.Uri.ToString(); // Commented for AWS Migration
                    // --- AWS S3 Migration: S3 upload logic below ---
                    string key = $"{containerName}/{uniqueFileName}"; // S3 'folders' are just key prefixes
                    using (var fileTransferUtility = new TransferUtility(_s3Client))
                    {
                        fileTransferUtility.UploadAsync(attachment.InputStream, _bucketName, key).Wait();
                    }
                    blobUrl = $"https://{_bucketName}.s3.amazonaws.com/{key}";
                }
                var CrewDetail = db.CrewDetails.Where(a => a.StaffNumber == staffNumber).FirstOrDefault();
                String FullName = "";
                if (CrewDetail != null)
                {
                    FullName = CrewDetail.FullName;
                }
                var newCommendation = new Crew_Commendation
                {
                    CommEntryDate = entryDate,
                    CommEntrySubject = subject,
                    CommEntryDesc = description,
                    StaffNumber = staffNumber,
                    StaffName = FullName,
                    Updated_By = loggedUser,
                    IsActive = true,
                    IsDeleted = false,
                    AttachmentPath = blobUrl // Store the blob URL
                };

                db.Crew_Commendation.Add(newCommendation);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> ViewCommData(int Id)
        {
            Crew_Appreciation crew_Appreciation = new Crew_Appreciation();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    var crewCommendation = db.Crew_Commendation.Where(a => a.Id == Id).FirstOrDefault();
                    return View(crewCommendation);
                }

            }
            catch (Exception ex)
            {
                return View();
            }

            return View();

        }
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> DeleteCommData(int Id)
        {
            Crew_Appreciation crew_Appreciation = new Crew_Appreciation();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    var crewAppreciation = db.Crew_Commendation.Where(a => a.Id == Id).FirstOrDefault();
                    crewAppreciation.IsDeleted = true;
                    db.SaveChanges();
                    return RedirectToAction("CommendationData", "CGAP", new { StaffNumber = crewAppreciation.StaffNumber });
                }

            }
            catch (Exception ex)
            {
                return View();
            }

            return View();

        }
        // view warning letters

        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> IndexWarnLetter(int? IsCheck)
        {
            var telemetry = new TelemetryClient();
            Crew_WarningLetter crew_Warning = new Crew_WarningLetter();
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
                TempData["Status"] = "Error Data";
            }
            if (IsCheck == null)
            {
                TempData["Status"] = "Loadstaff";
                return View(crew_Warning);
            }
            else if (IsCheck == 1)
            {
                TempData["Status"] = "NoCrew";
                return View(crew_Warning);
            }
            else
            {
                return View(crew_Warning);
            }
        }

        [System.Web.Mvc.HttpGet]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]// Very important: this attribute specifies that it's a POST action
        public async Task<System.Web.Mvc.ActionResult> WarningLetterData(string StaffNumber)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            String LoggedStaffNumber = "";
            if (getUser != null)
            {
                LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;

            }
            // StaffNumber will contain the value entered in the input field

            var crew_Warning = db.Crew_WarningLetter.Where(a => a.StaffNumber == StaffNumber && (a.IsDeleted == null || a.IsDeleted == false)).ToList();

            if (crew_Warning != null && crew_Warning.Count > 0)
            {



                TempData["LoggedUser"] = LoggedStaffNumber;
                TempData["CrewUser"] = StaffNumber;
                return View(crew_Warning);

            }

            else
            {
                var crewDetail = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).FirstOrDefault();
                if (crewDetail != null)
                {
                    List<Crew_WarningLetter> warnList = new List<Crew_WarningLetter>();
                    TempData["LoggedUser"] = LoggedStaffNumber;
                    TempData["CrewUser"] = StaffNumber;
                    return View(warnList);

                }
                else
                {
                    return RedirectToAction("IndexWarnLetter", "CGAP", new { IsCheck = 1 });
                }

            }




        }


        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins, CSM.CrewGrooming")]
        public JsonResult AddCrewWarning(DateTime entryDate, string subject, string description, string staffNumber, string loggedUser, HttpPostedFileBase attachment, string letterType)
        {
            try
            {
                string blobUrl = null;
                if (attachment != null && attachment.ContentLength > 0)
                {
                    //  string fileName = Path.GetFileName(attachment.FileName);
                    // get connection string from settings
                    string originalFileName = Path.GetFileNameWithoutExtension(attachment.FileName);
                    // Get file extension
                    string fileExtension = Path.GetExtension(attachment.FileName);
                    // Create unique filename with timestamp
                    string uniqueFileName = $"{staffNumber}_{originalFileName}_{DateTime.Now:yyyyMMddHHmmssfff}{fileExtension}";

                    string containerName = "crewwarningletters"; // Replace with your container name
                    // BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName); // Commented for AWS Migration
                    // containerClient.CreateIfNotExists(); // Commented for AWS Migration



                    // BlobClient blobClient = containerClient.GetBlobClient(uniqueFileName); // Commented for AWS Migration

                    // using (Stream uploadFileStream = attachment.InputStream) // Commented for AWS Migration
                    // { // Commented for AWS Migration
                    //     blobClient.Upload(uploadFileStream, true); // Commented for AWS Migration
                    // } // Commented for AWS Migration

                    // blobUrl = blobClient.Uri.ToString(); // Commented for AWS Migration
                    // --- AWS S3 Migration: S3 upload logic below ---
                    string key = $"{containerName}/{uniqueFileName}"; // S3 'folders' are just key prefixes
                    using (var fileTransferUtility = new TransferUtility(_s3Client))
                    {
                        fileTransferUtility.UploadAsync(attachment.InputStream, _bucketName, key).Wait();
                    }
                    blobUrl = $"https://{_bucketName}.s3.amazonaws.com/{key}";
                }
                var CrewDetail = db.CrewDetails.Where(a => a.StaffNumber == staffNumber).FirstOrDefault();
                String FullName = "";
                if (CrewDetail != null)
                {
                    FullName = CrewDetail.FullName;
                }
                var newCommendation = new Crew_WarningLetter
                {
                    WarnEntryDate = entryDate,
                    WarnEntrySubject = subject,
                    WarnEntryType = letterType,
                    StaffNumber = staffNumber,
                    StaffName = FullName,
                    Updated_By = loggedUser,
                    Reported_By = loggedUser,
                    IsActive = true,
                    IsDeleted = false,
                    WarnEntryDesc = description,
                    AttachmentPath = blobUrl // Store the blob URL
                };

                db.Crew_WarningLetter.Add(newCommendation);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> ViewWarnData(int Id)
        {
            Crew_Appreciation crew_Appreciation = new Crew_Appreciation();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    var crewWarning = db.Crew_WarningLetter.Where(a => a.Id == Id).FirstOrDefault();
                    return View(crewWarning);
                }

            }
            catch (Exception ex)
            {
                return View();
            }

            return View();

        }
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> DeleteWarnData(int Id)
        {
            Crew_Appreciation crew_Appreciation = new Crew_Appreciation();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                    var crewWarning = db.Crew_WarningLetter.Where(a => a.Id == Id).FirstOrDefault();
                    crewWarning.IsDeleted = true;
                    db.SaveChanges();
                    return RedirectToAction("WarningLetterData", "CGAP", new { StaffNumber = crewWarning.StaffNumber });
                }

            }
            catch (Exception ex)
            {
                return View();
            }

            return View();

        }
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public JsonResult GetWeightRangeByHeight(decimal height, string gender)
        {
            using (var db = new CSMEntities())
            {
                var weightRange = db.WeightMinMaxSettings
                    .Where(x => x.height == height &&
                           (x.gender == gender || x.gender == null) &&
                           x.isactive == true &&
                           x.isdeleted == false)
                    .OrderBy(x => x.gender) // Prefer gender-specific records
                    .FirstOrDefault();

                if (weightRange != null)
                {
                    return Json(new
                    {
                        success = true,
                        minWeight = weightRange.min_weight,
                        maxWeight = weightRange.max_weight
                    }, JsonRequestBehavior.AllowGet);
                }

                // Return false if no matching record found
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public JsonResult GetVariancePercentage(string gender)
        {
            using (var db = new CSMEntities())
            {
                // Try to get gender-specific variance first
                var varianceSetting = db.WeightVarianceSettings
                    .Where(x => x.isactive == true || x.isdeleted == false).FirstOrDefault();

                if (varianceSetting != null)
                {
                    return Json(new
                    {
                        percentage = varianceSetting.variance_percentage
                    }, JsonRequestBehavior.AllowGet);
                }

                // Default variance if none found
                return Json(new { percentage = 15 }, JsonRequestBehavior.AllowGet);
            }
        }
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> ViewMinMaxWeightChart()
        {
            Crew_Appreciation crew_Appreciation = new Crew_Appreciation();
            try
            {

                var minmaxweightlist = db.WeightMinMaxSettings.Where(a => a.isactive == true && a.isdeleted == false).ToList();
                return View(minmaxweightlist);


            }
            catch (Exception ex)
            {
                return View();
            }



        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> UpdateWeightRange(int id, decimal height, string gender, decimal minWeight, decimal maxWeight)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
            String LoggedStaffNumber = "";
            if (getUser != null)
            {
                LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;

            }
            var existingSetting = db.WeightMinMaxSettings.FirstOrDefault(w => w.id == id);

            if (existingSetting == null)
            {
                return Json(new { success = false, message = "Weight range record not found." });
            }

            existingSetting.isactive = false;
            existingSetting.modified_at = DateTime.Now;
            db.SaveChanges();

            WeightMinMaxSetting newSetting = new WeightMinMaxSetting();
            newSetting.height = height;
            newSetting.gender = gender;
            newSetting.min_weight = minWeight;
            newSetting.max_weight = maxWeight;
            newSetting.created_at = DateTime.Now;
            newSetting.created_by = LoggedStaffNumber;
            newSetting.isactive = true;
            newSetting.isdeleted = false;
            newSetting.variance_settings_id = existingSetting.variance_settings_id;
            db.WeightMinMaxSettings.Add(newSetting);


            // Save changes
            db.SaveChanges();

            return Json(new { success = true });

        }

        [System.Web.Mvc.HttpGet]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<JsonResult> GetActiveVariance()
        {
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                String LoggedStaffNumber = "";
                if (getUser != null)
                {
                    LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;

                }

                var variance = db.WeightVarianceSettings
                    .FirstOrDefault(a => a.isactive == true && a.isdeleted == false);

                if (variance == null)
                {
                    return Json(new { success = false, message = "No active variance setting found" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = variance.id,
                        variance_percentage = variance.variance_percentage,
                        description = variance.description
                    }
                }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<JsonResult> UpdateVariance(int id, decimal variancePercentage, string description)
        {
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                String LoggedStaffNumber = "";
                if (getUser != null)
                {
                    LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;

                }
                using (var db = new CSMEntities())
                {
                    var variance = db.WeightVarianceSettings.Find(id);
                    if (variance == null)
                    {
                        return Json(new { success = false, message = "Variance setting not found" });
                    }

                    variance.isactive = false;
                    variance.isdeleted = false;


                    db.SaveChanges();

                    WeightVarianceSetting setting = new WeightVarianceSetting();
                    setting.isactive = true;
                    setting.isdeleted = false;
                    setting.description = description;
                    setting.variance_percentage = variancePercentage;
                    db.WeightVarianceSettings.Add(setting);
                    db.SaveChanges();
                    // change current active ratios to this new varaince

                    var WeightMinMaxList = db.WeightMinMaxSettings.Where(a => a.isactive == true && a.isdeleted == false).ToList();
                    WeightMinMaxSetting newMinMax;
                    foreach (var minmaxVal in WeightMinMaxList)
                    {
                        minmaxVal.isactive = false;
                        minmaxVal.isdeleted = false;
                        minmaxVal.modified_at = DateTime.Now;
                        minmaxVal.modified_by = LoggedStaffNumber;
                        db.SaveChanges();
                        newMinMax = new WeightMinMaxSetting();
                        newMinMax.height = minmaxVal.height;
                        newMinMax.min_weight = minmaxVal.min_weight;
                        newMinMax.max_weight = minmaxVal.max_weight;
                        newMinMax.isactive = true;
                        newMinMax.isdeleted = false;
                        newMinMax.description = minmaxVal.description;
                        newMinMax.gender = minmaxVal.gender;
                        newMinMax.created_at = DateTime.Now;
                        newMinMax.created_by = LoggedStaffNumber;
                        newMinMax.variance_settings_id = setting.id;
                        db.WeightMinMaxSettings.Add(newMinMax);

                    }


                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public JsonResult AddWeightRange([FromBody] WeightMinMaxSetting model)
        {
            try
            {
                // Validate model
                if (ModelState.IsValid)
                {
                    // Check if this height/gender combination already exists
                    var existing = db.WeightMinMaxSettings
                        .FirstOrDefault(w => w.height == model.height && w.gender == model.gender && w.isactive == true && w.isdeleted == false);

                    if (existing != null)
                    {
                        return Json(new { success = false, message = "This height/gender combination already exists" });
                    }

                    // Create new entry
                    var newEntry = new WeightMinMaxSetting
                    {
                        height = model.height,
                        gender = model.gender,
                        min_weight = model.min_weight,
                        max_weight = model.max_weight,
                        description = model.description,
                        isactive = true,
                        isdeleted = false,
                        created_at = DateTime.Now,
                        created_by = User.Identity.Name,
                        variance_settings_id = db.WeightVarianceSettings
                            .Where(v => v.isactive == true && v.isdeleted == false)
                            .Select(v => v.id)
                            .FirstOrDefault()
                    };

                    db.WeightMinMaxSettings.Add(newEntry);
                    db.SaveChanges();

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Invalid data" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public JsonResult DeleteWeightRange(int id)
        {
            try
            {
                var record = db.WeightMinMaxSettings.Find(id);
                if (record == null)
                {
                    return Json(new { success = false, message = "Record not found" });
                }

                // Soft delete (set isdeleted flag)
                record.isactive = false;
                record.isdeleted = true;
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [Authorize(Roles = "CSM.Admins,CSM.CrewGrooming")]
        public async Task<System.Web.Mvc.ActionResult> CrewGroomingData(String Id)
        {
            var telemetry = new TelemetryClient();
            Crew_Grooming crew_Grooming = new Crew_Grooming();
            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());
                String LoggedStaffNumber = "";
                if (getUser != null)
                {
                    LoggedStaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                // Get crew details to determine gender
                //  var crewDetail = db.CrewDetails.Where(a => a.StaffNumber == Id).FirstOrDefault();
                //  var crewGender = crewDetail?.Sex ?? "M"; // Default to Male if not found

                // Get height options based on gender
                /*  var heightValues = db.WeightMinMaxSettings
                      .Where(x => x.gender == crewGender && x.isactive == true && x.isdeleted == false)
                      .Select(x => x.height)
                      .Distinct()
                      .ToList();

                  var heightOptions = heightValues
                      .OrderBy(h => h)
                      .Select(h => new
                      {
                          Height = h,
                          DisplayText = h.HasValue ? h.Value.ToString("0.00") + "m" : "N/A"
                      })
                      .ToList();*/

                // Rest of your existing code...
                var data = db.CrewGroomingAttributes.Where(a => a.Employee_Number == Id).FirstOrDefault();
                var latestWeight = db.CrewWeightDetailLists.Where(a => a.Employee_Number == Id && (a.Is_Deleted == null || a.Is_Deleted == false))
                                    .OrderByDescending(a => a.Date_From).FirstOrDefault();
                var crewGender = "M";
                if (data == null)
                {
                    // check in crewdetail table 
                    var crewDetail = db.CrewDetails.Where(a => a.StaffNumber == Id).FirstOrDefault();

                    if (crewDetail != null)
                    {
                        crewGender = crewDetail.Sex ?? "M";

                    }
                }
                else
                {

                    crewGender = data.Gender;
                }
                var heightValues = db.WeightMinMaxSettings
                .Where(x => x.gender == crewGender && x.isactive == true && x.isdeleted == false)
                .Select(x => x.height)
                .Distinct()
                .ToList();

                var heightOptions = heightValues
                    .OrderBy(h => h)
                    .Select(h => new
                    {
                        Height = h,
                        DisplayText = h.HasValue ? h.Value.ToString("0.00") + "m" : "N/A"
                    })
                    .ToList();

                CrewGroomingPojo groomingdata = new CrewGroomingPojo();
                if (data != null)
                {

                    groomingdata.StaffNo = data.Employee_Number;
                    groomingdata.staffName = data.Employee_Name;
                    if (data.Height.HasValue)
                    {
                        groomingdata.height = (decimal)data.Height;
                        decimal height = (decimal)data.Height;
                        double minweight = 0;
                        double maxweight = 0;

                        //if (crewDetail != null)
                        // {
                        var heightWeightRatio = db.WeightMinMaxSettings
                            .Where(a => a.height == height && a.gender == crewGender && a.isactive == true && a.isdeleted == false)
                            .FirstOrDefault();

                        if (heightWeightRatio != null && heightWeightRatio.WeightVarianceSetting != null)
                        {
                            var variancePercent = heightWeightRatio.WeightVarianceSetting.variance_percentage;
                            minweight = Math.Round((double)(heightWeightRatio.min_weight * (1 - (variancePercent / 100))));
                            maxweight = Math.Round((double)(heightWeightRatio.max_weight * (1 + (variancePercent / 100))));
                        }
                        // }

                        groomingdata.minweight = minweight;
                        groomingdata.maxweight = maxweight;
                    }

                    groomingdata.gender = data.Gender;
                    groomingdata.nation = data.Nationality;

                    // Create SelectList with selected value for height
                    ViewBag.HeightOptions = new SelectList(heightOptions, "Height", "DisplayText", data.Height ?? null);

                    // Eye Color Dropdown
                    if (data.Eye_Color_Cd.HasValue)
                    {
                        groomingdata.eyecolor = data.Eye_Color_Cd.Value;
                        ViewBag.EyeColorList = new SelectList(db.Crew_Grooming_EyeColor.ToList(), "ID", "Eye_Clr", groomingdata.eyecolor);
                    }
                    else
                    {
                        ViewBag.EyeColorList = new SelectList(db.Crew_Grooming_EyeColor.ToList(), "ID", "Eye_Clr");
                    }

                    // Hair Color Dropdown
                    if (data.Hair_Color_Cd.HasValue)
                    {
                        groomingdata.haircolor = data.Hair_Color_Cd.Value;
                        ViewBag.HairColorList = new SelectList(db.Crew_Grooming_HairColor.ToList(), "ID", "Hair_Clr", groomingdata.haircolor);
                    }
                    else
                    {
                        ViewBag.HairColorList = new SelectList(db.Crew_Grooming_HairColor.ToList(), "ID", "Hair_Clr");
                    }
                }
                else
                {
                    ViewBag.HeightOptions = new SelectList(heightOptions, "Height", "DisplayText");
                    ViewBag.EyeColorList = new SelectList(db.Crew_Grooming_EyeColor.ToList(), "ID", "Eye_Clr");
                    ViewBag.HairColorList = new SelectList(db.Crew_Grooming_HairColor.ToList(), "ID", "Hair_Clr");
                    var crewDetail = db.CrewDetails.Where(a => a.StaffNumber == Id).FirstOrDefault();
                    if (crewDetail != null)
                    {
                        groomingdata.staffName = crewDetail.FullName;
                        groomingdata.nation = crewDetail.Nation;
                        // groomingdata.category = data.;
                        groomingdata.StaffNo = Id;
                        groomingdata.gender = crewDetail.Sex;
                    }
                    else
                    {
                        groomingdata.staffName = "Not an active crew";
                        groomingdata.nation = "No data";
                        // groomingdata.category = data.;
                        groomingdata.StaffNo = Id;
                        groomingdata.gender = "No data";
                    }
                }

                if (latestWeight != null)
                {
                    if (latestWeight.Weight_Status_Id != null)
                    {
                        groomingdata.weightStat = db.Crew_Grooming_WeightStatus
                            .Where(a => a.ID == latestWeight.Weight_Status_Id)
                            .Select(a => a.Weight_Status)
                            .FirstOrDefault();
                    }
                    if (latestWeight.Weight != null)
                    {
                        groomingdata.weight = (decimal)latestWeight.Weight;
                    }
                    groomingdata.remarks = latestWeight.Remarks;
                    groomingdata.created = latestWeight.Date_Weight;
                }

                TempData["LoggedUser"] = LoggedStaffNumber;
                TempData["CrewNo"] = Id;
                var crewPhoto = db.CrewPhotos.Where(a => a.StaffNumber == Id).FirstOrDefault();

                string imagepath = "";

                if (crewPhoto != null)
                {
                    // If a CrewPhoto object was found, then safely access its AttachmentPath
                    // Use null-coalescing here too, in case AttachmentPath itself is null in the DB
                    imagepath = crewPhoto.AttachmentPath;
                }


                groomingdata.imagepath = imagepath;

                groomingdata.StaffNo = Id;
                // get General remarks

                var latestRemark = db.CrewGroomingRemarks.Where(a => a.StaffNo == Id).OrderByDescending(a => a.CreatedDate).FirstOrDefault();
                if (latestRemark != null)
                {
                    var remark = latestRemark.Remarks;
                    groomingdata.imageRemarks = remark;
                }
                return View(groomingdata);
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";
                return View();
            }
        }

        [System.Web.Mvc.HttpPost]
        public JsonResult SaveImageRemarks(string StaffNumber, string Remarks, string LoggedUser)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(StaffNumber) || string.IsNullOrWhiteSpace(Remarks))
                {
                    return Json(new { success = false, message = "Staff number and remarks are required." });
                }

                // Create new remark entity
                var newRemark = new CrewGroomingRemark
                {
                    StaffNo = StaffNumber,
                    Remarks = Remarks.Trim(),
                    CreatedBy = LoggedUser ?? "SYSTEM",
                    CreatedDate = DateTime.Now,
                    isActive = true,
                    isDeleted = false,
                };

                // Save to database
                db.CrewGroomingRemarks.Add(newRemark);
                db.SaveChanges();

                // Return success
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Trace.TraceError($"Error saving image remarks: {ex.Message}");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving remarks.",
                    error = ex.Message
                });
            }
        }

        [System.Web.Mvc.HttpGet]
        public JsonResult GetImageRemarks(string StaffNumber)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(StaffNumber))
                {
                    return Json(new { error = "Staff number is required." }, JsonRequestBehavior.AllowGet);
                }

                // Get remarks from database
                var remarks = db.CrewGroomingRemarks
                               .Where(r => r.StaffNo == StaffNumber && r.isActive == true && r.isDeleted == false)
                               .OrderByDescending(r => r.CreatedDate)
                               .ToList();

                // Format response
                var result = remarks.Select(r => new
                {
                    Remarks = r.Remarks,
                    CreatedBy = r.CreatedBy,
                    CreatedDate = r.CreatedDate.ToString("dd MMM yyyy HH:mm"),
                    SortableDate = r.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                });

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Trace.TraceError($"Error getting image remarks: {ex.Message}");

                return Json(new
                {
                    error = "An error occurred while retrieving remarks.",
                    details = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }



    }
}


