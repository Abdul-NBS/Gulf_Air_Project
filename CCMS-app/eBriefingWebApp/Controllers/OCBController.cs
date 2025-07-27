using eBriefingWebApp.Helper;
using eBriefingWebApp.Interfaces;
using eBriefingWebApp.Models;
using eBriefingWebApp.ViewModels;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace eBriefingWebApp.Controllers
{
    public class OCBController : Controller
    {
        private readonly IGetGraphUserService _graphUserService;
        private readonly UserHelper _userHelper;

        public OCBController(IGetGraphUserService graphUserService)
        {
            _graphUserService = graphUserService;
            _userHelper = new UserHelper();
        }

        CSMEntities db = new CSMEntities();

        // GET: eBriefing

       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        //[OutputCache(Duration = 900, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> Index(DateTime? Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                if (Block_Date == null)
                {
                    var blocks = db.OCB_Blocks.Where(a => DbFunctions.TruncateTime(a.Block_Date) == DbFunctions.TruncateTime(DateTime.Now) && a.OCB_Flights.Count > 0).ToList();

                    TempData["Block_Date"] = DateTime.Now;
                    return View(blocks);
                }
                else
                {
                    var blocks = db.OCB_Blocks.Where(a => DbFunctions.TruncateTime(a.Block_Date) == DbFunctions.TruncateTime(Block_Date) && a.OCB_Flights.Count > 0).ToList();

                    TempData["Block_Date"] = Block_Date;
                    return View(blocks);
                }
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



        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public System.Web.Mvc.ActionResult Index(DateTime Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                return RedirectToAction("Index", "OCB", new { Block_Date = Block_Date });
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
        //[OutputCache(Duration = 300, VaryByParam = "Block_Date")]
        public async Task<System.Web.Mvc.ActionResult> CabinCrewPos(int Id, DateTime? Block_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var getBlock = db.OCB_Blocks.Where(a => a.Id == Id).FirstOrDefault();

                if (getBlock == null)
                {
                    return RedirectToAction("", "OCB");
                }

                var crewPos = db.OCB_CrewPos.Where(a => a.BlockId == Id && a.is_deleted == false).ToList();

                var final = crewPos.OrderBy(m => m.Category, new CategoryComparer()).ToList();

                ViewBag.EqpList = db.OCB_AC_Pos.GroupBy(b => b.EQP).Select(a => new
                {
                    EQP = a.Key
                }).ToList();

                if (getBlock != null)
                {
                    ViewBag.AC_Pos = db.OCB_AC_Pos.Where(a => a.EQP == getBlock.Eqp).ToList();
                }

                ViewBag.Block = getBlock;

                var crewCat = Helper.CrewCategory.getCategoryList().ToList();

                ViewBag.PosHistory = db.OCB_Crew_Pos_History.OrderByDescending(a => a.OCB_CrewPos.OCB_Blocks.Block_Date).ToList();

                TempData["Block_Date"] = Block_Date;

                return View(crewPos);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            TempData["Block_Date"] = Block_Date;

            return View();
        }


        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CabinCrewPos(List<OCB_CrewPos> form, string EQP)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            int Id = form.FirstOrDefault().BlockId.Value;
            var block = db.OCB_Blocks.Where(a => a.Id == Id).FirstOrDefault();

            ViewBag.AC_Pos = db.OCB_AC_Pos.Where(a => a.EQP == block.Eqp).Select(a => new
            {
                a.Id,
                a.PosCode
            }).ToList();

            DateTime Block_Date = Convert.ToDateTime(block.Block_Date);

            TempData["Block_Date"] = Block_Date;

            try
            {
                if (block.Eqp != EQP)
                {
                    block.Eqp = EQP;
                    await db.SaveChangesAsync();
                }

                if (form.Count > 0)
                {
                    var checkDuplicate = form.Where(x => x.PosId != null && x.PosId != null)
                                             .GroupBy(x => x.PosId)
                                             .Where(g => g.Count() > 1)
                                             .Select(x => x.Key)
                                             .Where(a => a.Value != 147)
                                             .ToList();

                    if (checkDuplicate != null && checkDuplicate.Count == 0)
                    {
                        foreach (var item in form)
                        {
                            if (item.PosId != null)
                            {
                                var getCrew = db.OCB_CrewPos.Where(a => a.Id == item.Id).FirstOrDefault();

                                if (getCrew != null)
                                {
                                    getCrew.Last_Update = DateTime.Now;
                                    getCrew.PosId = item.PosId;

                                    db.SaveChanges();

                                    var getPosHistory = db.OCB_Crew_Pos_History.Where(a => a.CrewPosId == item.Id).FirstOrDefault();

                                    if (getPosHistory == null)
                                    {
                                        db.OCB_Crew_Pos_History.Add(new OCB_Crew_Pos_History
                                        {
                                            CrewPosId = item.Id,
                                            PosId = item.PosId.Value,
                                        });

                                        db.SaveChanges();
                                    }
                                    else
                                    {
                                        getPosHistory.PosId = item.PosId.Value;
                                        db.SaveChanges();
                                    }
                                }
                            }
                        }

                        TempData["Status"] = "Saved";
                    }
                    else
                    {
                        TempData["Status"] = "Exist";
                    }
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            // Clear cache
            //Response.RemoveOutputCacheItem(Url.Action("CabinCrewPos", "OCB"));
            //Response.RemoveOutputCacheItem(Url.Action("Index", "OCB"));

            return RedirectToAction("CabinCrewPos", "OCB", new { Id = Id, Block_Date = Block_Date });
        }



        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> ManageDeletedCrew(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                ViewBag.BlockId = Id;

                var getBlock = db.OCB_Blocks.Where(a => a.Id == Id).FirstOrDefault();

                if (getBlock != null)
                {
                    ViewBag.BlockDate = getBlock.Block_Date.Value.ToString("yyy-MM-dd");
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            var getCrew = db.OCB_CrewPos.Where(a => a.BlockId == Id && a.is_deleted == true).ToList();

            return View(getCrew);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> ReAddCrewPos(int BlockId, int PosId)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                ViewBag.BlockId = BlockId;

                var getCrew = db.OCB_CrewPos.Where(a => a.Id == PosId).FirstOrDefault();

                if (getCrew != null)
                {
                    getCrew.is_deleted = false;
                    getCrew.ManualAdd = true;
                    await db.SaveChangesAsync();

                    TempData["Status"] = "Saved";
                }

                var getCrewList = db.OCB_CrewPos.Where(a => a.BlockId == BlockId && a.is_deleted == true).ToList();
            }
            catch (Exception ex)
            {
                TempData["Status"] = "Error";
            }

            return RedirectToAction("ManageDeletedCrew", "OCB", new { Id = BlockId });
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> AddAllCrewPos(int BlockId)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var getCrew = db.OCB_CrewPos.Where(a => a.BlockId == BlockId && a.is_deleted == true).ToList();

                foreach (var item in getCrew)
                {
                    item.ManualAdd = true;
                    item.is_deleted = false;
                    await db.SaveChangesAsync();
                }

                TempData["Status"] = "Saved";
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("ManageDeletedCrew", "OCB", new { Id = BlockId });
        }


       [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> DailyTimeRecord(DateTime? FlightDate)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                ViewBag.GetStatus = ClsStatus.ListGetStatus();

                if (FlightDate == null || FlightDate.Value.Date == DateTime.Now.Date)
                {
                    //chamged order for proper data fetch for multiple sector flights
                    var flights = db.OCB_Flights.Where(a => DbFunctions.TruncateTime(a.Flight_Date) == DbFunctions.TruncateTime(DateTime.Now) && a.Orig == "BAH")
                    .OrderBy(a => a.Id)
                       .Select(a => new DailyTimeRecordVM
                       {
                           Id = a.Id,
                           BlockId = a.BlockId.Value,
                           CheckInTime = a.OCB_Blocks.Start_Time.Value,
                           Route = a.Dest,
                           Room = a.OCB_AllocationScreen.FirstOrDefault().RoomNumber,
                           Duty = a.OCB_AllocationScreen.FirstOrDefault().Duty,
                           FlightNumber = a.Flt_No,
                           Status = a.OCB_AllocationScreen.FirstOrDefault().Status,
                           FlightId = a.Id,
                           FlightDate = a.OCB_Blocks.Block_Date.Value
                       })
                       .ToList(); // Materialize the results here

                    var uniqueFlights = flights
                                        .GroupBy(f => f.BlockId)
                                        .Select(g => g.OrderBy(f => f.Id).First()) // Order by Id within each group
                                        .OrderBy(a => a.CheckInTime)
                                        .ToList();

                    List<DailyTimeRecordVM> dailyTimeRecordVM = new List<DailyTimeRecordVM>();


                    // Now uniqueFlights contains only the first record for each BlockId
                    foreach (var item in uniqueFlights)
                    {
                        dailyTimeRecordVM.Add(new DailyTimeRecordVM
                        {
                            BlockId = item.BlockId,
                            CheckInTime = item.CheckInTime,
                            Duty = item.Duty ?? "",
                            FlightDate = item.FlightDate,
                            FlightId = item.FlightId,
                            FlightNumber = item.FlightNumber ?? "",
                            Id = item.Id,
                            Room = item.Room ?? "",
                            Route = item.Route ?? "",
                            Status = item.Status ?? ""
                        });
                    }


                    ViewBag.FlightDate = DateTime.Now.ToString("yyyy-MM-dd");

                    return View(dailyTimeRecordVM);

                }
                else
                {

                    var flights = db.OCB_Flights.Where(a => DbFunctions.TruncateTime(a.Flight_Date) == DbFunctions.TruncateTime(FlightDate) && a.Orig == "BAH")
    .OrderBy(a => a.Id)
                       .Select(a => new DailyTimeRecordVM
                       {
                           Id = a.Id,
                           BlockId = a.BlockId.Value,
                           CheckInTime = a.OCB_Blocks.Start_Time.Value,
                           Route = a.Dest,
                           Room = a.OCB_AllocationScreen.FirstOrDefault().RoomNumber,
                           Duty = a.OCB_AllocationScreen.FirstOrDefault().Duty,
                           FlightNumber = a.Flt_No,
                           Status = a.OCB_AllocationScreen.FirstOrDefault().Status,
                           FlightId = a.Id,
                           FlightDate = a.OCB_Blocks.Block_Date.Value
                       })
                       .ToList();

                    // Group by BlockId and select the first record from each group
                    var uniqueFlights = flights
                                        .GroupBy(f => f.BlockId)
                                        .Select(g => g.OrderBy(f => f.Id).First()) // Order by Id within each group
                                        .OrderBy(a => a.CheckInTime)
                                        .ToList();

                    List<DailyTimeRecordVM> dailyTimeRecordVM = new List<DailyTimeRecordVM>();


                    // Now uniqueFlights contains only the first record for each BlockId
                    foreach (var item in uniqueFlights)
                    {
                        dailyTimeRecordVM.Add(new DailyTimeRecordVM
                        {
                            BlockId = item.BlockId,
                            CheckInTime = item.CheckInTime,
                            Duty = item.Duty ?? "",
                            FlightDate = item.FlightDate,
                            FlightId = item.FlightId,
                            FlightNumber = item.FlightNumber ?? "",
                            Id = item.Id,
                            Room = item.Room ?? "",
                            Route = item.Route ?? "",
                            Status = item.Status ?? ""
                        });
                    }

                    ViewBag.FlightDate = FlightDate.Value.ToString("yyyy-MM-dd");

                    return View(dailyTimeRecordVM);
                }

            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            List<OCB_Flights> list = new List<OCB_Flights>();

            ViewBag.FlightDate = DateTime.Now.ToString("yyyy-MM-dd");

            ViewBag.GetStatus = ClsStatus.ListGetStatus();

            return View(list);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CabinCrewPosPrint(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var block = db.OCB_Blocks.Where(a => a.Id == Id).FirstOrDefault();

                ViewBag.PosHistory = db.OCB_Crew_Pos_History.ToList();

                return View(block);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            return RedirectToAction("", "OCB");
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> DailyTimeRecord(List<DailyTimeRecordVM> form, string submit, DateTime Flight_Date)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            // Update the ViewBag.FlightDate
            ViewBag.FlightDate = Flight_Date.ToString("yyyy-MM-dd");

            try
            {
                var btn = submit;

                if (submit == "btnGetData")
                {
                    return RedirectToAction("DailyTimeRecord", "OCB", new { FlightDate = Flight_Date });
                }
                else if (submit == "btnSave")
                {
                    foreach (var item in form.Where(a => a.Room != null))
                    {
                        if (item.Room != null)
                        {
                            var checkFlightExist = db.OCB_AllocationScreen.Where(a => a.FlightId == item.FlightId).FirstOrDefault();

                            if (checkFlightExist == null)
                            {
                                db.OCB_AllocationScreen.Add(new OCB_AllocationScreen
                                {
                                    Duty = item.Duty,
                                    FlightId = item.FlightId,
                                    RoomNumber = item.Room,
                                    Status = item.Status
                                });

                                await db.SaveChangesAsync();

                            }
                            else
                            {
                                checkFlightExist.Duty = item.Duty;
                                checkFlightExist.RoomNumber = item.Room;
                                checkFlightExist.Status = item.Status;

                                await db.SaveChangesAsync();
                            }
                        }
                    }

                    TempData["Status"] = "Saved";
                }


            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            ViewBag.FlightDate = Flight_Date.ToString("yyyy-MM-dd");

            return RedirectToAction("DailyTimeRecord", "OCB", new { FlightDate = Flight_Date });

        }


        [HttpPost]
        public async Task<ActionResult> SaveRow(DailyTimeRecordVM item)
        {
            if (item == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            var checkFlightExist = db.OCB_AllocationScreen.Where(a => a.FlightId == item.FlightId).FirstOrDefault();

            if (checkFlightExist == null)
            {
                db.OCB_AllocationScreen.Add(new OCB_AllocationScreen
                {
                    Duty = item.Duty,
                    FlightId = item.FlightId,
                    RoomNumber = item.Room,
                    Status = item.Status
                });

                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Record saved successfully." });
            }
            else
            {
                checkFlightExist.Duty = item.Duty;
                checkFlightExist.RoomNumber = item.Room;
                checkFlightExist.Status = item.Status;

                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Record saved successfully." });
            }


        }

        [HttpPost]
        public async Task<ActionResult> SaveAll(List<DailyTimeRecordVM> item)
        {
            if (item == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            foreach (var flight in item)
            {
                var checkFlightExist = db.OCB_AllocationScreen.Where(a => a.FlightId == flight.FlightId).FirstOrDefault();

                if (checkFlightExist == null)
                {
                    db.OCB_AllocationScreen.Add(new OCB_AllocationScreen
                    {
                        Duty = flight.Duty,
                        FlightId = flight.FlightId,
                        RoomNumber = flight.Room,
                        Status = flight.Status
                    });

                    await db.SaveChangesAsync();


                }
                else
                {
                    checkFlightExist.Duty = flight.Duty;
                    checkFlightExist.RoomNumber = flight.Room;
                    checkFlightExist.Status = flight.Status;

                    await db.SaveChangesAsync();


                }
            }

            return Json(new { success = true, message = "Record saved successfully." });



        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> AllocationScreen()
        {
           var flights = db.OCB_AllocationScreen.Where(a => DbFunctions.TruncateTime(a.OCB_Flights.OCB_Blocks.Block_Date) == DbFunctions.TruncateTime(DateTime.UtcNow)).OrderBy(a => a.OCB_Flights.OCB_Blocks.Start_Time).ToList();

            ViewBag.getPinFlights = db.OCB_AllocationScreen.Where(a => a.Status == "Pin").ToList();

            return View(flights);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> AddNewCrewPos(int Id)
        {
            var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

            if (getUser != null)
            {
                Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
            }

            var getBlock = db.OCB_Blocks.Where(a => a.Id == Id).FirstOrDefault();

            if (getBlock != null)
            {
                ViewBag.Block = new OCB_Blocks
                {
                    Block_No = getBlock.Block_No,
                    Id = getBlock.Id,
                    Block_Date = getBlock.Block_Date,
                    Days_No = getBlock.Days_No,
                    End_Time = getBlock.End_Time,
                    End_Date = getBlock.End_Date,
                    Eqp = getBlock.Eqp,
                    Start_Time = getBlock.Start_Time,
                };

                ViewBag.AC_Pos = db.OCB_AC_Pos.Where(a => a.EQP == getBlock.Eqp).Select(a => new
                {
                    a.Id,
                    a.PosCode
                }).ToList();

                ViewBag.Category = db.CrewCategories.Select(a => new
                {
                    a.Id,
                    a.Category
                }).ToList();
            }

            OCB_CrewPos form = new OCB_CrewPos();

            form.BlockId = Id;

            return View(form);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> AddNewCrewPos(OCB_CrewPos form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var checlCrewAdded = db.OCB_CrewPos.Where(a => a.StaffNumber == form.StaffNumber &&
                a.BlockId == form.BlockId).FirstOrDefault();

                if (checlCrewAdded == null)
                {
                    var crew = db.OCB_CrewPos.Add(new OCB_CrewPos
                    {
                        ManualAdd = true,
                        BlockId = form.BlockId,
                        Category = form.Category,
                        guid = Guid.NewGuid().ToString(),
                        is_deleted = false,
                        Last_Update = DateTime.Now,
                        PosId = form.PosId,
                        StaffNumber = form.StaffNumber,
                    });

                    await db.SaveChangesAsync();

                    db.OCB_Crew_Pos_History.Add(new OCB_Crew_Pos_History
                    {
                        CrewPosId = crew.Id,
                        PosId = form.PosId.Value,
                    });

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Added";
                    return RedirectToAction("CabinCrewPos", "OCB", new { Id = form.Id });
                }
                else
                {
                    TempData["Status"] = "Exist";
                    return RedirectToAction("AddNewCrewPos", "OCB", new { Id = form.Id });
                }

                var getBlock = db.OCB_Blocks.Where(a => a.Id == form.BlockId).FirstOrDefault();

                if (getBlock != null)
                {
                    ViewBag.Block = new OCB_Blocks
                    {
                        Block_No = getBlock.Block_No,
                        Id = getBlock.Id,
                        Block_Date = getBlock.Block_Date,
                        Days_No = getBlock.Days_No,
                        End_Time = getBlock.End_Time,
                        End_Date = getBlock.End_Date,
                        Eqp = getBlock.Eqp,
                        Start_Time = getBlock.Start_Time,
                    };

                    ViewBag.AC_Pos = db.OCB_AC_Pos.Where(a => a.EQP == getBlock.Eqp).Select(a => new
                    {
                        a.Id,
                        a.PosCode
                    }).ToList();

                    ViewBag.Category = db.CrewCategories.Select(a => new
                    {
                        a.Id,
                        a.Category
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View(form);
        }

        [System.Web.Mvc.HttpGet]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public JsonResult GetStaffInfo(string StaffNumber)
        {
            // check if staffnumber is part of TL
            var TLInfo = db.Appraisal_Team_Leads.Where(a=>a.StaffNumber == StaffNumber).FirstOrDefault();
            if (TLInfo != null)
            {
                var query = db.Appraisal_Team_Leads.Where(a => a.StaffNumber == StaffNumber).Select(a => new
                {
                    a.StaffNumber,
                    a.FullName,
                    a.Category
                }).FirstOrDefault();

                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var query = db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).Select(a => new
                {
                    a.StaffNumber,
                    a.FullName,
                    a.Category
                }).FirstOrDefault();

                return Json(query, JsonRequestBehavior.AllowGet);
            }
           // return Json(query, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> RemoveCrewPos(int Id, int BlockId)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var query = db.OCB_CrewPos.Where(a => a.Id == Id).FirstOrDefault();

                if (query != null)
                {
                    query.is_deleted = true;
                    query.ManualAdd = true;
                    await db.SaveChangesAsync();

                    var getHistory = db.OCB_Crew_Pos_History.Where(a => a.CrewPosId == query.Id).FirstOrDefault();
                    db.OCB_Crew_Pos_History.Remove(getHistory);
                    await db.SaveChangesAsync();

                    TempData["Status"] = "Deleted";
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("CabinCrewPos", "OCB", new { Id = BlockId });
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public System.Web.Mvc.ActionResult CrewPosHistory()
        {
            List<OCB_Crew_Pos_History> list = new List<OCB_Crew_Pos_History>();

            return View(list);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CrewPosHistory(string StaffNumber)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                if (StaffNumber != "")
                {
                    var query = await db.OCB_Crew_Pos_History.Where(a => a.OCB_CrewPos.StaffNumber == StaffNumber).ToListAsync();

                    return View(query);
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            List<OCB_Crew_Pos_History> list = new List<OCB_Crew_Pos_History>();

            return View(list);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> AddBriefingComment(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            OCB_Comments form = new OCB_Comments();

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }



                ViewBag.Hr = TimeVM.GetHr().ToList();
                ViewBag.Mins = TimeVM.GetMin().ToList();

                var query = db.OCB_CrewPos.Where(a => a.Id == Id).FirstOrDefault();

                form.OCB_CrewPos = query;

                if (query.OCB_Comments.FirstOrDefault() != null)
                {
                    form.Comment = query.OCB_Comments.FirstOrDefault().Comment;
                    form.Id = query.OCB_Comments.FirstOrDefault().Id;
                    form.DateTime = query.OCB_Comments.FirstOrDefault().DateTime;
                    form.Time = query.OCB_Comments.FirstOrDefault().Time;
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View(form);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> AddBriefingComment(FormCollection data)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            int CrewPosId = Convert.ToInt32(Request.Form["CrewPosId"]);
            int BlockId = Convert.ToInt32(Request.Form["BlockId"]);
            string Comment = Request.Form["Comment"];
            string DropHr = Request.Form["DropHr"];
            string DropMin = Request.Form["DropMin"];

            try
            {
                var CheckExist = db.OCB_Comments.Where(a => a.CrewPosId == CrewPosId).FirstOrDefault();

                if (CheckExist == null)
                {
                    db.OCB_Comments.Add(new OCB_Comments
                    {
                        Comment = Comment,
                        CrewPosId = CrewPosId,
                        DateTime = DateTime.Now,
                        Time = DropHr + ":" + DropMin
                    });

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Added";

                    return RedirectToAction("CabinCrewPos", "OCB", new { Id = BlockId });
                }
                else
                {
                    CheckExist.Time = DropHr + ":" + DropMin;
                    CheckExist.Comment = Comment;
                    CheckExist.DateTime = DateTime.Now;

                    await db.SaveChangesAsync();

                    return RedirectToAction("CabinCrewPos", "OCB", new { Id = BlockId });
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            ViewBag.Hr = TimeVM.GetHr().ToList();
            ViewBag.Mins = TimeVM.GetMin().ToList();

            OCB_Comments form = new OCB_Comments();

            var query = db.OCB_CrewPos.Where(a => a.Id == CrewPosId).FirstOrDefault();

            form.OCB_CrewPos = query;

            return View(form);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> DeleteBriefingComment(int Id, int BlockId)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var query = db.OCB_Comments.Where(a => a.Id == Id).FirstOrDefault();
                db.OCB_Comments.Remove(query);
                await db.SaveChangesAsync();

                TempData["Status"] = "Deleted";
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("CabinCrewPos", "OCB", new { Id = BlockId });
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> BriefingComment()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            List<OCB_Comments> list = new List<OCB_Comments>();

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                if (TempData["CrewStaffNumber"] != null)
                {
                    string StaffNumber = TempData["CrewStaffNumber"].ToString();
                    var query = await db.OCB_Comments.Where(a => a.OCB_CrewPos.StaffNumber == StaffNumber).ToListAsync();
                    list.AddRange(query);
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View(list);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> BriefingComment(FormCollection data)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            List<OCB_Comments> list = new List<OCB_Comments>();

            try
            {
                string StaffNumber = Request.Form["StaffNumber"];
                DateTime DateFrom = Convert.ToDateTime(Request.Form["DateFrom"]);
                DateTime DateTo = Convert.ToDateTime(Request.Form["DateTo"]);

                var query = await db.OCB_Comments.Where(a => a.OCB_CrewPos.StaffNumber == StaffNumber &&
                (DbFunctions.TruncateTime(a.DateTime) >= DbFunctions.TruncateTime(DateFrom) &&
                 DbFunctions.TruncateTime(a.DateTime) <= DbFunctions.TruncateTime(DateTo))).ToListAsync();
                list.AddRange(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View(list);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> DeleteOCBComment(int Id)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var query = db.OCB_Comments.Where(a => a.Id == Id).FirstOrDefault();

                if (query != null)
                {
                    TempData["CrewStaffNumber"] = query.OCB_CrewPos.StaffNumber;

                    db.OCB_Comments.Remove(query);
                    await db.SaveChangesAsync();

                    TempData["Status"] = "Deleted";
                }
                else
                {
                    TempData["Status"] = "Not Found";
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("BriefingComment", "OCB");
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> AddNewAllocation(DateTime? FlightDate)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            ClsAllocationScreen oCB_Flights = new ClsAllocationScreen();

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                oCB_Flights.FlightDate = FlightDate.Value;

                ViewBag.GetStatus = ClsStatus.ListGetStatus();
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View(oCB_Flights);
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> ClearAllPin(DateTime? FlightDate)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            ClsAllocationScreen oCB_Flights = new ClsAllocationScreen();

            try
            {
                oCB_Flights.FlightDate = FlightDate.Value;

                ViewBag.GetStatus = ClsStatus.ListGetStatus();

                var getAllPins = db.OCB_AllocationScreen.Where(a => a.Status == "Pin").ToList();

                foreach (var item in getAllPins)
                {
                    var getPin = db.OCB_AllocationScreen.Where(a => a.Id == item.Id).FirstOrDefault();

                    getPin.Status = "";

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View(oCB_Flights);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> AddNewAllocation(ClsAllocationScreen data)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            ViewBag.GetStatus = ClsStatus.ListGetStatus();

            try
            {
                var chkFlightExist = db.OCB_Flights.Where(a => a.Flt_No == data.FlightNumber &&
            DbFunctions.TruncateTime(a.Flight_Date) == DbFunctions.TruncateTime(data.FlightDate)).FirstOrDefault();

                if (chkFlightExist != null)
                {
                    var chkExistAllocation = db.OCB_AllocationScreen.Where(a => a.FlightId == chkFlightExist.Id).FirstOrDefault();

                    if (chkExistAllocation == null)
                    {
                        db.OCB_AllocationScreen.Add(new OCB_AllocationScreen
                        {
                            Duty = data.Duty,
                            FlightId = chkFlightExist.Id,
                            RoomNumber = data.RoomNumber,
                            Status = data.Status,
                        });

                        await db.SaveChangesAsync();

                        TempData["Status"] = "Added";

                        return RedirectToAction("AddNewAllocation", "OCB", new { FlightDate = data.FlightDate.ToString("dd-MMM-yyyy") });
                    }
                    else
                    {
                        TempData["Status"] = "Exist";
                    }
                }
                else
                {
                    TempData["Status"] = "Not Found";

                    return View(data);
                }
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View(data);
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public System.Web.Mvc.ActionResult SetCurrentPage(int currentPage)
        {
            ViewBag.currentPage = currentPage;
            return Json(new { success = true });
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CrewDetails()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.CrewDetails.Where(a => a.Category != "SENIOR CAPTAIN"
                && a.Category != "SENIOR FIRST OFFICER"
                && a.Category != "FIRST OFFICER"
                && a.Category != "SECOND OFFICER-TAMKEEN"
                && a.Category != "CAPTAIN").ToListAsync();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View();
        }

        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CrewDetail(string StaffNumber)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                var query = await db.CrewDetails.Where(a => a.StaffNumber == StaffNumber).FirstOrDefaultAsync();

                return View(query);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return RedirectToAction("CrewDetails", "OCB");
        }

        [System.Web.Mvc.HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> CrewDetail(CrewDetail form)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var query = await db.CrewDetails.Where(a => a.StaffNumber == form.StaffNumber).FirstOrDefaultAsync();

                if (query != null)
                {
                    query.PA = form.PA;

                    await db.SaveChangesAsync();

                    TempData["Status"] = "Saved";

                    return RedirectToAction("CrewDetails", "OCB");
                }
                else
                {
                    TempData["Status"] = "Not Found";
                }

                return View(form);
            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error";
            }

            return View(form);
        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public Task<System.Web.Mvc.ActionResult> RecencyReportIndex()
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            List<vwRecency> vWlist1 = new List<vwRecency>();

            DateTime? crntDate = DateTime.Now;
            DateTime? defaultDate = DateTime.Now.AddDays(-10);
            TempData["StaffNumber"] = "";
            List<vwRecency> seqList = db.vwRecencies.Where(a => DbFunctions.TruncateTime(a.Expiry_Date) < DbFunctions.TruncateTime(crntDate) && DbFunctions.TruncateTime(a.Expiry_Date) >= DbFunctions.TruncateTime(defaultDate)).OrderByDescending(a => a.Expiry_Date).ToList();

            return Task.FromResult<ActionResult>(View(seqList));

        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public Task<System.Web.Mvc.ActionResult> RecencyReport(DateTime? From_Date, DateTime? To_Date)
        {

            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            Debug.WriteLine("Hello");
            List<vwRecencySummary> vwLIst;

            if (From_Date == null && To_Date == null)
            {
                DateTime? crntDate = DateTime.Now;
                DateTime? defaultDate = DateTime.Now.AddDays(+3);
                vwLIst = db.vwRecencySummaries.Where(a => DbFunctions.TruncateTime(a.Expiry_Date) >= DbFunctions.TruncateTime(crntDate) && DbFunctions.TruncateTime(a.Expiry_Date) < DbFunctions.TruncateTime(defaultDate)).OrderByDescending(a => a.Expiry_Date).ToList();
                TempData["From_Date"] = DateTime.Now; ;
                TempData["To_Date"] = DateTime.Now.AddDays(+3);
            }
            else
            {
                vwLIst = db.vwRecencySummaries.Where(a => DbFunctions.TruncateTime(a.Expiry_Date) >= DbFunctions.TruncateTime(From_Date) && DbFunctions.TruncateTime(a.Expiry_Date) < DbFunctions.TruncateTime(To_Date)).OrderByDescending(a => a.Expiry_Date).ToList();
                TempData["From_Date"] = From_Date;
                TempData["To_Date"] = To_Date;
            }


            return Task.FromResult<ActionResult>(View(vwLIst));
        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public Task<System.Web.Mvc.ActionResult> StaffRecencyReport(String StaffNumber)
        {

            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS
            
            String StaffNumbr = StaffNumber;

            List<vwRecency> vwLIst = db.vwRecencies.Where(a => a.StaffNumber == StaffNumber).ToList();
            return Task.FromResult<ActionResult>(View(vwLIst));
        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> ViewEditManualAlloc(DateTime? FlightDate)
        {
            // var telemetry = new TelemetryClient(); // TODO: commented by NBS for migration to AWS

            try
            {
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                if (getUser != null)
                {
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }

                ViewBag.GetStatus = ClsStatus.ListGetStatus();

                if (FlightDate == null || FlightDate.Value.Date == DateTime.Now.Date)
                {
                    var flights = db.OCB_Flights
                    .Where(a => DbFunctions.TruncateTime(a.Flight_Date) == DbFunctions.TruncateTime(DateTime.Now) && a.Orig == "BAH")
                           .Select(a => new DailyTimeRecordVM
                           {
                               Id = a.Id,
                               BlockId = a.BlockId.Value,
                               CheckInTime = a.OCB_Blocks.Start_Time.Value,
                               Route = a.Dest,
                               Room = a.OCB_AllocationScreen.FirstOrDefault().RoomNumber,
                               Duty = a.OCB_AllocationScreen.FirstOrDefault().Duty,
                               FlightNumber = a.Flt_No,
                               Status = a.OCB_AllocationScreen.FirstOrDefault().Status,
                               FlightId = a.Id,
                               FlightDate = a.OCB_Blocks.Block_Date.Value
                           })
                           .OrderBy(a => a.CheckInTime)
                           .ToList();

                    // Group by BlockId and select the first record from each group
                    var uniqueFlights = flights.Where(a=>a.Duty!="OPS")
                        .GroupBy(f => f.BlockId)
                        .Select(g => g.First())
                        .ToList();

                    List<DailyTimeRecordVM> dailyTimeRecordVM = new List<DailyTimeRecordVM>();


                    // Now uniqueFlights contains only the first record for each BlockId
                    foreach (var item in uniqueFlights)
                    {
                        dailyTimeRecordVM.Add(new DailyTimeRecordVM
                        {
                            BlockId = item.BlockId,
                            CheckInTime = item.CheckInTime,
                            Duty = item.Duty ?? "",
                            FlightDate = item.FlightDate,
                            FlightId = item.FlightId,
                            FlightNumber = item.FlightNumber ?? "",
                            Id = item.Id,
                            Room = item.Room ?? "",
                            Route = item.Route ?? "",
                            Status = item.Status ?? ""
                        });
                    }


                    ViewBag.FlightDate = DateTime.Now.ToString("yyyy-MM-dd");

                    return View(dailyTimeRecordVM);

                }
                else
                {

                        var flights = db.OCB_Flights
                        .Where(a => DbFunctions.TruncateTime(a.Flight_Date) == DbFunctions.TruncateTime(FlightDate) && a.Orig == "BAH")
                        .Select(a => new DailyTimeRecordVM
                        {
                            Id = a.Id,
                            BlockId = a.BlockId.Value,
                            CheckInTime = a.OCB_Blocks.Start_Time.Value,
                            Route = a.Dest,
                            Room = a.OCB_AllocationScreen.FirstOrDefault().RoomNumber,
                            Duty = a.OCB_AllocationScreen.FirstOrDefault().Duty,
                            FlightNumber = a.Flt_No,
                            Status = a.OCB_AllocationScreen.FirstOrDefault().Status,
                            FlightId = a.Id,
                            FlightDate = a.OCB_Blocks.Block_Date.Value
                        })
                        .OrderBy(a => a.CheckInTime)
                        .ToList();

                    // Group by BlockId and select the first record from each group
                    var uniqueFlights = flights.Where(a => a.Duty == "ops")
                       .GroupBy(f => f.BlockId)
                       .Select(g => g.First())
                       .ToList();

                    List<DailyTimeRecordVM> dailyTimeRecordVM = new List<DailyTimeRecordVM>();


                    // Now uniqueFlights contains only the first record for each BlockId
                    foreach (var item in uniqueFlights)
                    {
                        dailyTimeRecordVM.Add(new DailyTimeRecordVM
                        {
                            BlockId = item.BlockId,
                            CheckInTime = item.CheckInTime,
                            Duty = item.Duty ?? "",
                            FlightDate = item.FlightDate,
                            FlightId = item.FlightId,
                            FlightNumber = item.FlightNumber ?? "",
                            Id = item.Id,
                            Room = item.Room ?? "",
                            Route = item.Route ?? "",
                            Status = item.Status ?? ""
                        });
                    }

                    ViewBag.FlightDate = FlightDate.Value.ToString("yyyy-MM-dd");

                    return View(dailyTimeRecordVM);
                }

            }
            catch (Exception ex)
            {
                // telemetry.TrackException(ex); // TODO: commented by NBS for migration to AWS
                TempData["Status"] = "Error Data";
            }

            List<OCB_Flights> list = new List<OCB_Flights>();

            ViewBag.FlightDate = DateTime.Now.ToString("yyyy-MM-dd");

            ViewBag.GetStatus = ClsStatus.ListGetStatus();

            return View(list);
        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> SaveRowModified(DailyTimeRecordVM item)
        {
            if (item == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            var checkFlightExist = db.OCB_AllocationScreen.Where(a => a.FlightId == item.FlightId).FirstOrDefault();

            if (checkFlightExist == null)
            {
                db.OCB_AllocationScreen.Add(new OCB_AllocationScreen
                {
                    Duty = item.Duty,
                    FlightId = item.FlightId,
                    RoomNumber = item.Room,
                    Status = item.Status
                });

                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Record saved successfully." });
            }
            else
            {
                checkFlightExist.Duty = item.Duty;
                checkFlightExist.RoomNumber = item.Room;
                checkFlightExist.Status = item.Status;

                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Record saved successfully." });
            }


        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<ActionResult> SaveAllModified(List<DailyTimeRecordVM> item)
        {
            if (item == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            foreach (var flight in item)
            {
                var checkFlightExist = db.OCB_AllocationScreen.Where(a => a.FlightId == flight.FlightId).FirstOrDefault();

                if (checkFlightExist == null)
                {
                    db.OCB_AllocationScreen.Add(new OCB_AllocationScreen
                    {
                        Duty = flight.Duty,
                        FlightId = flight.FlightId,
                        RoomNumber = flight.Room,
                        Status = flight.Status
                    });

                    await db.SaveChangesAsync();


                }
                else
                {
                    checkFlightExist.Duty = flight.Duty;
                    checkFlightExist.RoomNumber = flight.Room;
                    checkFlightExist.Status = flight.Status;

                    await db.SaveChangesAsync();


                }
            }

            return Json(new { success = true, message = "Record saved successfully." });



        }

        [HttpPost]
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> DeleteRowModified(DailyTimeRecordVM item)
        {
            if (item == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }
            
                var checkFlightExist = db.OCB_AllocationScreen.Where(a => a.FlightId == item.FlightId).FirstOrDefault();

                if (checkFlightExist != null)
                {
                             
                    db.OCB_AllocationScreen.Remove(checkFlightExist);
                    db.SaveChanges();
                }

            return RedirectToAction("ViewEditManualAlloc", "OCB", new { FlightDate = item.FlightDate });



        }
         [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        [HttpPost]
        public async Task<JsonResult> DeleteBlock(int BlockId, DateTime Block_Date)
        {
            var blockDetails = db.OCB_Blocks.Where(a => a.Id == BlockId).FirstOrDefault();


            if (blockDetails != null)
            {
                String BlockNo = blockDetails.Block_No;
                var deleteSetting = db.Settings.Where(a => a.Title == "BlockDeleteIntervel" && a.isDeleted == false).FirstOrDefault();
                var getUser = await _graphUserService.GetAccountInfo(_userHelper.GetUser());

                String StaffNumber = "";

                if (getUser != null)
                {
                    StaffNumber = getUser.OnPremisesSamAccountName ?? string.Empty;
                    Session["FullName"] = getUser.GivenName + " " + getUser.Surname;
                }
                try
                {
                    // Delete records from PreFlight_TestLog
                    var preFlightTestLogsToDelete = (from log in db.PreFlight_TestLog
                                                     join crewTest in db.PreFlight_CrewTest on log.CrewTestId equals crewTest.Id
                                                     join flight in db.OCB_Flights on crewTest.FlightId equals flight.Id
                                                     where flight.BlockId == BlockId
                                                     select log).Distinct().ToList();

                    db.PreFlight_TestLog.RemoveRange(preFlightTestLogsToDelete);
                    // Save changes after deleting PreFlight_TestLog

                    // Delete records from PreFlight_CrewTest
                    var preFlightCrewTestsToDelete = db.PreFlight_CrewTest
                        .Where(crewTest => db.OCB_Flights
                            .Any(flight => flight.Id == crewTest.FlightId && flight.BlockId == BlockId))
                        .ToList();

                    db.PreFlight_CrewTest.RemoveRange(preFlightCrewTestsToDelete);
                    // Save changes after deleting PreFlight_CrewTest

                    var allocationScreens = db.OCB_AllocationScreen
                        .Where(a => a.OCB_Flights.BlockId == BlockId)
                        .ToList();
                    db.OCB_AllocationScreen.RemoveRange(allocationScreens);
                    // Save changes after deleting OCB_AllocationScreen

                    // Delete from SpecialMeals
                    var specialMeals = db.SpecialMeals
                        .Where(s => s.OCB_Flights.BlockId == BlockId)
                        .ToList();
                    db.SpecialMeals.RemoveRange(specialMeals);
                    // Save changes after deleting SpecialMeals

                    // Delete from FlightMeals
                    var flightMeals = db.FlightMeals
                        .Where(fm => fm.OCB_Flights.BlockId == BlockId)
                        .ToList();
                    db.FlightMeals.RemoveRange(flightMeals);
                    // Save changes after deleting FlightMeals

                    // Delete from PreBookedMeals
                    var preBookedMeals = db.PreBookedMeals
                        .Where(s => s.OCB_Flights.BlockId == BlockId)
                        .ToList();
                    db.PreBookedMeals.RemoveRange(preBookedMeals);
                    // Save changes after deleting PreBookedMeals

                    // Delete from SSRs
                    var Ssrs = db.SSRs
                        .Where(s => s.OCB_Flights.BlockId == BlockId)
                        .ToList();
                    db.SSRs.RemoveRange(Ssrs);
                    // Save changes after deleting SSRs

                    // Delete from OCB_Flights
                    var flights = db.OCB_Flights
                        .Where(f => f.BlockId == BlockId)
                        .ToList();
                    db.OCB_Flights.RemoveRange(flights);
                    // Save changes after deleting OCB_Flights

                    // Delete from OCB_Crew_Pos_History
                    var crewPosHistoryIds = db.OCB_CrewPos
                        .Where(cp => cp.BlockId == BlockId)
                        .Select(cp => cp.Id)
                        .ToList();

                    var crewPosHistory = db.OCB_Crew_Pos_History
                        .Where(cph => crewPosHistoryIds.Contains(cph.CrewPosId))
                        .ToList();

                    db.OCB_Crew_Pos_History.RemoveRange(crewPosHistory);
                    // Save changes after deleting OCB_Crew_Pos_History

                    // Delete from OCB_CrewPos
                    var crewPositions = db.OCB_CrewPos
                        .Where(cp => cp.BlockId == BlockId)
                        .ToList();
                    db.OCB_CrewPos.RemoveRange(crewPositions);
                    // Save changes after deleting OCB_CrewPos

                    // Delete from OCB_Blocks
                    var block = db.OCB_Blocks.Find(BlockId);
                    if (block != null)
                    {
                        db.OCB_Blocks.Remove(block);
                        db.SaveChanges();
                        // Save changes after deleting OCB_Blocks
                    }

                    // Add to delete block log table
                    BlockDeleteLog blkdeletelog = new BlockDeleteLog();
                    blkdeletelog.BlockId = BlockId;
                    blkdeletelog.BlockNo = BlockNo;
                    blkdeletelog.StaffNumber = StaffNumber;
                    blkdeletelog.DeleteTime = DateTime.Now;
                    if (deleteSetting != null)
                    {
                        blkdeletelog.DeleteIntervelSetting = deleteSetting.Value;
                    }
                    blkdeletelog.BlockDate = blockDetails.Block_Date;
                    db.BlockDeleteLogs.Add(blkdeletelog);
                    db.SaveChanges(); // Save changes after adding to BlockDeleteLogs

                    return Json(new { success = true });

                }
                catch (Exception ex)
                {
                    // Log the error for debugging
                    System.Diagnostics.Debug.WriteLine($"Error deleting block: {ex.Message}");
                    return Json(new
                    {
                        success = false,
                        message = "An error occurred while deleting the block." + ex.Message
                    });
                }
            }
            else
            {
                return Json(new { success = false, message = "Block not found." });
            }
        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        public async Task<System.Web.Mvc.ActionResult> UpdateSettings()
        {
            var settings = db.Settings.ToList();
            return View(settings);
        }
        [Authorize(Roles = "CSM.Admins,CSM.Briefing")]
        [HttpPost]
        public async Task<ActionResult> SaveRowSettings(int Id,string Value)
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
                // Find the setting by Id
                var settingToUpdate = db.Settings.Find(Id);
                if (settingToUpdate != null)
                {
                    // Update the Title and Value (you might want to verify Title doesn't change)
              
                    settingToUpdate.Value = Value;
                    settingToUpdate.UpdatedBy = StaffNumber; // Or however you track the user
                    settingToUpdate.UpdatedDT = DateTime.Now;
                    db.SaveChanges();
                    TempData["Status"] = "Saved";
                    return Json(new { success = true });
                }
                else
                {
                    TempData["Status"] = "Error";
                    return Json(new { success = false, message = "Setting not found." });
                }
            }
            catch (Exception)
            {
                TempData["Status"] = "Error";
                return Json(new { success = false, message = "Error saving setting." });
            }
        }

        [HttpPost]
        public async Task<ActionResult> SaveAllSettings(List<YourSaveViewModel> allSettings) // Replace YourSaveViewModel with the actual type
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
                foreach (var settingData in allSettings)
                {
                    var settingToUpdate = db.Settings.Find(settingData.Id);
                    if (settingToUpdate != null)
                    {
                     
                        settingToUpdate.Value = settingData.Value;
                        settingToUpdate.UpdatedBy = StaffNumber;
                        settingToUpdate.UpdatedDT = DateTime.Now;
                    }
                    // Optionally handle cases where setting is not found
                }
                db.SaveChanges();
                TempData["Status"] = "Saved";
                return Json(new { success = true });
            }
            catch (Exception)
            {
                TempData["Status"] = "Error";
                return Json(new { success = false, message = "Error saving all settings." });
            }
        }

        public class YourSaveViewModel
        {
            public int Id { get; set; }
            
            public string Value { get; set; }
        }
    }
}