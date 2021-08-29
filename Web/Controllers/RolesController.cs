using HlidacStatu.Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Controllers
{
    [Authorize]
    public partial class RolesController : Controller
    {
        private readonly DbEntities _context = new();
        private readonly UserManager<Entities.ApplicationUser> _userManager;

        public RolesController(UserManager<Entities.ApplicationUser> userManager)
        {
            _userManager = userManager;
        }


        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            // Populate Dropdown Lists

            var rolelist = _context.Roles.AsNoTracking().OrderBy(r => r.Name).ToList().Select(rr =>
            new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = rolelist;

            var userlist = _context.Users.AsNoTracking().OrderBy(u => u.UserName).ToList().Select(uu =>
            new SelectListItem { Value = uu.UserName.ToString(), Text = uu.UserName }).ToList();
            ViewBag.Users = userlist;

            ViewBag.Message = "";

            return View();
        }


        // GET: /Roles/Create
        [Authorize(Roles = "Admin")]

        public ActionResult Create()
        {
            return View(nameof(Index));
        }

        //
        // POST: /Roles/Create
        [Authorize(Roles = "Admin")]

        [HttpPost]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                _context.Roles.Add(new IdentityRole(collection["RoleName"]));
                _context.SaveChanges();
                ViewBag.Message = "Role created successfully !";
                return RedirectToAction("Index");
            }
            catch
            {
                return View(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]

        public ActionResult Delete(string RoleName)
        {
            var thisRole = _context.Roles
                .AsNoTracking()
                .FirstOrDefault(r => r.Name.Equals(RoleName));
            _context.Roles.Remove(thisRole);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        //
        // GET: /Roles/Edit/5
        public ActionResult Edit(string roleName)
        {
            var thisRole = _context.Roles
                .AsNoTracking()
                .FirstOrDefault(r => r.Name.Equals(roleName));
            var rolelist = _context.Roles.AsNoTracking().OrderBy(r => r.Name).ToList().Select(rr =>
new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = rolelist;

            var userlist = _context.Users.AsNoTracking().OrderBy(u => u.UserName).ToList().Select(uu =>
new SelectListItem { Value = uu.UserName.ToString(), Text = uu.UserName }).ToList();
            ViewBag.Users = userlist;

            ViewBag.Message = "";

            return View(thisRole);
        }

        //
        // POST: /Roles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(IdentityRole role)
        {
            try
            {
                _context.Entry(role).State = EntityState.Modified;
                _context.SaveChanges();

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }


        //  Adding Roles to a user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RoleAddToUser(string UserName, string RoleName)
        {
            if (_context == null)
            {
                throw new ArgumentNullException("context", "Context must not be null.");
            }

            var user = await _userManager.FindByEmailAsync(UserName);

            await _userManager.AddToRoleAsync(user, RoleName);

            ViewBag.Message = "Role created successfully !";

            // Repopulate Dropdown Lists
            var rolelist = _context.Roles.AsNoTracking().OrderBy(r => r.Name).ToList()
                .Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = rolelist;
            var userlist = _context.Users.AsNoTracking().OrderBy(u => u.UserName).ToList()
                .Select(uu => new SelectListItem { Value = uu.UserName.ToString(), Text = uu.UserName }).ToList();
            ViewBag.Users = userlist;

            return View("Index");
        }


        //Getting a List of Roles for a User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetRoles(string UserName)
        {
            if (!string.IsNullOrWhiteSpace(UserName))
            {
                var user = await _userManager.FindByEmailAsync(UserName);

                ViewBag.RolesForThisUser = await _userManager.GetRolesAsync(user);

                // Repopulate Dropdown Lists
                var rolelist = _context.Roles.AsNoTracking().OrderBy(r => r.Name).ToList()
                    .Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
                ViewBag.Roles = rolelist;
                var userlist = _context.Users.AsNoTracking().OrderBy(u => u.UserName).ToList()
                    .Select(uu => new SelectListItem { Value = uu.UserName.ToString(), Text = uu.UserName }).ToList();
                ViewBag.Users = userlist;
                ViewBag.Message = "Roles retrieved successfully !";
            }

            return View("Index");
        }

        //Deleting a User from A Role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteRoleForUser(string UserName, string RoleName)
        {
            var user = await _userManager.FindByEmailAsync(UserName);


            if (await _userManager.IsInRoleAsync(user, RoleName))
            {
                await _userManager.RemoveFromRoleAsync(user, RoleName);
                ViewBag.Message = "Role removed from this user successfully !";
            }
            else
            {
                ViewBag.Message = "This user doesn't belong to selected role.";
            }

            // Repopulate Dropdown Lists
            var rolelist = _context.Roles.AsNoTracking().OrderBy(r => r.Name).ToList()
                .Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = rolelist;
            var userlist = _context.Users.AsNoTracking().OrderBy(u => u.UserName).ToList()
                .Select(uu => new SelectListItem { Value = uu.UserName.ToString(), Text = uu.UserName }).ToList();
            ViewBag.Users = userlist;

            return View("Index");
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            base.Dispose(disposing);
        }
    }
}