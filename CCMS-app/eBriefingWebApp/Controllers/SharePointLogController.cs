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
using System.Linq.Expressions;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;
using static System.Data.Entity.Infrastructure.Design.Executor;
using FormCollection = System.Web.Mvc.FormCollection;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;


namespace eBriefingWebApp.Controllers
{
    public class SharePointLogController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;
        // private readonly BlobServiceClient _blobServiceClient;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;


        public SharePointLogController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
            // _blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=ebriefingaims;AccountKey=n33PmfYsfpNf0GepSGew03MnHy1LENUN/PdCdrb8iMq2yKO609R9eoH7b3TqaRIZu1NZ18XBV4Gi+AStnIWJ9Q==;EndpointSuffix=core.windows.net");
            var region = System.Configuration.ConfigurationManager.AppSettings["AWSRegion"];
            _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));
            _bucketName = System.Configuration.ConfigurationManager.AppSettings["S3BucketName"];
        }

        CSMEntities db = new CSMEntities();




       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        //[OutputCache(Duration = 900, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> Index()
        {
            var telemetry = new TelemetryClient();
            SharePointLogPojo splog = new SharePointLogPojo();
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

            // List<String> fileNameList =db.SharePointLogs.Select(a=>a.FileName).Distinct().ToList();
            //  ViewBag.FileList = fileNameList;
            // 1. Get filename prefixes
            List<string> filePrefixes = await db.SharepointFilePrefixes
           .Select(p => p.filePrefix)
           .ToListAsync();

            // 2. Get all distinct filenames first
            List<string> allFileNames = await db.SharePointLogs
                .Select(a => a.FileName)
                .Distinct()
                .ToListAsync();

            // 3. Filter in memory (works for smaller datasets)
            List<string> fileNameList = allFileNames
                .Where(fn => filePrefixes.Any(prefix => fn.StartsWith(prefix)))
                .ToList();

            ViewBag.FileList = fileNameList;
            List<String> operationList = db.SharePointLogs.Select(a => a.Operation).Distinct().ToList();
            operationList = operationList.Where(f => f == "FileDownloaded" || f == "FilePreviewed").ToList();
            ViewBag.OperationList = operationList;
            TempData["From_Block_Date"] = DateTime.Now;
            TempData["To_Block_Date"] = DateTime.Now;
            var query = db.SharePointLogs.AsQueryable();
            String filename = "";
            String operationname ="";
            if (!string.IsNullOrEmpty(filename))
            {
                query = query.Where(a => a.FileName == filename);
            }

            if (!string.IsNullOrEmpty(operationname))
            {
                query = query.Where(a => a.Operation == operationname);
            }
            else
            {
                query = query.Where(a => a.Operation == "filedownloaded" || a.Operation == "filepreviewed");
            }

            //splog.fileName = "Select";
            query = query.Where(a =>DbFunctions.TruncateTime(a.CreationTime) == DbFunctions.TruncateTime(DateTime.Now));

            var dataretrieved = query.ToList();
            splog.sharePOintLoglist = dataretrieved;
            return View(splog);
        }
        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> IndexPost(DateTime From_Block_Date, DateTime To_Block_Date, SharePointLogPojo form)
        {
            var telemetry = new TelemetryClient();
            SharePointLogPojo splog = new SharePointLogPojo();
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

            //get data based on search criteria
            DateTime? dateTimefrom = From_Block_Date;
            DateTime? dateTimeTo = To_Block_Date;
            String filename = form.fileName;
            String operationname = form.operation;
         

            var query = db.SharePointLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filename))
            {
                query = query.Where(a => a.FileName == filename);
            }

            if (!string.IsNullOrEmpty(operationname))
            {
                query = query.Where(a => a.Operation == operationname);
            }
            else
            {
                query = query.Where(a => a.Operation == "filedownloaded" || a.Operation == "filepreviewed");
            }

            query = query.Where(a =>
                DbFunctions.TruncateTime(a.CreationTime) >= DbFunctions.TruncateTime(dateTimefrom) &&
                DbFunctions.TruncateTime(a.CreationTime) <= DbFunctions.TruncateTime(dateTimeTo));

            var dataretrieved = query.ToList();

            String[] crewCat = { "FLIGHT ATTENDANT", "CABIN SENIOR","FLIGHT ATTENDANT FIRST CLASS" ,
                "FA FALCON GOLD" ,"FLIGHT ATTENDANT","FLIGHT ATTENDANT BUSINESS" };


            var crList = db.CrewDetails.Where(a => crewCat.Any(cat => a.Category.Contains(cat))).ToList();

            List<string> crStaffNumbers = crList.Select(a => a.StaffNumber).ToList();
            dataretrieved = dataretrieved.Where(item => crStaffNumbers.Contains(item.StaffNumber)).ToList();

            //  List<String> fileNameList = db.SharePointLogs.Select(a => a.FileName).Distinct().ToList();
            //  ViewBag.FileList = fileNameList;

            List<string> filePrefixes = await db.SharepointFilePrefixes
           .Select(p => p.filePrefix)
           .ToListAsync();

            // 2. Get all distinct filenames first
            List<string> allFileNames = await db.SharePointLogs
                .Select(a => a.FileName)
                .Distinct()
                .ToListAsync();

            // 3. Filter in memory (works for smaller datasets)
            List<string> fileNameList = allFileNames
                .Where(fn => filePrefixes.Any(prefix => fn.StartsWith(prefix)))
                .ToList();

            ViewBag.FileList = fileNameList;

            List<String> operationList = db.SharePointLogs.Select(a => a.Operation).Distinct().ToList();
            operationList = operationList.Where(f => f == "FileDownloaded" || f == "FilePreviewed").ToList();
            ViewBag.OperationList = operationList;
            if (From_Block_Date != null)
            {
                TempData["From_Block_Date"] = From_Block_Date;
            }
            else
            {
                TempData["From_Block_Date"] = DateTime.Now;
            }

            if (To_Block_Date != null)
            {
                TempData["To_Block_Date"] = To_Block_Date;
            }
            else
            {
                TempData["To_Block_Date"] = DateTime.Now;
            }

            splog.sharePOintLoglist = dataretrieved;
            return View(splog);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> ExcludeData()
        {
            var telemetry = new TelemetryClient();
            SharePointLogPojo splog = new SharePointLogPojo();
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

            //List<String> fileNameList = db.SharePointLogs.Select(a => a.FileName).Distinct().ToList();
            // ViewBag.FileList = fileNameList;

            List<string> filePrefixes = await db.SharepointFilePrefixes
           .Select(p => p.filePrefix)
           .ToListAsync();

            // 2. Get all distinct filenames first
            List<string> allFileNames = await db.SharePointLogs
                .Select(a => a.FileName)
                .Distinct()
                .ToListAsync();

            // 3. Filter in memory (works for smaller datasets)
            List<string> fileNameList = allFileNames
                .Where(fn => filePrefixes.Any(prefix => fn.StartsWith(prefix)))
                .ToList();

            ViewBag.FileList = fileNameList;
            List<String> operationList = db.SharePointLogs.Select(a => a.Operation).Distinct().ToList();
            operationList = operationList.Where(f => f == "FileDownloaded" || f == "FilePreviewed").ToList();
            ViewBag.OperationList = operationList;
            TempData["From_Block_Date"] = DateTime.Now;
            TempData["To_Block_Date"] = DateTime.Now;
                     

            return View(splog);
        }
        [System.Web.Mvc.HttpPost]
       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> ExcludeDataPost(DateTime From_Block_Date, DateTime To_Block_Date, SharePointLogPojo form)
        {
            var telemetry = new TelemetryClient();
            SharePointLogPojo splog = new SharePointLogPojo();
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

            //List<String> fileNameList = db.SharePointLogs.Select(a => a.FileName).Distinct().ToList();
            // ViewBag.FileList = fileNameList;

            List<string> filePrefixes = await db.SharepointFilePrefixes
           .Select(p => p.filePrefix)
           .ToListAsync();

            // 2. Get all distinct filenames first
            List<string> allFileNames = await db.SharePointLogs
                .Select(a => a.FileName)
                .Distinct()
                .ToListAsync();

            // 3. Filter in memory (works for smaller datasets)
            List<string> fileNameList = allFileNames
                .Where(fn => filePrefixes.Any(prefix => fn.StartsWith(prefix)))
                .ToList();

            ViewBag.FileList = fileNameList;

            List<String> operationList = db.SharePointLogs.Select(a => a.Operation).Distinct().ToList();
            operationList = operationList.Where(f => f == "FileDownloaded" || f == "FilePreviewed").ToList();
            ViewBag.OperationList = operationList;
            TempData["From_Block_Date"] = From_Block_Date;
            TempData["To_Block_Date"] = To_Block_Date;
            var query = db.SharePointLogs.AsQueryable();
            String filename = form.fileName;

            if (!string.IsNullOrEmpty(filename))
            {
                query = query.Where(a => a.FileName == filename);
            }



            //splog.fileName = "Select";
            query = query.Where(a =>
               DbFunctions.TruncateTime(a.CreationTime) >= DbFunctions.TruncateTime(From_Block_Date) &&
               DbFunctions.TruncateTime(a.CreationTime) <= DbFunctions.TruncateTime(To_Block_Date));

            var dataretrieved = query.ToList();
            splog.sharePOintLoglist = dataretrieved;
            List<String> crewList = dataretrieved.Select(a => a.StaffNumber).Distinct().ToList();
            var allCrew = db.CrewDetails.Select(a => a.StaffNumber).ToList(); // Get all StaffNumbers from CrewDetail
            var crewInLogs = crewList.ToList(); // Assuming crewList is already populated

            String[] crewCat = { "FLIGHT ATTENDANT", "CABIN SENIOR","FLIGHT ATTENDANT FIRST CLASS" ,
                "FA FALCON GOLD" ,"FLIGHT ATTENDANT","FLIGHT ATTENDANT BUSINESS" };
            /*  List<CrewDetail> crewNotInLogs = (from cd in db.CrewDetails
                                   where !crewInLogs.Contains(cd.StaffNumber)
                                   select cd).ToList();*/

            List<CrewDetail> crewNotInLogs = db.CrewDetails .Where(cd => !crewInLogs.Contains(cd.StaffNumber) &&
                                           crewCat.Any(cat => cd.Category.Contains(cat)) &&!db.InactiveCrewLists.Any(sc => sc.StaffNumber == cd.StaffNumber)).ToList();

            //List<String> crewNotInLogs1 = allCrew.Except(crewInLogs).ToList();
            //List<CrewDetail> crList = new List<CrewDetail>();
            TempData["FileName_Search"] = filename;


            return View(crewNotInLogs);
        }



    }
}

