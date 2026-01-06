using HlidacStatu.Datasets;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.Util;
using HlidacStatu.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.KIndex;
using Serilog;
using HlidacStatu.LibCore.Filters;

namespace HlidacStatu.Web.Controllers
{
    [Authorize]
    public partial class ManageController : Controller
    {
        private readonly UserManager<Entities.ApplicationUser> _userManager;
        private readonly ILogger _logger = Log.ForContext<ManageController>();

        public ManageController(UserManager<Entities.ApplicationUser> userManager, SignInManager<Entities.ApplicationUser> signInManager)
        {
            _userManager = userManager;
        }
        
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            if (!string.IsNullOrEmpty(Request.Query["msg"]))
            {
                ViewBag.StatusMessage = Request.Query["msg"];
            }
            var user = await _userManager.GetUserAsync(User);
            IndexViewModel model = null;
            using (var db = new DbEntities())
            {
                model = new IndexViewModel
                {
                    HasPassword = await HasPassword(),
                    PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                    TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                    Logins = await _userManager.GetLoginsAsync(user),
                    HasWatchdogs = db.WatchDogs.AsNoTracking().Any(m => m.UserId == user.Id)
                };
            }
            return View(model);
        }

        public ActionResult Firmy2ICO()
        {
            List<Firma> res = null;
            string jmena = "";
            if (Request.HasFormContentType)
                jmena = Request.Form["names"];

            if (!string.IsNullOrWhiteSpace(jmena))
            {
                string[] names = jmena.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                res = new List<Firma>();
                foreach (var n in names)
                {
                    Firma f = FirmaRepo.FirmaInText(n);
                    if (f != null && f.Valid)
                        res.Add(f);
                    else
                        res.Add(new Firma() { Jmeno = n, ICO = "" });
                }
            }
            return View(res);
        }

        public ActionResult ICO2Firmy([FromForm] string names)
        {
            List<Firma> res = null;
            string jmena = names;//Request.Form.Keys.Contains("names") ? Request.Form["names"] : "";
            if (!string.IsNullOrWhiteSpace(jmena))
            {
                string[] icos = jmena.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                res = new List<Firma>();
                foreach (var ic in icos)
                {
                    Firma f = Firmy.Get(ic);
                    if (f != null && f.Valid)
                        res.Add(f);
                    else
                        res.Add(new Firma() { Jmeno = ic, ICO = ic });
                }
            }
            return View(res);
        }
        public ActionResult ClassifAnalysis()
        {
            return View();
        }
       

        [Authorize(Roles = "canEditData")]
        public ActionResult AddKindexFeedback(string ico, int year)
        {
            if (string.IsNullOrWhiteSpace(ico))
                return RedirectToAction(nameof(HomeController.Index), "Home");

            var kindexFeedback = new KindexFeedback()
            {
                Ico = ico,
                Year = year
            };
            return View(kindexFeedback);
        }

        // set classification
        [Authorize(Roles = "canEditData")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddKindexFeedback(KindexFeedback feedback)
        {
            if (ModelState.IsValid)
            {
                await feedback.SaveAsync();
            }

            return RedirectToAction(
                nameof(KindexController.Detail),
                nameof(KindexController).Replace("Controller", ""),
                new { id = feedback.Ico, rok = feedback.Year });
        }

        public ActionResult SubjektHlidac(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                return NotFound();
            Firma model = Firmy.Get(Id);
            if (model == null || !Firma.IsValid(model))
            {
                return NotFound();
            }
            else
            {
                var aktualnost = Relation.AktualnostType.Nedavny;
                ViewBag.Aktualnost = aktualnost;
                return View(model);
            }
        }

        [HttpPost()]
        public ActionResult SubjektHlidac(string Id, IFormCollection form)
        {
            bool existing = false;
            string wd_id = form["wd_id"];
            int wdId;
            string subject = form["subjekt"];
            using (DbEntities db = new DbEntities())
            {

                string id = _userManager.GetUserId(User);
                WatchDog wd = null;
                if (!string.IsNullOrEmpty(wd_id) && int.TryParse(wd_id, out wdId))
                {
                    wd = WatchdogRepo.Load(wdId);
                    existing = wd != null;
                }
                string query = "";
                if (string.IsNullOrEmpty(form["ico"]) && wd != null)
                {
                    wd.Delete();
                    return RedirectToAction("Watchdogs", "Manage");
                }
                else if (string.IsNullOrEmpty(form["ico"]))
                    return Redirect("/Subjekt/" + id);

                query = string.Join(" OR ", form["ico"].ToString().Split(',').Select(m => "ico:" + m));

                if (wd == null)
                    wd = new WatchDog();

                wd.Created = DateTime.Now;
                wd.UserId = id;
                wd.StatusId = 1;
                wd.SearchTerm = query;
                wd.SearchRawQuery = "icoVazby:" + form["ico"];
                wd.PeriodId = Convert.ToInt32(form["period"]);
                wd.FocusId = Convert.ToInt32(form["focus"]);
                wd.Name = Devmasters.TextUtil.ShortenText(form["wdname"], 50);
                wd.Save();
                return RedirectToAction("Watchdogs", "Manage");
            }


        }


        [Authorize(Roles = "canEditData")]
        public ActionResult WatchdogsAdminList()
        {
            return View();
        }
        [Authorize(Roles = "canEditData")]
        public async Task<ActionResult> EditSmlouva(string Id, string type)
        {
            object item = await SmlouvaRepo.LoadAsync(Id, includePlaintext:false);
            if (item != null)
            {
                ViewBag.objectType = type;
                ViewBag.objectId = Id;
                return View(item);
            }
            else
                return NotFound();
        }


        [Authorize(Roles = "canEditData")]
        [HttpPost]
        public async Task<ActionResult> EditSmlouva(string Id, IFormCollection form)
        {
            string newJson = form["jsonRaw"];
            Smlouva s = Newtonsoft.Json.JsonConvert.DeserializeObject<Smlouva>(newJson);
            await SmlouvaRepo.SaveAsync(s, fireOCRDone:true);

            return Redirect("Index");
        }

        [Authorize(Roles = "canEditData")]
        public ActionResult AddPersons()
        {
            return View();
        }

        [Authorize(Roles = "canEditData")]
        [HttpPost]
        public async Task<ActionResult> AddPersons(IFormCollection form)
        {
            List<string> newIds = new List<string>();
            string tabdelimited = form["data"];
            foreach (var line in tabdelimited.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] cols = line.Split(new string[] { "\t", "|" }, StringSplitOptions.None);

                // Vždy musí být řádek o 13 sloupcích. Povinné položky jsou:
                // varianta a) jmeno, prijmeni, narozeni
                // varianta b) fullname, narozeni

                if (cols.Count() != 13)
                    continue;
                string fullName = cols[0];
                string jmeno = cols[1];
                string prijmeni = cols[2];
                string titulPred = cols[3];
                string titulPo = cols[4];
                DateTime? narozeni = Devmasters.DT.Util.ToDate(cols[5]);

                Osoba.StatusOsobyEnum status = GetStatusFromText(cols[6]);

                string clenstviStrana = ParseTools.NormalizaceStranaShortName(cols[7]);
                DateTime? clenstviVznik = Devmasters.DT.Util.ToDate(cols[8]);

                string eventOrganizace = cols[9];
                string eventRole = cols[10];
                DateTime? eventVznik = Devmasters.DT.Util.ToDate(cols[11]);
                string eventTyp = cols[12];

                // set person from fulltext when not properly defined
                if (string.IsNullOrWhiteSpace(jmeno) || string.IsNullOrWhiteSpace(prijmeni))
                {
                    if (string.IsNullOrWhiteSpace(fullName))
                        continue;

                    var osoba = Validators.JmenoInText(fullName);

                    if (osoba is null)
                        continue;
                    if (string.IsNullOrWhiteSpace(jmeno))
                        jmeno = osoba.Jmeno;
                    if (string.IsNullOrWhiteSpace(prijmeni))
                        prijmeni = osoba.Prijmeni;
                    if (string.IsNullOrWhiteSpace(titulPred))
                        titulPred = osoba.TitulPred;
                    if (string.IsNullOrWhiteSpace(titulPo))
                        titulPo = osoba.TitulPo;
                }

                // when there is no narozeni Date, then we are not going to save person...
                if (!narozeni.HasValue)
                    continue;

                Osoba p = OsobaRepo.GetOrCreateNew(titulPred, jmeno, prijmeni, titulPo, narozeni, status,
                    User.Identity?.Name);

                if (!string.IsNullOrWhiteSpace(clenstviStrana))
                {
                    OsobaEvent clenStrany = new OsobaEvent
                    {
                        OsobaId = p.InternalId,
                        DatumOd = clenstviVznik,
                        Type = 14,
                        AddInfo = "člen strany",
                        Organizace = clenstviStrana,
                        Title = $"člen v {clenstviStrana}"
                    };

                    await OsobaEventRepo.CreateOrUpdateAsync(clenStrany, User.Identity?.Name);
                }


                if (int.TryParse(eventTyp, out int typ)
                    && !string.IsNullOrWhiteSpace(eventRole)
                    && !string.IsNullOrWhiteSpace(eventOrganizace))
                {
                    OsobaEvent dalsiEvent = new OsobaEvent
                    {
                        OsobaId = p.InternalId,
                        DatumOd = eventVznik,
                        Type = typ,
                        AddInfo = eventRole,
                        Organizace = eventOrganizace,
                        Title = $"{eventRole} v {eventOrganizace}"
                    };

                    await OsobaEventRepo.CreateOrUpdateAsync(dalsiEvent, User.Identity?.Name);
                }


            }
            return View(newIds);

        }

        /// <summary>
        /// Assigns StatusOsoby based on text. 
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Returns StatusOsobyEnum.Politik if string is empty or invalid (not a number)</returns>
        private Osoba.StatusOsobyEnum GetStatusFromText(string text)
        {
            Osoba.StatusOsobyEnum statusOsoby = Osoba.StatusOsobyEnum.Politik;

            try
            {
                if (int.TryParse(text, out int statx))
                {
                    statusOsoby = (Osoba.StatusOsobyEnum)statx;
                }
            }
            catch { }

            return statusOsoby;
        }

        [Authorize(Roles = "canEditData")]
        public ActionResult ShowClassification(string id, bool force = false)
        {

            return View(new Tuple<string, bool>(id, force));
        }

        [Authorize(Roles = "canEditData")]
        public ActionResult Reviews(string id, string a, string reason)
        {
            var model = new List<Review>();
            //show list
            using (DbEntities db = new DbEntities())
            {
                if (!string.IsNullOrEmpty(id)
                    && !string.IsNullOrEmpty(a)
                    && Devmasters.TextUtil.IsNumeric(id)
                    )
                {
                    var iId = Convert.ToInt32(id);
                    var item = db.Review.FirstOrDefault(m => m.Id == iId);
                    if (item != null)
                    {
                        switch (a.ToLower())
                        {
                            case "accepted":
                                item.Accepted(User.Identity?.Name);
                                break;
                            case "denied":
                                item.Denied(User.Identity?.Name, reason);
                                break;
                            default:
                                break;
                        }
                    }
                    return RedirectToAction("Reviews", "Manage");
                }
                else
                {
                    model = db.Review.AsNoTracking()
                            .Where(m => m.Reviewed == null)
                            .ToList();
                }
            }

            return View(model);
        }


        static string[] WatchdogTypes = new string[]{
                    WatchDog.AllDbDataType,
                    nameof(Smlouva),
                    nameof(VerejnaZakazka),
                    nameof(DataSet),
                };

        [HttpPost]
        public async Task<ActionResult> WatchdogsSett(IFormCollection form)
        {
            var user = await _userManager.GetUserAsync(User);
            user.SentWatchdogOneByOne = (form["allinone"] != "on");
            return Redirect("Watchdogs?rnd=" + Util.Consts.Rnd.Next(1, 10000));
        }

        [HttpGet, ActionName("WatchdogsSett")]
        public ActionResult WatchdogsSett_get()
        {
            return Redirect("Watchdogs");
        }

        public ActionResult Watchdogs(string id, string wid, string disable, string enable, string delete)
        {
            string currWDtype = null;
            if (!string.IsNullOrEmpty(id))
            {
                if (WatchdogTypes.Any(w => w.ToLower() == id.ToLower()))
                    currWDtype = id;
            }
            //if (currWDtypes == null)
            //    currWDtypes = WatchdogTypes;

            List<WatchDog> wds = new List<WatchDog>();
            using (DbEntities db = new DbEntities())
            {
                string userid = _userManager.GetUserId(User);

                if (!string.IsNullOrEmpty(wid))
                {
                    int watchdogid = Convert.ToInt32(wid);
                    WatchDog wd;
                    wd = db.WatchDogs.AsQueryable()
                        .Where(m => m.Id == watchdogid && m.UserId == userid)
                        .FirstOrDefault();

                    if (wd == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        if (disable == "1")
                        {
                            wd.StatusId = 0;
                            wd.Save();
                        }
                        if (enable == "1")
                        {
                            wd.StatusId = 1;
                            wd.Save();
                        }
                        if (delete == "1")
                            wd.Delete();

                        return RedirectToAction("Watchdogs");
                    }
                }
                else
                {
                    wds.AddRange(
                        db.WatchDogs.AsNoTracking()
                            .Where(m =>
                            (m.DataType == currWDtype || currWDtype == null)
                            && m.UserId == userid)
                        );
                }

            }
            return View(wds);

        }

        public ActionResult OutOfWatchdogs()
        {
            return View();
        }


        [AllowAnonymous]
        public ActionResult ChangePhoto(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction(nameof(OsobyController.Index), "Osoby");

            var o = Osoby.GetByNameId.Get(id);
            if (o == null)
                return NotFound();

            ViewBag.Phase = "start";
            ViewBag.Osoba = o;
            return View();
        }
        

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> ChangePhoto(string id, IFormCollection form, IFormFile file1)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return RedirectToAction(nameof(OsobyController.Index), "Osoby");

                var o = Osoby.GetByNameId.Get(id);

                if (o == null)
                    return NotFound();
                ViewBag.Osoba = o;

                if (form["phase"] == "start") //upload
                {
                    byte[] data = null;
                    var path = o.GetPhotoPath(Osoba.PhotoTypes.UploadedOriginal, true); //, "original.uploaded.jpg");
                    var pathTxt = o.GetPhotoPath(Osoba.PhotoTypes.SourceOfPhoto, true);// Init.OsobaFotky.GetFullPath(o, "source.txt");
                    var source = form["source"].ToString();
                    string[] facesFiles = new string[] { };
                    if (!string.IsNullOrEmpty(form["url"]))
                    {
                        try
                        {
                            data = new System.Net.WebClient().DownloadData(form["url"]);
                            source = form["url"];
                        }
                        catch (Exception)
                        {
                            data = null;
                        }
                    }
                    else if (file1.Length > 0)
                    {

                        using (var binaryReader = new BinaryReader(file1.OpenReadStream()))
                        {
                            data = binaryReader.ReadBytes((int)file1.Length);
                        }
                    }
                    try
                    {
                        facesFiles = DetectFaces.FromImage.DetectAndParseFacesIntoFiles(data, 150, 40).ToArray();
                        if (data != null && facesFiles.Length > 0)
                        {
                            if (!string.IsNullOrWhiteSpace(source))
                                System.IO.File.WriteAllText(pathTxt, source);
                            System.IO.File.WriteAllBytes(path, data);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "PhotoChange processing");
                    }

                    ViewBag.Osoba = o;
                    ViewBag.Phase = "choose";
                    ViewBag.Email = form["email"].ToString();
                    return View("ChangePhoto_Choose", facesFiles);

                } //upload
                else if (form["phase"] == "choose") //upload
                {
                    string fn = form["fn"];

                    if (!string.IsNullOrEmpty(fn) && fn.Contains(Path.GetTempPath()))
                    {
                        if (System.IO.File.Exists(fn))
                        {
                            //C:\Windows\TEMP\tmp3EB0.tmp.0.faces.jpg
                            var rootfn = Devmasters.RegexUtil.GetRegexGroupValue(fn, @"(?<tempfn>.*)\.\d{1,2}\.faces\.jpg$", "tempfn");
                            var target = o.GetPhotoPath(Osoba.PhotoTypes.UploadedSmall, true) ;//Init.OsobaFotky.GetFullPath(o, "small.uploaded.jpg");
                            try
                            {
                                using (Devmasters.Imaging.InMemoryImage imi = new Devmasters.Imaging.InMemoryImage(fn))
                                {
                                    Devmasters.IO.IOTools.DeleteFile(fn);
                                    if (User?.IsInRole("Admin") == true)
                                    {
                                        //Devmasters.IO.IOTools.MoveFile(fn, HlidacStatu.Lib.Init.OsobaFotky.GetFullPath(o, "small.jpg"));
                                        imi.Resize(new SixLabors.ImageSharp.Size(300, 300), true, Devmasters.Imaging.InMemoryImage.InterpolationsQuality.High, true);
                                        imi.SaveAsJPEG(o.GetPhotoPath(), 80);
                                        var noBackGr = await HlidacStatu.AI.Photo.RemoveBackgroundAsync(
                                                    new Uri(Devmasters.Config.GetWebConfigValue("RemoveBackgroundAPI")),
                                                    System.IO.File.ReadAllBytes(o.GetPhotoPath()),
                                                    AI.Photo.RemoveBackgroundStyles.Person);
                                        if (noBackGr != null)
                                            System.IO.File.WriteAllBytes(o.GetPhotoPath( Osoba.PhotoTypes.NoBackground, true), noBackGr);

                                            return Redirect("/Osoba/" + o.NameId);
                                    }
                                    else
                                    {
                                        imi.SaveAsJPEG(target, 80);
                                        //Devmasters.IO.IOTools.MoveFile(fn, HlidacStatu.Lib.Init.OsobaFotky.GetFullPath(o, "small.review.jpg"));
                                        //Devmasters.IO.IOTools.MoveFile(HlidacStatu.Lib.Init.OsobaFotky.GetFullPath(o, "uploaded.jpg"), HlidacStatu.Lib.Init.OsobaFotky.GetFullPath(o, "original.uploaded.jpg"));
                                        using (DbEntities db = new DbEntities())
                                        {
                                            var r = new Review()
                                            {
                                                Created = DateTime.Now,
                                                CreatedBy = form["email"].ToString(),
                                                itemType = "osobaPhoto",
                                                NewValue = Newtonsoft.Json.JsonConvert.SerializeObject(new { nameId = o.NameId, file = target }),
                                            };

                                            db.Review.Add(r);
                                            using (System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient())
                                            {
                                                smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                                                smtp.Send("info@hlidacstatu.cz", "michal@michalblaha.cz", "Photo review",
                                                    Newtonsoft.Json.JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented)
                                                    );
                                            }
                                            db.SaveChanges();
                                        }
                                        return View("ChangePhoto_finish", o);
                                    }
                                }
                            }
                            finally
                            {
                                try
                                {
                                    foreach (var f in Directory.EnumerateFiles(Path.GetDirectoryName(rootfn), Path.GetFileName(rootfn) + ".*"))
                                    {
                                        Devmasters.IO.IOTools.DeleteFile(f);
                                    }
                                }
                                catch
                                {
                                }
                            }

                        }

                    }
                }

                //smazat fotku
                if (form["phase"] == "delete"
                    && (User.IsInRole("Admin") || User.IsInRole("Editor")))
                {
                    var path = o.GetPhotoPath();

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    return RedirectToAction("Index", "Osoba", new { id = o.NameId });
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "PhotoChange");
                return View();
            }
            return View();
        }

        [HlidacOutputCache(60 * 10, null, false)]
        public ActionResult AllOsobaPhotos()
        {
            return View();
        }



        public ActionResult Zalozky()
        {
            using (DbEntities db = new DbEntities())
            {
                var bookmarks = db.Bookmarks
                    .AsNoTracking()
                    .Where(m => m.UserId == User.Identity.Name)
                    .OrderByDescending(m => m.Created)
                    .ToArray();
                return View(bookmarks);
            }

        }

        #region Helpers

        private async Task<bool> HasPassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }


        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        #endregion
    }
}