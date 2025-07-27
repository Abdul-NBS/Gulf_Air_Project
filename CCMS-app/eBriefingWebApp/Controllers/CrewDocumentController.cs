using Azure.Identity;
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
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ExternalConnectors;
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
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.UI.WebControls;
using Microsoft.Graph.Users.Item.SendMail;
using ActionResult = System.Web.Mvc.ActionResult;
using FormCollection = System.Web.Mvc.FormCollection;
using System.Configuration;
using System.Net.Http;
using System.Threading;
using Microsoft.Graph.Models.ODataErrors;
using Azure.Core;
using System.Net;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Net.Http.Headers;
using Microsoft.Kiota.Abstractions;
using Amazon;

namespace eBriefingWebApp.Controllers
{
    public class CrewDocumentController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;
        // private readonly BlobServiceClient _blobServiceClient;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;


        public CrewDocumentController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
            // _blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=ebriefingaims;AccountKey=n33PmfYsfpNf0GepSGew03MnHy1LENUN/PdCdrb8iMq2yKO609R9eoH7b3TqaRIZu1NZ18XBV4Gi+AStnIWJ9Q==;EndpointSuffix=core.windows.net");
            var region = System.Configuration.ConfigurationManager.AppSettings["AWSRegion"];
            _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));
            _bucketName = System.Configuration.ConfigurationManager.AppSettings["S3BucketName"];
        }

        CSMEntities db = new CSMEntities();

        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<System.Web.Mvc.ActionResult> CrewDocumentCategories()
        {
            var DocumentCategories = db.CrewDocumentCategories.Where(a => a.IsDeleted == false).ToList();
            // (TempData["From_Block_Date"]) = rosterData.FROM_DATE;

            return View(DocumentCategories);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<System.Web.Mvc.ActionResult> ChangeCategoryStatus(int id, bool isActive)
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

                var category = db.CrewDocumentCategories.Find(id);
                if (category != null)
                {
                    category.IsActive = isActive;
                    category.Updated_DT = DateTime.UtcNow;
                    category.Updated_By = StaffNumber;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Category not found." });
            }
            catch (Exception ex)
            {
                // Log the error (consider using a logging framework)
                Console.WriteLine($"Error updating category status: {ex.Message}");
                return Json(new { success = false, message = "Error updating status." });
            }
        }
        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<System.Web.Mvc.ActionResult> AddDocumentCategory(string category, string description)
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

                var newCategory = new CrewDocumentCategory
                {
                    CategoryName = category,
                    CategoryDescription = description,
                    Inserted_By = StaffNumber,
                    Insertion_DT = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false,
                    // Store the blob URL
                };

                db.CrewDocumentCategories.Add(newCategory);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<System.Web.Mvc.ActionResult> DocumentForReview()
        {
            var DocsToReview = db.PilotCrewDocuments.Where(a => a.IsActive == true && a.IsDeleted == false && (a.Status == "S" || a.Status == "C")).OrderByDescending(a => a.Id).ToList();

            return View(DocsToReview);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<ActionResult> ApproveDocument(int Id, string AdminComments)
        {
            try
            {
                using (var db = new eBriefingWebApp.CSMEntities()) // Use a using block for proper disposal
                {
                    var docToReview = await db.PilotCrewDocuments.FindAsync(Id); // Use FindAsync for asynchronous operation

                    if (docToReview == null)
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

                    docToReview.Status = "A";
                    docToReview.Admin_Reason = AdminComments;
                    docToReview.Updated_Dt = DateTime.Now;
                    docToReview.Updated_By = StaffNumber;

                    await db.SaveChangesAsync();
                    PilotCrewDocumentStatusHistory pilotCrewDocumentStatusHistory = new PilotCrewDocumentStatusHistory
                    {
                        Document_Id = docToReview.Id, // Get the generated Id from the newly added PilotCrewDocument
                        StaffNumber = StaffNumber,
                        Comments = AdminComments,
                        Status = docToReview.Status,
                        Change_DT = docToReview.Updated_Dt,
                        Attachment_Path = docToReview.Attachment_Path
                    };

                    db.PilotCrewDocumentStatusHistories.Add(pilotCrewDocumentStatusHistory);
                    await db.SaveChangesAsync();// Use SaveChangesAsync for asynchronous operation
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

        /*   [System.Web.Mvc.HttpPost]
         //  [Authorize(Roles = "CSM.DocumentApprover")]
           public async Task<ActionResult> RejectDocument(int Id, string AdminComments)
           {
               try
               {
                   using (var db = new eBriefingWebApp.CSMEntities()) // Use a using block for proper disposal
                   {
                       var docToReview = await db.PilotCrewDocuments.FindAsync(Id); // Use FindAsync for asynchronous operation

                       if (docToReview == null)
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

                       docToReview.Status = "R";
                       docToReview.Admin_Reason = AdminComments;
                       docToReview.Updated_Dt = DateTime.Now;
                       docToReview.Updated_By = StaffNumber;

                       SendRejectedmail(docToReview);


                       await db.SaveChangesAsync(); // Use SaveChangesAsync for asynchronous operation


                       PilotCrewDocumentStatusHistory pilotCrewDocumentStatusHistory = new PilotCrewDocumentStatusHistory
                       {
                           Document_Id = docToReview.Id, // Get the generated Id from the newly added PilotCrewDocument
                           StaffNumber = StaffNumber,
                           Comments = AdminComments,
                           Status = docToReview.Status,
                           Change_DT = docToReview.Updated_Dt,
                           Attachment_Path = docToReview.Attachment_Path
                       };

                       db.PilotCrewDocumentStatusHistories.Add(pilotCrewDocumentStatusHistory);
                       await db.SaveChangesAsync();
                       SendRejectedmail(docToReview);
                       return Json(new { success = true }); // Return JSON for AJAX success
                   }
               }
               catch (Exception ex)
               {
                   // Log the exception for debugging
                   System.Diagnostics.Debug.WriteLine(ex.Message); // Or use a proper logging framework

                   // Return a JSON error response with a user-friendly message (do NOT expose raw exception details in production)
                   return Json(new { success = false, message = "An error occurred while approving the leave request." });
               }
           }*/

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.DocumentApprover")]
        public ActionResult GetCrewWiseDocumentList(String StaffNumber)
        {


            var crewExists = db.PilotCrewDetails.Any(c => c.StaffNumber == StaffNumber);

            if (!crewExists)
            {
                TempData["InvalidUser"] = true;
                return RedirectToAction("GetStaffWise", "CrewDocument"); // Redirect back to the same view
            }

            List<PilotCrewDocument> crewDocuments;

            // check entered staff is pilot or crew 


            try
            {
                crewDocuments = db.PilotCrewDocuments
                                    .Where(d => d.StaffNumber == StaffNumber && d.IsActive == true && d.IsDeleted == false)
                                    .ToList();

                // You might want to eagerly load related data like CrewDocumentCategory
                foreach (var doc in crewDocuments)
                {
                    // Assuming CrewDocumentCategory is a navigation property
                    db.Entry(doc).Reference(d => d.CrewDocumentCategory).Load();
                }
                TempData["ShowPastRecordsButton"] = true;
                return View("GetStaffWise", crewDocuments);
            }
            catch (Exception ex)
            {
                // Log the error
                // ModelState.AddModelError("", "Error loading data.");
                TempData["Status"] = "Error";
                return View("YourViewName"); // Redirect back to the same view with an error message
            }




            // Pass the list of documents to the view
        }

        [Authorize(Roles = "CSM.DocumentApprover")]
        public ActionResult GetStaffWise()
        {
            List<PilotCrewDocument> pilotlist = new List<PilotCrewDocument>();
            return View(pilotlist);
        }
        [Authorize(Roles = "CSM.DocumentApprover")]
        public JsonResult GetDocumentHistory(int documentId)
        {
            try
            {
                var history = db.PilotCrewDocumentStatusHistories
                    .Where(h => h.Document_Id == documentId)
                    .OrderByDescending(h => h.Change_DT)
                    .Select(h => new
                    {
                        h.Id,
                        h.Status,
                        h.Change_DT,
                        h.Comments,
                        h.Attachment_Path,
                        IsRejected = (h.Status == "R"),
                        HasRejectionReasons = db.PilotCrewDocRejectReasons.Any(r => r.History_Id == h.Id) // Check if reasons exist
                    })
                    .ToList();

                return Json(history.Select(h => new
                {
                    h.Id,
                    h.Status,
                    h.Change_DT,
                    h.Comments,
                    h.IsRejected,
                    h.HasRejectionReasons,
                    AttachmentPath = h.Attachment_Path,
                    AttachmentUrl = Url.Content(h.Attachment_Path)
                }), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error fetching history." }, JsonRequestBehavior.AllowGet);
            }
        }
        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<System.Web.Mvc.ActionResult> DeleteCategory(int id)
        {


            var existingCategories = db.CrewDocumentCategories.Where(a => a.Id == id).FirstOrDefault();
            if (existingCategories != null)
            {
                existingCategories.IsDeleted = true;
                existingCategories.IsActive = false;
            }
            await db.SaveChangesAsync();

            return RedirectToAction("CrewDocumentCategories", "CrewDocument");



        }
        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<System.Web.Mvc.ActionResult> CrewDocumentsByDate(DateTime? From_Block_Date, DateTime? To_Block_Date)
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
            var leaveReq = db.PilotCrewDocuments.Where(a => a.IsActive == true && a.IsDeleted == false && DbFunctions.TruncateTime(a.Submited_DT) >= DbFunctions.TruncateTime(From_Block_Date)
                                         && DbFunctions.TruncateTime(a.Submited_DT) <= DbFunctions.TruncateTime(To_Block_Date)).ToList();
            TempData["From_Block_Date"] = From_Block_Date;
            TempData["To_Block_Date"] = To_Block_Date;
            return View(leaveReq);
        }

        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<System.Web.Mvc.ActionResult> CrewDocumentsByDatePost(DateTime From_Block_Date, DateTime To_Block_Date)
        {
            var telemetry = new TelemetryClient();
            try
            {
                return RedirectToAction("CrewDocumentsByDate", "CrewDocument", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                TempData["Status"] = "Error Data";
            }
            return RedirectToAction("CrewDocumentsByDate", "CrewDocument", new { From_Block_Date = From_Block_Date, To_Block_Date = To_Block_Date });
        }
        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<System.Web.Mvc.ActionResult> ShowAllHistoryDocuments(string StaffNumber)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());


            var CrewDocuments = db.PilotCrewDocuments.Where(a => a.StaffNumber == StaffNumber && a.IsDeleted == false && a.IsActive == false)
                .OrderByDescending(a => a.Submited_DT).ToList();
            TempData["StaffNumber"] = StaffNumber;
            // ViewBag.CategoryId = new SelectList(crewDocumentCategories, "Id", "CategoryName");
            return View(CrewDocuments);
        }

        [Authorize(Roles = "CSM.DocumentApprover")]
        public void GetCrewWiseDocuments(String StaffNumber)
        {


            try
            {
                GetCrewWiseDocumentList(StaffNumber);
            }
            catch (Exception ex)
            {
                // Log the error
                // ModelState.AddModelError("", "Error loading data.");
                TempData["Status"] = "Error";
                // return View("GetStaffWise");// Redirect back to the same view with an error message
            }


        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.DocumentApprover")]
        public JsonResult GetPastRecords(string StaffNumber)
        {
            try
            {
                var pastRecords = db.PilotCrewDocuments
                    .Where(d => d.StaffNumber == StaffNumber && (d.IsActive == false || d.IsDeleted == true))
                    .OrderByDescending(d => d.Submited_DT)
                    .Select(d => new {
                        d.StaffNumber,
                        FullName = d.PilotCrewDetail.FullName,
                        CategoryName = d.CrewDocumentCategory.CategoryName,
                        d.Submited_DT,
                        d.Status,
                        d.Attachment_Path,
                        d.Id
                    })
                    .ToList();

                return Json(pastRecords, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [System.Web.Mvc.HttpGet]
        public JsonResult GetRejectReasons()
        {
            using (var db = new CSMEntities())
            {
                var reasons = db.DocumentRejectReasons
                               .Where(r => r.IsActive && !r.IsDeleted)
                               .Select(r => new {
                                   r.Id,
                                   r.Description
                               })
                               .ToList();
                return Json(reasons, JsonRequestBehavior.AllowGet);
            }
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.DocumentApprover")]
        public async Task<ActionResult> RejectDocument(int Id, string AdminComments, List<int> RejectReasonIds)
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
                DateTime updateTime = DateTime.Now;
                using (var db = new CSMEntities())
                {
                    var document = db.PilotCrewDocuments.Find(Id);
                    PilotCrewDocumentStatusHistory pilotCrewDocumentStatusHistory = new PilotCrewDocumentStatusHistory
                    {
                        Document_Id = document.Id, // Get the generated Id from the newly added PilotCrewDocument
                        StaffNumber = StaffNumber,
                        Comments = AdminComments,
                        Status = "R",
                        Change_DT = updateTime,
                        Attachment_Path = document.Attachment_Path
                    };
                    db.PilotCrewDocumentStatusHistories.Add(pilotCrewDocumentStatusHistory);
                    db.SaveChanges();

                    if (document != null)
                    {
                        document.Status = "R";
                        document.Admin_Reason = AdminComments;
                        document.Updated_Dt = updateTime;
                        db.SaveChanges();
                        // Add rejection reasons if any
                        if (RejectReasonIds != null && RejectReasonIds.Any())
                        {
                            PilotCrewDocRejectReason crewRejectRecord;
                            foreach (var reasonId in RejectReasonIds)
                            {
                                crewRejectRecord = new PilotCrewDocRejectReason();
                                crewRejectRecord.DocId = document.Id;
                                crewRejectRecord.ReasonId = reasonId;
                                crewRejectRecord.IsActive = true;
                                crewRejectRecord.IsDeleted = false;
                                crewRejectRecord.History_Id = pilotCrewDocumentStatusHistory.Id;
                                db.PilotCrewDocRejectReasons.Add(crewRejectRecord);
                                db.SaveChanges();
                            }
                        }

                        await SendRejectedMail(document, RejectReasonIds);
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /*  private void SendRejectedMail(PilotCrewDocument docToReview, List<int> rejectReasonIds)
          {
              string fileCategory = docToReview.CrewDocumentCategory?.CategoryName ?? "Document";
              string adminComment = docToReview.Admin_Reason ?? string.Empty;
              string crewStaffId = docToReview.StaffNumber;

              // String mailId = "Varada.Nellayikunnath@gulfairgroup.bh";
              String mailId = crewStaffId + "@gulfair.com";
              string fromEmail = "Pilots.Documents@gulfair.com";
              string smtpServer = "smtp.gulfair.com";
              int port = 25;

              string subject = $"{fileCategory} document has been rejected";


              try
              {
                  // Assuming CSMEntities is your DbContext/data access
                  using (var db = new CSMEntities())
                  {
                      var allReasons = db.DocumentRejectReasons
                                        .Where(r => r.IsActive && !r.IsDeleted)
                                        .ToList();

                      // Build HTML email with table layout
                      var emailBody = new StringBuilder();
                      emailBody.AppendLine("<html>");
                      emailBody.AppendLine("<head>");
                      // Basic inline CSS for better compatibility across email clients
                      // Using a common sans-serif font like Arial, Helvetica, sans-serif
                      emailBody.AppendLine("<style>");
                      emailBody.AppendLine("body { font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: #333; line-height: 1.5; }");
                      emailBody.AppendLine("table { width: 100%; max-width: 600px; border-collapse: collapse; }");
                      emailBody.AppendLine("td { padding: 3px 0; }"); // Adjust default cell padding
                      emailBody.AppendLine(".header-padding { padding: 15px 0 10px 0; }");
                      emailBody.AppendLine(".document-info-padding { padding: 5px 0 15px 0; }");
                      emailBody.AppendLine(".reason-list-container { padding: 0 0 0 20px; }"); // Indent the reasons list
                      emailBody.AppendLine(".checkbox-cell { padding: 3px 5px 3px 0; vertical-align: top; width: 25px; font-size: 16px; }");
                      emailBody.AppendLine(".instruction-padding { padding: 15px 0 10px 0; }");
                      emailBody.AppendLine(".comments-section { padding: 10px 0; border-top: 1px solid #eee; }");
                      emailBody.AppendLine(".closing-padding { padding: 15px 0 5px 0; }");
                      emailBody.AppendLine("</style>");
                      emailBody.AppendLine("</head>");
                      emailBody.AppendLine("<body>");
                      emailBody.AppendLine("<table style='width: 100%; max-width: 600px; border-collapse: collapse;'>"); // Re-apply max-width here

                      // Header
                      emailBody.AppendLine("<tr><td class='header-padding'>Dear Pilot,</td></tr>");

                      emailBody.AppendLine($"<tr><td class='document-info-padding'>Your document (<strong>{fileCategory}</strong>) has been rejected for the following reason(s):</td></tr>");


                      // Reasons list
                      emailBody.AppendLine("<tr><td class='reason-list-container'>"); // Apply padding to the cell containing the nested table
                      emailBody.AppendLine("<table style='border-collapse: collapse; width: 100%;'>"); // Inner table for reasons



                      foreach (var reason in allReasons)
                      {
                          bool isChecked = rejectReasonIds?.Contains(reason.Id) ?? false;
                          emailBody.AppendLine("<tr>");
                          // Using a non-breaking space after the checkbox character for better spacing
                          emailBody.AppendLine($"<td class='checkbox-cell'>{(isChecked ? "☑" : "□")}&nbsp;</td>");
                          emailBody.AppendLine($"<td style='padding: 3px 0;'>{reason.Description}</td>"); // Retain default padding for description cell
                          emailBody.AppendLine("</tr>");
                      }

                      emailBody.AppendLine("</table>");
                      emailBody.AppendLine("</td></tr>");

                      // Instructions
                      emailBody.AppendLine("<tr><td class='instruction-padding'>Please correct the issues and upload the updated document.</td></tr>");

                      // Additional comments if present
                      if (!string.IsNullOrEmpty(adminComment))
                      {
                          emailBody.AppendLine("<tr><td class='comments-section'>");
                          emailBody.AppendLine($"<strong>Additional Comments:</strong><br>{adminComment}");
                          emailBody.AppendLine("</td></tr>");
                      }

                      // Closing
                      emailBody.AppendLine("<tr><td class='closing-padding'>Should you have any questions, feel free to get in touch</td></tr>");
                      emailBody.AppendLine("<tr><td style='padding: 5px 0;'>Best regards,</td></tr>"); // Keep consistent padding
                      emailBody.AppendLine("</table>");
                      emailBody.AppendLine("</body></html>");

                      // Send email
                      using (var smtpClient = new SmtpClient(smtpServer, port))
                      using (var mailMessage = new MailMessage())
                      {
                          mailMessage.From = new MailAddress(fromEmail);
                          mailMessage.To.Add(mailId);
                          mailMessage.Subject = subject;
                          mailMessage.Body = emailBody.ToString();
                          mailMessage.IsBodyHtml = true;

                          // Add plain text alternative for clients that don't support HTML
                          var plainTextBody = new StringBuilder();
                          plainTextBody.AppendLine("Dear Pilot,");
                          plainTextBody.AppendLine($"Your document ({fileCategory}) has been rejected for the following reason(s):");
                          plainTextBody.AppendLine();

                          foreach (var reason in allReasons)
                          {
                              bool isChecked = rejectReasonIds?.Contains(reason.Id) ?? false;
                              plainTextBody.AppendLine($"{(isChecked ? "[X]" : "[ ]")} {reason.Description}");
                          }

                          plainTextBody.AppendLine();
                          plainTextBody.AppendLine("Please correct the issues and upload the updated document.");

                          if (!string.IsNullOrEmpty(adminComment))
                          {
                              plainTextBody.AppendLine();
                              plainTextBody.AppendLine($"Additional Comments: {adminComment}");
                          }

                          plainTextBody.AppendLine();
                          plainTextBody.AppendLine("Should you have any questions, feel free to get in touch");
                          plainTextBody.AppendLine("Best regards,");

                          // Create the HTML part
                          AlternateView htmlView = AlternateView.CreateAlternateViewFromString(emailBody.ToString(), null, "text/html");
                          mailMessage.AlternateViews.Add(htmlView);

                          // Create the Plain Text part
                          AlternateView plainView = AlternateView.CreateAlternateViewFromString(plainTextBody.ToString(), null, "text/plain");
                          mailMessage.AlternateViews.Add(plainView);



                          smtpClient.Send(mailMessage);
                      }
                  }
              }
              catch (Exception ex)
              {
                  System.Diagnostics.Debug.WriteLine($"Error sending rejection email: {ex.Message}");
                  // Consider logging the full error details here
              }
          }*/

        [System.Web.Mvc.HttpGet]
        public JsonResult GetRejectionReasons(int documentId, int historyId)
        {
            try
            {
                using (var db = new CSMEntities())
                {
                    var reasons = db.PilotCrewDocRejectReasons
                                  .Where(r => r.DocId == documentId && r.IsActive && !r.IsDeleted)
                                  .Join(db.DocumentRejectReasons,
                                        reject => reject.ReasonId,
                                        reason => reason.Id,
                                        (reject, reason) => new {
                                            reason.Description
                                        })
                                  .ToList();

                    return Json(reasons, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetRejectionReasonsForHistory(int historyId)
        {
            try
            {
                var reasons = db.PilotCrewDocRejectReasons
                    .Where(r => r.History_Id == historyId)
                    .Select(r => new
                    {
                        Reason = r.DocumentRejectReason.Description // or whatever property contains the reason text
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = reasons,
                    message = reasons.Any() ? "" : "No rejection reasons found"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = "An error occurred while fetching rejection reasons"
                }, JsonRequestBehavior.AllowGet);
            }
        }
        private async Task SendRejectedMail(PilotCrewDocument docToReview, List<int> rejectReasonIds)
        {
            string fileCategory = docToReview.CrewDocumentCategory?.CategoryName ?? "Document";
            string adminComment = docToReview.Admin_Reason ?? string.Empty;
            string crewStaffId = docToReview.StaffNumber;
            string mailId = crewStaffId + "@gulfair.com";
            string fromEmail = "Pilots.Documents@gulfair.onmicrosoft.com"; // STILL THE CRITICAL POINT
            string subject = $"{fileCategory} document has been rejected";

            try
            {
                var graphClient = GetAuthenticatedGraphClient();

                // --- Debug: Test user access before sending mail ---
                // This is good for diagnosing the "Resource does not exist" error. Keep it.
                try
                {
                    var user = await graphClient.Users[fromEmail].GetAsync();
                    System.Diagnostics.Debug.WriteLine($"Successfully accessed user: {user?.Mail ?? "null"} (Display Name: {user?.DisplayName ?? "null"})");
                }
                /* catch (Microsoft.Graph.ODataErrors.ODataError odataError) // Specific ODataError catch
                 {
                     // This is the error you're getting. Log its full details.
                     System.Diagnostics.Debug.WriteLine($"Graph API ODataError (User GetAsync): Code='{odataError.Error?.Code}', Message='{odataError.Error?.Message}'");
                     if (odataError.ResponseHeaders != null)
                     {
                         System.Diagnostics.Debug.WriteLine("Response Headers:");
                         foreach (var header in odataError.ResponseHeaders)
                         {
                             System.Diagnostics.Debug.WriteLine($"- {header.Key}: {string.Join(", ", header.Value)}");
                         }
                     }
                     // Re-throw to propagate and indicate failure
                     throw new ApplicationException($"Failed to verify sender mailbox '{fromEmail}' via Microsoft Graph API. " +
                                                    $"Graph Error: {odataError.Error?.Code} - {odataError.Error?.Message}", odataError);
                 }*/
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"General Error accessing user '{fromEmail}': {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    throw; // Re-throw to propagate
                }
                // ---------------------------------------------------

                using (var db = new CSMEntities()) // Assuming CSMEntities is your DbContext/data access
                {
                    var allReasons = db.DocumentRejectReasons
                                         .Where(r => r.IsActive && !r.IsDeleted)
                                         .ToList();

                    // Build HTML email body
                    var emailHtmlBody = new StringBuilder();
                    emailHtmlBody.AppendLine("<html>");
                    emailHtmlBody.AppendLine("<head>");
                    emailHtmlBody.AppendLine("<style>");
                    emailHtmlBody.AppendLine("body { font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: #333; line-height: 1.5; }");
                    emailHtmlBody.AppendLine("table { width: 100%; max-width: 600px; border-collapse: collapse; }");
                    emailHtmlBody.AppendLine("td { padding: 3px 0; }");
                    emailHtmlBody.AppendLine(".header-padding { padding: 15px 0 10px 0; }");
                    emailHtmlBody.AppendLine(".document-info-padding { padding: 5px 0 15px 0; }");
                    emailHtmlBody.AppendLine(".reason-list-container { padding: 0 0 0 20px; }");
                    emailHtmlBody.AppendLine(".checkbox-cell { padding: 3px 5px 3px 0; vertical-align: top; width: 25px; font-size: 16px; }");
                    emailHtmlBody.AppendLine(".instruction-padding { padding: 15px 0 10px 0; }");
                    emailHtmlBody.AppendLine(".comments-section { padding: 10px 0; border-top: 1px solid #eee; }");
                    emailHtmlBody.AppendLine(".closing-padding { padding: 15px 0 5px 0; }");
                    emailHtmlBody.AppendLine("</style>");
                    emailHtmlBody.AppendLine("</head>");
                    emailHtmlBody.AppendLine("<body>");
                    emailHtmlBody.AppendLine("<table style='width: 100%; max-width: 600px; border-collapse: collapse;'>");

                    emailHtmlBody.AppendLine("<tr><td class='header-padding'>Dear Pilot,</td></tr>");
                    emailHtmlBody.AppendLine($"<tr><td class='document-info-padding'>Your document (<strong>{fileCategory}</strong>) has been rejected for the following reason(s):</td></tr>");

                    emailHtmlBody.AppendLine("<tr><td class='reason-list-container'>");
                    emailHtmlBody.AppendLine("<table style='border-collapse: collapse; width: 100%;'>");
                    foreach (var reason in allReasons)
                    {
                        bool isChecked = rejectReasonIds?.Contains(reason.Id) ?? false;
                        emailHtmlBody.AppendLine("<tr>");
                        emailHtmlBody.AppendLine($"<td class='checkbox-cell'>{(isChecked ? "☑" : "□")}&nbsp;</td>");
                        emailHtmlBody.AppendLine($"<td style='padding: 3px 0;'>{reason.Description}</td>");
                        emailHtmlBody.AppendLine("</tr>");
                    }
                    emailHtmlBody.AppendLine("</table>");
                    emailHtmlBody.AppendLine("</td></tr>");

                    emailHtmlBody.AppendLine("<tr><td class='instruction-padding'>Please correct the issues and upload the updated document.</td></tr>");

                    if (!string.IsNullOrEmpty(adminComment))
                    {
                        emailHtmlBody.AppendLine("<tr><td class='comments-section'>");
                        emailHtmlBody.AppendLine($"<strong>Additional Comments:</strong><br>{adminComment}");
                        emailHtmlBody.AppendLine("</td></tr>");
                    }

                    emailHtmlBody.AppendLine("<tr><td class='closing-padding'>Should you have any questions, feel free to get in touch</td></tr>");
                    emailHtmlBody.AppendLine("<tr><td style='padding: 5px 0;'>Best regards,</td></tr>");
                    emailHtmlBody.AppendLine("</table>");
                    emailHtmlBody.AppendLine("</body></html>");

                    // --- Plain Text Body (Good practice for email clients) ---
                    var emailPlainTextBody = new StringBuilder();
                    emailPlainTextBody.AppendLine("Dear Pilot,");
                    emailPlainTextBody.AppendLine($"Your document ({fileCategory}) has been rejected for the following reason(s):");
                    emailPlainTextBody.AppendLine();
                    foreach (var reason in allReasons)
                    {
                        bool isChecked = rejectReasonIds?.Contains(reason.Id) ?? false;
                        emailPlainTextBody.AppendLine($"{(isChecked ? "[X]" : "[ ]")} {reason.Description}");
                    }
                    emailPlainTextBody.AppendLine();
                    emailPlainTextBody.AppendLine("Please correct the issues and upload the updated document.");
                    if (!string.IsNullOrEmpty(adminComment))
                    {
                        emailPlainTextBody.AppendLine();
                        emailPlainTextBody.AppendLine($"Additional Comments: {adminComment}");
                    }
                    emailPlainTextBody.AppendLine();
                    emailPlainTextBody.AppendLine("Should you have any questions, feel free to get in touch");
                    emailPlainTextBody.AppendLine("Best regards,");
                    // ----------------------------------------------------------

                    // Create email message
                    var message = new Message
                    {
                        Subject = subject,
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Html, // Set as HTML, but the next line for Content ensures both are present
                            Content = emailHtmlBody.ToString() // HTML content
                        },
                        // For both HTML and Plain Text, set the main Body to HTML,
                        // and then add an additional body if needed (though often not explicitly required
                        // when ContentType is HTML and the content contains the HTML structure).
                        // Microsoft Graph handles the HTML body primarily. If you need a strict alternative
                        // text version, you would typically generate it separately and the client handles it.
                        // For the Graph API, sending an HTML body is usually sufficient for rich content.
                        // If a very specific plain text alternative is needed, some complex scenarios
                        // might involve multipart MIME, but for basic emails, HTML is often enough.

                        // If you wanted to include a plain text version for strict clients,
                        // you'd typically manage this client-side or use a more advanced MIME structure
                        // if sending directly via raw email bytes, which the SDK abstracts.
                        // For standard SendMail via Graph, setting ContentType.Html is usually how rich text is sent.

                        // To make sure a plain text version is also sent for robust compatibility:
                        // The SDK 'Message' object doesn't directly support AlternateViews like SmtpClient.
                        // The usual way to send both HTML and plain text with Graph is to set the 'Body'
                        // to HTML, and ensure email clients gracefully fallback if they don't support HTML.
                        // However, if strict multipart/alternative is needed, the Graph SDK provides
                        // a way to construct it, but it's more involved than a simple Body property.

                        // For now, let's assume setting ContentType.Html is the primary way
                        // for rich emails through the Graph API.
                        // If you REALLY need explicit plain text fallback within the same email for Graph:
                        // This often requires building a MIME multipart message manually or using a library
                        // that wraps it for Graph, as the Message object is simplified.
                        // For most common scenarios, simply sending HTML is enough.
                        // Let's stick to the current Body structure, as the previous code didn't explicitly
                        // add plain text to the Graph Message object either.

                        ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = mailId
                            }
                        }
                    }
                    };

                    // Create the SendMailPostRequestBody
                    var sendMailBody = new SendMailPostRequestBody
                    {
                        Message = message,
                        SaveToSentItems = true // Saves a copy in the sender's Sent Items folder
                    };

                    System.Diagnostics.Debug.WriteLine($"Attempting to send email to {mailId} from {fromEmail}...");

                    // This is the line that will attempt the send.
                    await graphClient.Users[fromEmail]
                        .SendMail
                        .PostAsync(sendMailBody);

                    System.Diagnostics.Debug.WriteLine($"Rejection email sent successfully to {mailId} from {fromEmail}.");
                }
            }
            /* catch (Microsoft.Graph.) // Catch specific Graph API errors
             {
                 System.Diagnostics.Debug.WriteLine($"Graph Service Error during SendMail: Code='{odataError.Error?.Code}', Message='{odataError.Error?.Message}'");
                 if (odataError.ResponseHeaders != null)
                 {
                     System.Diagnostics.Debug.WriteLine("Response Headers:");
                     foreach (var header in odataError.ResponseHeaders)
                     {
                         System.Diagnostics.Debug.WriteLine($"- {header.Key}: {string.Join(", ", header.Value)}");
                     }
                 }
                 // Propagate as ApplicationException with more detail
                 throw new ApplicationException("Failed to send email via Microsoft Graph API. " +
                                                $"Graph Error: {odataError.Error?.Code} - {odataError.Error?.Message}", odataError);
             }*/
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Error sending rejection email: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw new ApplicationException("Failed to send rejection email", ex);
            }
        }

        // Your GetAuthenticatedGraphClient and TokenCredentialAuthProvider are correct for ClientSecretCredential
        private GraphServiceClient GetAuthenticatedGraphClient()
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" }; // This scope covers Mail.Send if granted in Azure AD
            string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
            string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
            string clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];

            if (string.IsNullOrEmpty(tenantId)) throw new ConfigurationErrorsException("TenantId is not configured");
            if (string.IsNullOrEmpty(clientId)) throw new ConfigurationErrorsException("ClientId is not configured");
            if (string.IsNullOrEmpty(clientSecret)) throw new ConfigurationErrorsException("ClientSecret is not configured");

            try
            {
                var clientSecretCredential = new ClientSecretCredential(
                    tenantId,
                    clientId,
                    clientSecret,
                    new TokenCredentialOptions
                    {
                        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                        Retry = {
                        Mode = RetryMode.Exponential,
                        MaxRetries = 3,
                        Delay = TimeSpan.FromSeconds(2)
                        }
                    });

                var authProvider = new TokenCredentialAuthProvider(clientSecretCredential, scopes);
                var graphClient = new GraphServiceClient(authProvider);

                return graphClient;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create Graph client: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw new ApplicationException("Failed to authenticate with Microsoft Graph", ex);
            }
        }

        public class TokenCredentialAuthProvider : IAuthenticationProvider
        {
            private readonly TokenCredential _tokenCredential;
            private readonly string[] _scopes;

            public TokenCredentialAuthProvider(TokenCredential tokenCredential, string[] scopes)
            {
                _tokenCredential = tokenCredential;
                _scopes = scopes;
            }

            public async Task AuthenticateRequestAsync(RequestInformation requestInfo,
                Dictionary<string, object> additionalAuthenticationContext = null,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    var token = await _tokenCredential.GetTokenAsync(
                        new TokenRequestContext(_scopes),
                        cancellationToken);

                    requestInfo.Headers.Add("Authorization", $"Bearer {token.Token}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Token acquisition failed in AuthenticateRequestAsync: {ex.Message}");
                    throw;
                }
            }
        }
    }
}


