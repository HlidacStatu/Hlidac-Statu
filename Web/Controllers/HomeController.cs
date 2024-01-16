using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Devmasters;
using Devmasters.Enums;
using HlidacStatu.Connectors;
using HlidacStatu.Datasets;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Extensions;
using HlidacStatu.LibCore.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Analysis;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Framework;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Nest;
using Serilog;
using Visit = HlidacStatu.Web.Framework.Visit;


namespace HlidacStatu.Web.Controllers
{
    public partial class HomeController : Controller
    {
        private readonly ILogger _logger = Log.ForContext<HomeController>();

        private readonly UserManager<ApplicationUser> _userManager;
        protected readonly IWebHostEnvironment _hostingEnvironment;

        public HomeController(IWebHostEnvironment hostingEnvironment, UserManager<ApplicationUser> userManager)
        {
            _hostingEnvironment = hostingEnvironment;
            _userManager = userManager;
        }
        public async Task<ActionResult> MenuPage(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return Redirect("/");
            return View((object)id);

        }
        [Authorize]
        public async Task<ActionResult> Kod(string? id)
        {

            if (id != null && id?.ToLower()?.RemoveAccents()?.Equals("lizatko") == true)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                await _userManager.AddToRoleAsync(user, "TK-KIndex-2021");

                return View(true);
            }

            return View(false);


        }

        [HttpPost]
        public ActionResult Analyza(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return View((TemplatedQuery)null);

            string[] ids_arr = ids.Split(new string[] { ",", ";", " ", Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
            ids_arr = ids_arr.Select(m => m.Trim()).Where(s => s.Length > 0).ToArray();

            if (ids_arr.Length == 0)
                return View((TemplatedQuery)null);

            var q = $"id:( {string.Join(" OR ", ids_arr)} )";
            return Analyza("", q, "", "", "",null);
        }
        [HttpGet]
        public ActionResult Analyza(string p, string q, string title, string description, string moreUrl, int? y)
        {
            ViewData.Add(Constants.CacheKeyName,
                WebUtil.GenerateCacheKey(new object[] { p, q, title, description, moreUrl, y }));

            var model = new TemplatedQuery() { Query = q, Text = title, Description = description };


            if (StaticData.Afery.ContainsKey(p?.ToLower() ?? ""))
                model = StaticData.Afery[p.ToLower()];
            else if (!string.IsNullOrEmpty(q))
            {
                model = new TemplatedQuery() { Query = q, Text = title, Description = description };
                if (Uri.TryCreate(moreUrl, UriKind.Absolute, out var uri))
                    model.Links = new TemplatedQuery.AHref[] {
                        new(uri.AbsoluteUri,"více informací")
                    };
            }

            return View(model);
        }

        public const string NoPhotoSvg = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 1.1//EN"" ""http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"">
<svg width=""100%"" height=""100%"" viewBox=""0 0 256 256"" version=""1.1"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" xml:space=""preserve"" xmlns:serif=""http://www.serif.com/"" style=""fill-rule:evenodd;clip-rule:evenodd;stroke-linejoin:round;stroke-miterlimit:2;"">
    <rect x=""-0"" y=""-0"" width=""256"" height=""256"" style=""fill:rgb(190,190,190);""/>
    <g transform=""matrix(1.08475,0,0,1.04237,-10.8475,-0.423729)"">
        <g>
            <path d=""M246,246C246,246 247.3,229.8 246,221.3C244.9,214.6 234.3,205.7 194.9,191.2C156.1,176.9 158.5,183.9 158.5,157.7C158.5,140.7 167.2,150.6 172.7,118.3C174.8,105.6 176.6,114.1 181.2,93.7C183.7,83 179.5,82.2 180,77.1C180.5,72 181,67.4 181.9,57C183,44 171,10 128,10C85,10 73,44 74.2,57C75.1,67.4 75.6,72 76.1,77.1C76.6,82.2 72.5,83 74.9,93.7C79.6,114 81.3,105.6 83.4,118.3C88.9,150.6 97.6,140.7 97.6,157.7C97.6,184 100,177.1 61.2,191.2C21.8,205.6 11,214.6 10,221.3C8.6,229.8 10,246 10,246L246,246Z"" style=""fill:rgb(240,240,240);fill-rule:nonzero;""/>
        </g>
    </g>
</svg>
";
        public const string NoPhotoAnonymousSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""560.508"" height=""726.375"" xmlns:v=""https://vecta.io/nano""><path d=""M284.868 715.716c0 1.729 19.437.233 26.719-2.056 22.437-7.055 53.834-27.493 87.223-56.78 29.5-25.875 89.805-91.452 104.219-113.33 5.883-8.93 16.184-30.635 20.281-42.736 13.138-38.806 21.033-83.595 25.614-145.312 2.041-27.497 2.336-105.572.499-132.187-6.08-88.103-18.171-146.109-35.376-169.712-12.073-16.563-69.556-31.341-152.772-39.274-49.004-4.672-77.512-6.046-99.431-4.793C148.017 16.041 71.502 30.79 49.649 50.438 18.567 78.383.319 248.816 13.974 383.626c6.19 61.112 17.413 108.908 33.174 141.289 7.572 15.557 12.406 22.684 27.899 41.132 69.748 83.056 134.205 137.003 177.477 148.538 6.042 1.611 22.031 2.578 22.031 1.332z"" fill=""none"" stroke=""#000"" stroke-width=""10""/><path d=""M270.933 710.059c-12.112-17.856-17.725-38.185-18.597-67.355-.599-20.031.325-30.539 4.098-46.591 1.689-7.188 2.279-11.915 2.011-16.125-.346-5.434-.728-6.351-4.358-10.456-5.989-6.773-11.518-15.451-11.211-17.595.327-2.282 1.195-2.303 22.484-.544 12.075.997 17.199.996 28.594-.009 20.572-1.814 21.321-1.785 21.693.843.35 2.473-4.488 10.299-10.194 16.49-1.992 2.161-4.064 5.091-4.604 6.511-1.507 3.963-1.174 10.701.885 17.899 4.293 15.012 6.347 39.783 4.771 57.527-2.143 24.123-8.466 44.525-18.349 59.212-5.155.942-8.359 4.813-17.222.193zM155.452 587.297c-38.9-40.408-69.025-82.061-90.911-125.7-13.138-26.197-24.483-56.71-27.553-74.106l-.558-3.165 4.555 4.419c8.326 8.077 18.744 13.672 30.858 16.572 11.353 2.718 30.138 2.066 43.806-1.519 16.356-4.291 34.615-12.374 51.38-22.743l8.455-5.23 2.641 2.956c3.543 3.965 4.221 7.122 2.366 11.012-4.051 8.494-54.169 25.708-74.85 25.708-2.622 0-4.522.427-4.522 1.016 0 .559 4.113 8.481 9.141 17.605l11.945 22.187c1.542 3.079 4.917 7.77 7.5 10.426l4.696 4.828 27.674.673c31.412.764 34.483.372 46.232-5.898 7.516-4.011 20.198-15.943 27.217-25.607l4.486-6.176 3.446 1.693c13.189 6.478 58.408 6.47 71.606-.013l3.472-1.706 5.682 7.33c13.069 16.86 25.142 26.082 38.687 29.552 4.998 1.28 9.727 1.393 33.679.804l27.865-.685 4.656-4.68c2.561-2.574 6.048-7.482 7.751-10.906s7.226-13.732 12.276-22.907l9.181-16.971c0-.159-3.691-.533-8.203-.831-10.325-.682-22.83-3.453-37.266-8.256-13.03-4.336-31.049-12.76-33.703-15.758-2.981-3.367-2.407-8.017 1.467-11.892l3.295-3.295 8.377 5.287c18.031 11.381 42.358 21.259 59.827 24.295 9.719 1.689 24.132 1.879 32.049.424 12.244-2.251 25.345-9.025 34.86-18.026 1.676-1.585 3.047-2.406 3.047-1.824 0 3.021-6.103 24.046-10.17 35.036-21.35 57.692-58.738 114.977-107.282 164.374l-14.796 15.055 16.757-25.312c25.776-38.938 42.392-66.38 41.022-67.749-.252-.252-2.645 1.507-5.318 3.91-10.635 9.561-30.087 20.693-44.22 25.304-7.472 2.438-22.231 5.579-22.93 4.881-.215-.215 2.067-1.076 5.07-1.915 26.096-7.286 54.39-25.677 73.036-47.473 5.922-6.922 8.046-10.775 24.421-44.301l17.669-37.019c-.659-.744-7.806 3.695-9.076 5.636-.778 1.19-7.643 13.554-15.255 27.476-18.323 33.511-20.76 37.624-25.051 42.28-5.067 5.497-12.483 10.398-20.084 13.273l-6.449 2.439-48.407.195-48.407.195-11.593-13.581c-6.376-7.469-14.207-17.23-17.402-21.691-6.493-9.065-8.276-9.958-17.797-8.913-4.629.508-5.011.768-8.191 5.557-4.09 6.16-17.484 22.902-25.545 31.93l-5.968 6.685-48.033-.217c-42.674-.193-48.608-.399-53.189-1.85-9.959-3.154-18.79-9.508-25.515-18.357-1.381-1.818-9.213-15.529-17.403-30.469-21.233-38.731-20.226-37.108-24.216-39.048-1.924-.936-3.686-1.514-3.915-1.285s7.739 17.137 17.708 37.572l18.124 37.156 9.297 9.81c15.52 16.377 29.785 26.978 47.804 35.525 5.753 2.729 13.624 5.88 17.491 7.003 9.778 2.839 9.7 2.811 9.156 3.354-.808.808-17.576-3.023-25.094-5.733-13.806-4.976-29.224-13.896-41.016-23.73-3.223-2.688-5.859-4.38-5.859-3.761 0 3.143 16.887 30.803 40.28 65.974 17.699 26.61 17.166 25.781 16.585 25.781-.238 0-6.018-5.801-12.843-12.891zm123.829-78.984l-67.195.789c-.862.101-1.375.211-1.5.336-.781.781-1.155 1.466-1.074 2.098-.067.519.157.911.697 1.359.661.549 2.494 1.191 5.025 1.834l.135.039 7.686 1.678.848.014a143.29 143.29 0 0 0 8.467 1.113c15.632 1.265 31.377 1.212 47.139.75 15.762.462 31.507.515 47.139-.75 2.871-.287 5.747-.676 8.465-1.113l.85-.014c3.147-.596 5.688-1.144 7.682-1.678.051-.013.088-.026.139-.039 2.531-.643 4.364-1.285 5.025-1.834.54-.448.764-.841.697-1.359.08-.631-.293-1.317-1.074-2.098-.125-.125-.638-.235-1.5-.336-4.087-.757-17.544-.789-67.195-.789h-.453z""/><path d=""M262.968 431.921c-14.206-2.961-23.845-9.924-29.016-20.959-3.325-7.096-3.403-8.634-2.067-40.696.403-9.668.407-17.578.01-17.578s-4.092 2.426-8.21 5.391c-9.062 6.525-21.78 19.684-25.286 26.164-5.386 9.954-6.524 20.775-3.057 29.072 1.99 4.762.325 3.727-3.032-1.884-4.627-7.736-5.533-12.312-4.22-21.32.653-4.481 1.957-9.002 3.179-11.024 3.061-5.066 13.753-19.066 22.307-29.21 13.614-16.143 20.751-34.398 26.283-67.229.974-5.78 2.257-16.538 2.851-23.906 1.673-20.759 2.073-21.779 3.955-10.094 4.988 30.971 4.218 77.335-2.123 127.771-2.454 19.519-2.398 24.303.355 30.215 3.99 8.57 12.425 13.602 26.079 15.559 18.47 2.648 36.894-3.559 42.538-14.329 3.244-6.19 3.377-10.51.934-30.194-6.101-49.15-7.204-96.567-2.913-125.261.801-5.355 1.456-10.295 1.456-10.978s.332-1.241.739-1.241 1.262 6.577 1.902 14.617c2.442 30.683 9.141 60.459 17.275 76.79 3.326 6.678 7.671 12.946 14.297 20.625 8.217 9.522 18.349 23.153 20.788 27.966 3.156 6.229 4.089 19.043 1.786 24.534-1.954 4.657-6.206 11.988-6.679 11.514-.193-.193.266-2.403 1.02-4.91 5.024-16.695-4.353-34.639-27.028-51.72-9.276-6.988-9.468-7.079-10.313-4.908-.944 2.424-.124 27.441 1.147 35.024 1.672 9.969-2.034 22.811-8.716 30.207-3.795 4.201-11.654 8.684-19.344 11.034-7.174 2.193-28.345 2.744-36.9.961zm-44.268-6.468c-2.619-1.219-5.677-3.382-6.797-4.805-3.882-4.936-2.147-7.96 4.567-7.96 5.464 0 11.865 3.249 14.726 7.474 2.507 3.703 2.553 4.136.636 6.053-2.119 2.119-7.632 1.799-13.132-.762zm108.543 1.11c-4.473-4.473 5.73-13.875 15.056-13.875 7.125 0 8.551 4.408 3.002 9.28-5.826 5.116-15.163 7.491-18.059 4.595zM169.086 283.335c11.186-2.308 33.098-10.576 34.64-13.07.251-.407-3.432.2-8.185 1.348-15.507 3.746-30.091 5.432-47.08 5.444-17.806.013-27.25-1.318-41.8-5.89-14.12-4.437-17.419-9.233-10.707-15.564 7.853-7.407 28.886-16.06 44.468-18.295 10.163-1.458 25.532-.611 35.085 1.933 4.047 1.078 11.219 3.862 15.938 6.188 9.855 4.857 7.517 2.486-3.608-3.659-16.483-9.105-30.024-13.012-45.469-13.119-9.688-.067-11.593.21-18.791 2.741-4.406 1.549-10.408 4.408-13.34 6.353-8.808 5.846-21.63 20.881-24.618 28.866-1.121 2.997 16.437 11.397 32.16 15.385 17.388 4.411 34.287 4.852 51.308 1.34zm-148.44 20.566c1.281-3.674 9.666-11.023 21.35-18.713 14.097-9.278 13.374-8.582 8.36-8.037-5.115.557-13.084 3.91-19.055 8.018-2.335 1.606-4.245 2.521-4.245 2.032 0-1.535 10.439-10.507 15.643-13.444 2.74-1.546 4.982-2.981 4.982-3.188s-1.898-2.955-4.218-6.106c-5.176-7.032-10.513-17.837-11.294-22.869-.542-3.492-.266-3.208 4.006 4.118 2.523 4.327 6.778 10.306 9.454 13.287 10.698 11.914 16.503 9.823 41.185-14.831 23.048-23.023 34.151-27.124 61.697-22.791 49.549 7.794 69.291 18.265 73.107 38.773 3.088 16.597-26.213 32.051-67.021 35.347-25.43 2.054-50.465-2.121-70.608-11.774-5.651-2.708-7.754-3.222-13.177-3.222-8.536 0-16.384 2.117-25.201 6.797-6.929 3.678-23.242 16.705-23.242 18.561 0 .491-.611.893-1.357.893-1.105 0-1.173-.53-.364-2.849z"" stroke=""#000""/><path d=""M433.903 283.621c10.328-2.075 21.099-5.665 29.43-9.809 12.042-5.989 11.54-5.032 6.724-12.844-5.64-9.147-11.108-15.444-17.828-20.532-20.992-15.89-46.088-16.101-77.041-.649-6.785 3.387-13.038 6.933-13.894 7.88-.95 1.049 1.125.315 5.317-1.881 21.85-11.45 49.785-12.302 74.644-2.278 14.102 5.686 23.465 12.088 24.979 17.078 2.096 6.911-20.536 14.783-47.77 16.615-14.503.976-40.005-1.717-56.953-6.013-3.48-.882-6.328-1.345-6.328-1.028 0 .792 9.337 5.303 16.748 8.093 19.821 7.461 42.002 9.382 61.974 5.37zm97.203 16.927c-13.231-12.657-26.249-19.016-40.563-19.814l-8.543-.476-9.533 4.37c-17.566 8.052-33.963 11.233-57.286 11.113-31.173-.161-59.736-8.442-72.359-20.978-6.332-6.289-7.277-14.586-2.678-23.509 7.436-14.425 26.021-22.564 67.068-29.374 31.197-5.176 39.407-2.215 66.554 24.003 15.768 15.228 23.058 20.563 28.083 20.551 5.593-.014 14.318-8.963 21.041-21.579 2.192-4.113 4.155-7.478 4.364-7.478.69 0-.754 6.016-2.588 10.781-2.132 5.542-9.188 17.156-11.931 19.638-1.061.961-1.93 2.073-1.93 2.473s1.996 1.705 4.436 2.902 7.397 5.069 11.016 8.605c4.729 4.622 5.657 5.864 3.298 4.416-10.13-6.219-19.361-9.885-23.406-9.296-1.858.271-1.069 1.015 4.729 4.457 14.123 8.385 28.053 20.289 28.053 23.972 0 2.334-1.086 1.671-7.824-4.775zm-305.344-57.75c-8.546-11.626-28.975-32.929-40.587-42.324-33.839-27.378-70.469-39.879-106.329-36.289-8.681.869-22.385 3.775-33.7 7.146-2.033.606-2.021.501.767-6.748 9.364-24.351 24.251-41.984 43.486-51.508 10.237-5.069 18.499-6.911 31.089-6.933 17.252-.029 33.542 4.451 54.163 14.896 11.078 5.611 33.844 19.585 33.108 20.321-.165.165-4.851-1.146-10.415-2.911-12.616-4.004-30.319-8.409-41.383-10.299-9.148-1.562-28.461-2.966-27.585-2.005.297.326 4.187 1.228 8.644 2.005 30.954 5.398 64.048 23.93 89.548 50.145 6.139 6.311 6.8 7.344 6.317 9.862-.297 1.547-1.322 3.867-2.278 5.156-1.488 2.005-2.467 2.338-6.786 2.306-2.776-.021-6.193-.379-7.594-.797l-2.546-.759 3.755 8.766c7.043 16.441 17.008 47.783 15.629 49.161-.173.173-3.46-3.963-7.304-9.192zm107.379 0c8.546-11.626 28.975-32.929 40.587-42.324 33.839-27.378 70.469-39.879 106.329-36.289 8.681.869 22.385 3.775 33.7 7.146 2.033.606 2.021.501-.767-6.748-9.364-24.351-24.251-41.984-43.486-51.508-10.237-5.069-18.499-6.911-31.089-6.933-17.252-.029-33.542 4.451-54.163 14.896-11.078 5.611-33.844 19.585-33.108 20.321.165.165 4.851-1.146 10.415-2.911 12.616-4.004 30.319-8.409 41.383-10.299 9.148-1.562 28.461-2.966 27.585-2.005-.297.326-4.187 1.228-8.644 2.005-30.954 5.398-64.048 23.93-89.548 50.145-6.139 6.311-6.8 7.344-6.317 9.862.297 1.547 1.322 3.867 2.278 5.156 1.488 2.005 2.467 2.338 6.786 2.306 2.776-.021 6.193-.379 7.594-.797l2.546-.759-3.755 8.766c-7.043 16.441-17.008 47.783-15.629 49.161.173.173 3.46-3.963 7.304-9.192z""/></svg>";

        [ResponseCache(Duration = 60*10, Location = ResponseCacheLocation.Client, VaryByQueryKeys =new string[] {"*" })]
        public ActionResult Photo(string id, [FromQuery] Osoba.PhotoTypes phototype, [FromQuery] bool rnd, [FromQuery] string f)
        {
            bool specialTime = false;
            DateTime now = DateTime.Now;
            if (now.Day == 31 && now.Month == 12 && now.Hour > 17)
                specialTime = true;
            if (now.Day == 1 && now.Month == 4 )
                specialTime = true;
            if (false && User?.IsInRole("Admin") == true)
                specialTime = true;

            string noPhoto = specialTime ? NoPhotoAnonymousSvg : NoPhotoSvg;

            if (specialTime)
                phototype = Osoba.PhotoTypes.Cartoon;


            //string noPhotoPath = $"Content{Path.DirectorySeparatorChar}Img{Path.DirectorySeparatorChar}personNoPhoto.png";
            if (string.IsNullOrEmpty(id))
            {
                if (string.IsNullOrEmpty(f) || f?.Contains("..") == true)
                    return Content(noPhoto, "image/svg+xml");

                var nameId = Devmasters.RegexUtil.GetRegexGroupValue(f, @"\d{2} \\ (?<nameid>\w* - \w* (-\d{1,3})?) - small\.jpg", "nameid");
                if (string.IsNullOrEmpty(nameId))
                    return Content(noPhoto, "image/svg+xml");
                else
                    return Redirect("/photo/" + nameId);
            }
            var o = Osoby.GetByNameId.Get(id);
            if (o == null)
                return Content(noPhoto, "image/svg+xml");
            else
            {
                if (o.HasPhoto())
                    return File(System.IO.File.ReadAllBytes(o.GetPhotoPath(phototype)), "image/jpg");
                else
                    return Content(noPhoto, "image/svg+xml");
            }
        }

        
        public ActionResult Reporty()
        {
            return View();
        }

        public ActionResult ProvozniPodminky()
        {
            return RedirectPermanent("/texty/provoznipodminky");
        }

        public ActionResult Index()
        {
            return View();
        }

        [ActionName("K-Index")]
        public ActionResult Kindex()
        {
            return RedirectPermanent("/kindex");
        }


        public ActionResult PridatSe()
        {
            return RedirectPermanent("https://texty.hlidacstatu.cz/pridejte-se/");
        }


        public ActionResult VerejneZakazky(string q)
        {
            return Redirect("/VerejneZakazky");
        }

        public ActionResult Licence()
        {
            return RedirectPermanent("/texty/licence");
        }

        public ActionResult OServeru()
        {

            return RedirectPermanent("/texty/o-serveru");
        }

        public ActionResult Zatmivaci()
        {

            return View();
        }
        public ActionResult VisitImg(string id)
        {
            try
            {
                var path = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(id));
                Visit.AddVisit(path,
                    Visit.IsCrawler(Request.Headers[HeaderNames.UserAgent]) ?
                        Visit.VisitChannel.Crawler : Visit.VisitChannel.Web);
            }
            catch (Exception e)
            {
                _logger.Information(e, "VisitImg base64 encoding error");
            }

            return File(@"Content\Img\1x1.png", "image/png");
        }

        public ActionResult Kontakt()
        {
            return RedirectPermanent("/texty/kontakt");
        }


        public async Task<ActionResult> sendFeedbackMail(string typ, string email, string txt, string url, bool? auth, string data)
        {
            string to = "podpora@hlidacstatu.cz";
            string subject = "Zprava z HlidacStatu.cz: " + typ;
            if (!string.IsNullOrEmpty(data))
            {
                if (data.StartsWith("dataset|"))
                {
                    data = data.Replace("dataset|", "");
                    try
                    {
                        var ds = DataSet.CachedDatasets.Get(data);
                        to = (await ds.RegistrationAsync()).createdBy;
                        subject = subject + $" ohledně databáze {ds.DatasetId}";
                    }
                    catch (Exception)
                    {
                        return Content("");
                    }
                }
            }

            if (auth == null || auth == false || (auth == true && User?.Identity?.IsAuthenticated == true))
            {
                if (!string.IsNullOrEmpty(email) && Devmasters.TextUtil.IsValidEmail(email)
                    && !string.IsNullOrEmpty(to) && Devmasters.TextUtil.IsValidEmail(to)
                    )
                {
                    try
                    {
                        string body = $@"
Zpráva z hlidacstatu.cz.

Typ zpravy:{typ}
Od uzivatele:{email} 
ke stránce:{url}

text zpravy: {txt}";
                        Util.SMTPTools.SendSimpleMailToPodpora(subject, body, email);

                    }
                    catch (Exception ex)
                    {

                        _logger.Fatal(string.Format("{0}|{1}|{2}", email, url, txt, ex));
                        return Content("");
                    }
                }
            }
            return Content("");
        }

        public ActionResult ClassificationFeedback(string typ, string email, string txt, string url, string data)
        {
            // create a task, so user doesn't have to wait for anything
            _ = Task.Run(async () =>
            {
                try
                {
                    string subject = "Zprava z HlidacStatu.cz: " + typ;
                    string body = $@"
Návrh na opravu klasifikace.

Od uzivatele:{email} 
ke stránce:{url}

text zpravy: {txt}

";
                    Util.SMTPTools.SendSimpleMailToPodpora(subject, body, email);

                    string classificationExplanation = await SmlouvaRepo.GetClassificationExplanationAsync(data);

                    string explain = $"explain result: {classificationExplanation} ";

                    Util.SMTPTools.SendEmail(subject, "", body + explain, "michal@michalblaha.cz");
                    Util.SMTPTools.SendEmail(subject, "", body + explain, "petr@hlidacstatu.cz");
                    Util.SMTPTools.SendEmail(subject, "", body + explain, "lenka@hlidacstatu.cz");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"error sending classification feedback {email}|{url}|{txt}");
                }

                try
                {
                    string connectionString = Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString");
                    if (string.IsNullOrWhiteSpace(connectionString))
                        throw new Exception("Missing RabbitMqConnectionString");

                    var message = new Q.Messages.ClassificationFeedback()
                    {
                        FeedbackEmail = email,
                        IdSmlouvy = data,
                        ProposedCategories = txt
                    };

                    Q.Publisher.QuickPublisher.Publish(message, connectionString);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Problem sending data to ClassificationFeedback queue. Message={ex}");
                }


            });

            return Content("");
        }

        public async Task<ActionResult> TextSmlouvy(string Id, string hash, string secret)
        {
            if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(hash))
                return NotFound();

            var model = await SmlouvaRepo.LoadAsync(Id);
            if (model == null)
            {
                return NotFound();
            }
            var priloha = model.Prilohy?.FirstOrDefault(m => m.hash.Value == hash);
            if (priloha == null)
            {
                return NotFound();
            }

            if (model.znepristupnenaSmlouva())
            {
                if (string.IsNullOrEmpty(secret)) //pokus jak se dostat k znepristupnene priloze
                    return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                else if (User?.Identity?.IsAuthenticated == false) //neni zalogovany
                    return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                else
                {
                    if (priloha.LimitedAccessSecret(User.Identity.Name) != secret)
                        return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                }
            }

            ViewBag.hashValue = hash;
            return View(model);
        }
        public async Task<ActionResult> KopiePrilohy(string Id, string hash, string secret, bool forcePDF = false)
        {

            if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(hash))
                return NotFound();

            var exists = await SmlouvaRepo.ExistsZaznamAsync(Id);
            if (exists == false)
            {
                return NotFound();
            }
            var hashlist = await SmlouvaRepo.GetPartValuesAsync<string>(Id, "prilohy.hash.value", "$.Prilohy..hash.Value");
            var prilohaExists = hashlist.Any(h => h == hash);
            if (prilohaExists == false)
            {
                return NotFound();
            }

            var platnyZaznam = await SmlouvaRepo.GetPartValueAsync<bool>(Id, "platnyZaznam");
            if (platnyZaznam==false)
            {
                if (User.IsInRole("Admin") == false && secret != Devmasters.Config.GetWebConfigValue("LocalPrilohaUniversalSecret"))
                {
                    if (string.IsNullOrEmpty(secret)) //pokus jak se dostat k znepristupnene priloze
                        return Redirect(Smlouva.GetUrl(Id,true)); //jdi na detail smlouvy
                    else if (User?.Identity?.IsAuthenticated == false) //neni zalogovany
                        return Redirect(Smlouva.GetUrl(Id,true)); //jdi na detail smlouvy
                    else if (User?.HasEmailConfirmed() == false)
                    {
                        return Redirect(Smlouva.GetUrl(Id,true)); //jdi na detail smlouvy
                    }
                    else
                    {
                        if (Smlouva.Priloha.LimitedAccessSecret(User?.Identity?.Name, hash) != secret)
                            return Redirect(Smlouva.GetUrl(Id,true)); //jdi na detail smlouvy
                    }
                }
            }
            var model = await SmlouvaRepo.LoadAsync(Id, includePrilohy:false) ;
            var priloha = model.Prilohy?.FirstOrDefault(m => m.UniqueHash() == hash);

            var fn = SmlouvaRepo.GetDownloadedPrilohaPath(priloha,model, 
                forcePDF ? Connectors.IO.PrilohaFile.RequestedFileType.PDF : Connectors.IO.PrilohaFile.RequestedFileType.Original );

            if (string.IsNullOrEmpty(fn)
                || System.IO.File.Exists(fn) == false)
                return NotFound();

            if (Lib.OCR.DocTools.HasPDFHeader(fn))
            {
                return File(await System.IO.File.ReadAllBytesAsync(fn), "application/pdf", string.IsNullOrWhiteSpace(priloha.nazevSouboru) ? $"{model.Id}_smlouva.pdf" : priloha.nazevSouboru + ".pdf");
            }
            else
                return File(await System.IO.File.ReadAllBytesAsync(fn),
                    string.IsNullOrWhiteSpace(priloha.ContentType) ? "application/octet-stream" : priloha.ContentType,
                    (string.IsNullOrWhiteSpace(priloha.nazevSouboru) ? "priloha" : priloha.nazevSouboru) 
                        + (forcePDF ? ".pdf" : "")
                    );

        }

        public ActionResult PoliticiChybejici()
        {
            return View();
        }

        public ActionResult Api(string id)
        {
            return RedirectToActionPermanent("Index", "ApiV1");
        }

        public ActionResult JsonDoc()
        {
            return View();
        }
        public ActionResult Smlouvy()
        {
            return View();
        }
        public ActionResult ORegistru()
        {
            return RedirectPermanent("https://texty.hlidacstatu.cz/o-registru");
        }

        public ActionResult Afery()
        {
            return View();
        }

        public ActionResult HledatVice()
        {
            return RedirectToActionPermanent("SnadneHledani");
        }
        public ActionResult SnadneHledani()
        {
            string[] splitChars = new string[] { " " };
            var qs = HttpContext.Request.Query;

            string query = "";


            if (!string.IsNullOrWhiteSpace(qs["alltxt"]))
            {
                query += " " + qs["alltxt"];
            }
            if (!string.IsNullOrWhiteSpace(qs["exacttxt"]))
            {
                query += " \"" + qs["exacttxt"] + "\"";
            }
            if (!string.IsNullOrWhiteSpace(qs["anytxt"]))
            {
                query += " ("
                    + qs["anytxt"].ToString().Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Aggregate((f, s) => f + " OR " + s)
                    + ")";
            }
            if (!string.IsNullOrWhiteSpace(qs["nonetxt"]))
            {
                query += " " + qs["nonetxt"].ToString().Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Select(s => s.StartsWith("-") ? s : "-" + s).Aggregate((f, s) => f + " " + s);
            }
            if (!string.IsNullOrWhiteSpace(qs["textsmlouvy"]))
            {
                query += " textSmlouvy:\"" + qs["textsmlouvy"].ToString().Trim() + "\"";
            }


            List<KeyValuePair<string, string>> platce = new();
            if (qs["icoPlatce"].ToString() != null)
                foreach (var val in qs["icoPlatce"]
                    .ToString().Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    )
                { platce.Add(new KeyValuePair<string, string>("icoPlatce", val)); }


            if (qs["dsPlatce"].ToString() != null)
                foreach (var val in qs["dsPlatce"]
                        .ToString().Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    )
                { platce.Add(new KeyValuePair<string, string>("dsPlatce", val)); }


            platce.Add(new KeyValuePair<string, string>("jmenoPlatce", qs["jmenoPlatce"]));
            if (platce.Count(m => !string.IsNullOrWhiteSpace(m.Value)) > 1)
            { // into ()
                query += " ("
                        + platce.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).Aggregate((f, s) => f + " OR " + s)
                        + ")";
            }
            else if (platce.Count(m => !string.IsNullOrWhiteSpace(m.Value)) == 1)
            {
                query += " " + platce.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).First();
            }


            List<KeyValuePair<string, string>> prijemce = new();
            if (qs["icoPrijemce"].ToString() != null)
                foreach (var val in qs["icoPrijemce"]
                    .ToString().Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    )
                { prijemce.Add(new KeyValuePair<string, string>("icoPrijemce", val)); }


            if (qs["dsPrijemce"].ToString() != null)
                foreach (var val in qs["dsPrijemce"]
                        .ToString().Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    )
                { prijemce.Add(new KeyValuePair<string, string>("dsPrijemce", val)); }



            prijemce.Add(new KeyValuePair<string, string>("jmenoprijemce", qs["jmenoprijemce"]));
            if (prijemce.Count(m => !string.IsNullOrWhiteSpace(m.Value)) > 1)
            { // into ()
                query += " ("
                        + prijemce.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).Aggregate((f, s) => f + " OR " + s)
                        + ")";
            }
            else if (prijemce.Count(m => !string.IsNullOrWhiteSpace(m.Value)) == 1)
            {
                query += " " + prijemce.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).First();
            }


            if (!string.IsNullOrWhiteSpace(qs["cenaOd"]) && Devmasters.TextUtil.IsNumeric(qs["cenaOd"]))
                query += " cena:>" + qs["cenaOd"];

            if (!string.IsNullOrWhiteSpace(qs["cenaDo"]) && Devmasters.TextUtil.IsNumeric(qs["cenaDo"]))
                query += " cena:<" + qs["cenaDo"];

            if (!string.IsNullOrWhiteSpace(qs["zverejnenoOd"]) && !string.IsNullOrWhiteSpace(qs["zverejnenoDo"]))
            {
                query += $" zverejneno:[{qs["zverejnenoOd"]} TO {qs["zverejnenoDo"]}]";
            }
            else if (!string.IsNullOrWhiteSpace(qs["zverejnenoOd"]))
            {
                query += $" zverejneno:[{qs["zverejnenoOd"]} TO *]";
            }
            else if (!string.IsNullOrWhiteSpace(qs["zverejnenoDo"]))
            {
                query += $" zverejneno:[* TO {qs["zverejnenoDo"]}]";
            }

            if (!string.IsNullOrWhiteSpace(qs["podepsanoOd"]) && !string.IsNullOrWhiteSpace(qs["podepsanoDo"]))
            {
                query += $" podepsano:[{qs["podepsanoOd"]} TO {qs["podepsanoDo"]}]";
            }
            else if (!string.IsNullOrWhiteSpace(qs["podepsanoOd"]))
            {
                query += $" podepsano:[{qs["podepsanoOd"]} TO *]";
            }
            else if (!string.IsNullOrWhiteSpace(qs["podepsanoDo"]))
            {
                query += $" podepsano:[* TO {qs["podepsanoDo"]}]";
            }


            if (!string.IsNullOrWhiteSpace(qs["osobaNamedId"]))
            {
                query += $" {qs["osobaNamedId"]}";
            }
            if (!string.IsNullOrWhiteSpace(qs["holding"]))
            {
                query += " holding:" + qs["holding"];
            }

            if (!string.IsNullOrWhiteSpace(qs["obory"]))
            {
                foreach (var obor in qs["obory"])
                {
                    query += " oblast:" + obor;
                }
            }

            query = query.Trim();

            if (!string.IsNullOrWhiteSpace(query))
            {
                if (!string.IsNullOrEmpty(qs["hledatvse"]))
                    return Redirect("/Hledat?q=" + System.Net.WebUtility.UrlEncode(query));

                if (!string.IsNullOrEmpty(qs["hledatsmlouvy"]))
                    return Redirect("/HledatSmlouvy?q=" + System.Net.WebUtility.UrlEncode(query));

                if (!string.IsNullOrEmpty(qs["hledatvz"]))
                    return Redirect("/VerejneZakazky/Hledat?q=" + System.Net.WebUtility.UrlEncode(query));
            }

            return View();
        }

        public ActionResult Adresar(string id, string kraj = null, string vz= "")
        {
            (string oborName, string kraj, bool zakazky) model = (id, kraj, vz=="1");
            return View(model);
        }

        public ActionResult Politici(string prefix)
        {
            return RedirectPermanent(Url.Action("Index", "Osoby", new { prefix = prefix }));
        }

        public ActionResult Politik(string Id, Relation.AktualnostType? aktualnost)
        {
            return RedirectPermanent(Url.Action("Index", "Osoba", new { Id = Id, aktualnost = aktualnost }));
        }

        public ActionResult PolitikVazby(string Id, Relation.AktualnostType? aktualnost)
        {
            return RedirectPermanent(Url.Action("Vazby", "Osoba", new { Id = Id, aktualnost = aktualnost }));
        }

        public async Task<ActionResult> Detail(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                return NotFound();

            var model = await SmlouvaRepo.LoadAsync(Id);
            if (model == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["qs"]))
            {
                var findSm = await SmlouvaRepo.Searching
                    .SimpleSearchAsync($"_id:\"{model.Id}\" AND ({HttpContext.Request.Query["qs"]})", 1, 1,
                    SmlouvaRepo.Searching.OrderResult.FastestForScroll, withHighlighting: true);
                if (findSm.Total > 0)
                    ViewBag.Highlighting = findSm.ElasticResults.Hits.First().Highlight;

            }
            return View(model);
        }

        public async Task<ActionResult> HledatFirmy(string q, int? page = 1)
        {
            var model = await FirmaRepo.Searching.SimpleSearchAsync(q, page.Value, 50);
            return View(model);
        }

        public async Task<ActionResult> HledatSmlouvy(Repositories.Searching.SmlouvaSearchResult model)
        {
            if (model == null || ModelState.IsValid == false)
                return View(new Repositories.Searching.SmlouvaSearchResult());



            var sres = await SmlouvaRepo.Searching.SimpleSearchAsync(model.Q, model.Page,
                SmlouvaRepo.Searching.DefaultPageSize,
                (SmlouvaRepo.Searching.OrderResult)(Convert.ToInt32(model.Order)),
                includeNeplatne: model.IncludeNeplatne,
                anyAggregation: new Nest.AggregationContainerDescriptor<Smlouva>().Sum("sumKc", m => m.Field(f => f.CalculatedPriceWithVATinCZK)),
                logError: false);

            AuditRepo.Add(
                    Audit.Operations.UserSearch
                    , User?.Identity?.Name
                    , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                    , "Smlouva"
                    , sres.IsValid ? "valid" : "invalid"
                    , sres.Q, sres.OrigQuery);

            if (sres.IsValid == false && !string.IsNullOrEmpty(sres.Q))
            {
                Manager.LogQueryError<Smlouva>(sres.ElasticResults, "/hledat", HttpContext);
            }

            return View(sres);
        }

        public async Task<ActionResult> Hledat(string q, string order)
        {
            bool showBeta = User.Identity?.IsAuthenticated == true && User.IsInRole("BetaTester");

            var res = await XLib.Search
                .GeneralSearchAsync(q, 1, showBeta, order, this.User, smlouvySize: Repositories.Searching.SearchDataResult<object>.DefaultPageSizeGlobal);
            AuditRepo.Add(
                Audit.Operations.UserSearch
                , User?.Identity?.Name
                , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                , "General"
                , res.IsValid ? "valid" : "invalid"
                , q, null);

            if (System.Diagnostics.Debugger.IsAttached ||
                Devmasters.Config.GetWebConfigValue("LogSearchTimes") == "true")
            {
                _logger.Information($"Search times: {q}\n" + res.SearchTimesReport());

                var data = res.SearchTimes();


                // Set up some properties:

                //foreach (var kv in data)
                //{
                //    var metrics = new Dictionary<string, double> { { "web-search-" + kv.Key, kv.Value.TotalMilliseconds } };
                //    var props = new Dictionary<string, string> { { "query", q }, { "database", kv.Key } };

                //    Metric elaps = _telemetryClient.GetMetric("web-GlobalSearch_Elapsed", "Database");
                //    _telemetryClient.TrackEvent("web-GlobalSearch_Elapsed", props, metrics);
                //    var ok = elaps.TrackValue(kv.Value.TotalMilliseconds, kv.Key);
                //}
            }
            string viewName = "Hledat";
            return View(viewName, res);
        }

        public ActionResult Novinky()
        {
            return RedirectPermanent("https://texty.hlidacstatu.cz");
        }
        public ActionResult Napoveda()
        {

            return View();
        }
        public ActionResult Cenik()
        {

            return View();
        }

        public ActionResult PravniPomoc()
        {

            return View();
        }



        public ActionResult Error404(string nextUrl = null, string nextUrlText = null)
        {
            return NotFound();
        }


        public enum ErrorPages
        {
            Ok = 0,
            NotFound = 404,
            Error = 500,
            ErrorHack = 555
        }

        public ActionResult Error(string id, string nextUrl = null, string nextUrlText = null)
        {
            ViewBag.NextText = nextUrl;
            ViewBag.NextUrlText = nextUrlText;
            ViewBag.InvokeErrorAction = true;

            ErrorPages errp = (ErrorPages)EnumTools.GetValueOrDefaultValue(id, typeof(ErrorPages));

            switch (errp)
            {
                case ErrorPages.Ok:
                    return Redirect("/");
                case ErrorPages.NotFound:
                    return NotFound();
                case ErrorPages.Error:
                    return StatusCode((int)ErrorPages.Error);
                case ErrorPages.ErrorHack:
                    return StatusCode((int)ErrorPages.ErrorHack);
                default:
                    return Redirect("/");
            }
        }

        public ActionResult Widget(string id, string width)
        {
            string path = Path.Combine(_hostingEnvironment.WebRootPath, "Scripts\\widget.js");

            string widgetjs = System.IO.File.ReadAllText(path)
                .Replace("#RND#", id ?? Devmasters.TextUtil.GenRandomString(4))
                .Replace("#MAXWIDTH#", width != null ? width.ToString() : "")
                .Replace("#MAXWIDTHSCRIPT#", width != null ? ",maxWidth:" + width : "")
                .Replace("#WEBROOT#", HttpContext.Request.Scheme + "://" + HttpContext.Request.Host)
                ;
            return Content(widgetjs, "text/javascript");
        }

        [HlidacCache(60 * 60, "id", false)]
        public async Task<ActionResult> Export(string id)
        {
            System.Text.StringBuilder sb = new(2048);

            if (id == "uohs-ed")
            {
                var ds = DataSet.CachedDatasets.Get("rozhodnuti-uohs");
                var res = await ds.SearchDataAsync("*", 0, 30, "PravniMoc desc");
                if (res.Total > 0)
                {
                    sb.Append(
                        Newtonsoft.Json.JsonConvert.SerializeObject(
                            res.Result
                            .Select(m =>
                            {
                                m.DetailUrl = "https://www.hlidacstatu.cz/data/Detail/rozhodnuti-uohs/" + m.Id;
                                m.DbCreatedBy = null; m.Rozhodnuti = null;
                                return m;
                            })
                            )
                        );
                }
                else
                {
                    sb.Append("[]");
                }
            }
            else if (id == "vz-ed")
            {
                string[] icos = FirmaRepo.MinisterstvaCache.Get().Select(s => s.ICO).ToArray();

                var vz = VerejnaZakazkaRepo.Searching.CachedSimpleSearch(TimeSpan.FromHours(6),
                    new Repositories.Searching.VerejnaZakazkaSearchData()
                    {
                        Q = icos.Select(i => "ico:" + i).Aggregate((f, s) => f + " OR " + s),
                        Page = 0,
                        PageSize = 30,
                        Order = "1"
                    }
                    );
                if (vz.Total > 0)
                {
                    sb.Append(
                        Newtonsoft.Json.JsonConvert.SerializeObject(
                            vz.ElasticResults.Hits.Select(h => new
                            {
                                Id = h.Id,
                                DetailUrl = h.Source.GetUrl(false),
                                Zadavatel = h.Source.Zadavatel,
                                Dodavatele = h.Source.Dodavatele,
                                NazevZakazky = h.Source.NazevZakazky,
                                Cena = h.Source.FormattedCena(false),
                                CPVkody = h.Source.CPV.Count() == 0
                                        ? "" :
                                        h.Source.CPV.Select(c => VerejnaZakazka.CPVToText(c)).Aggregate((f, s) => f + ", " + s),
                                Stav = h.Source.StavZakazky.ToNiceDisplayName(),
                                DatumUverejneni = h.Source.DatumUverejneni
                            })
                            )
                        );
                }
                else
                {
                    sb.Append("[]");
                }
            }
            return Content(sb.ToString(), "application/json");
        }


        public class ImageBannerCoreData
        {
            public string title { get; set; }
            public string subtitle { get; set; }
            public string body { get; set; }
            public string footer { get; set; }
            public string img { get; set; }
            public string color { get; set; }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult ImageBannerCore(string id, string title, string subtitle, string body, string footer, string img, string ratio = "16x9", string color = "blue-dark")
        {
            id = id ?? "social";

            string viewName = "ImageBannerCore16x9_social";
            if (id.ToLower() == "quote")
            {
                if (ratio == "1x1")
                    viewName = "ImageBannerCore1x1_quote";
                else
                    viewName = "ImageBannerCore16x9_quote";
            }
            else
            {
                if (ratio == "1x1")
                    viewName = "ImageBannerCore1x1_social";
                else
                    viewName = "ImageBannerCore16x9_social";
            }

            return View(viewName, new ImageBannerCoreData() { title = title, subtitle = subtitle, body = body, footer = footer, img = img, color = color });
        }

        public async Task<ActionResult> SocialBanner(string id, string v, string t, string st, string b, string f, string img, string rat = "16x9", string res = "1200x628", string col = "")
        {
            string mainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;


            // #if (DEBUG)
            //             if (System.Diagnostics.Debugger.IsAttached)
            //                 mainUrl = "http://local.hlidacstatu.cz";
            //             //mainUrl = "https://www.hlidacstatu.cz";
            // #endif
            //twitter Recommended size: 1024 x 512 pixels
            //fb Recommended size: 1200 pixels by 630 pixels

            string url = null;

            byte[] data = null;
            if (id?.ToLower() == "subjekt")
            {
                Firma fi = Firmy.Get(v);
                if (fi.Valid)
                {
                    if (!(await fi.NotInterestingToShowAsync()))
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = fi.SocialInfoTitle(),
                            body = fi.SocialInfoBody(),
                            footer = fi.SocialInfoFooter(),
                            subtitle = fi.SocialInfoSubTitle(),
                            img = fi.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }
            }
            else if (id?.ToLower() == "zakazka")
            {
                VerejnaZakazka vz = await VerejnaZakazkaRepo.LoadFromESAsync(v);
                if (vz != null)
                    try
                    {
                        var path = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(id));
                        Visit.AddVisit(path,
                            Visit.IsCrawler(Request.Headers[HeaderNames.UserAgent]) ?
                                Visit.VisitChannel.Crawler : Visit.VisitChannel.Web);
                    }
                    catch (Exception e)
                    {
                        _logger.Information(e, "VisitImg base64 encoding error");
                    }

                if (!vz.NotInterestingToShow())
                {
                    var social = new ImageBannerCoreData()
                    {
                        title = vz.SocialInfoTitle(),
                        body = vz.SocialInfoBody(),
                        footer = vz.SocialInfoFooter(),
                        subtitle = vz.SocialInfoSubTitle(),
                        img = vz.SocialInfoImageUrl(),
                        color = "blue-dark"
                    };
                    url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                }

            }
            else if (id?.ToLower() == "osoba")
            {
                Osoba o = Osoby.GetByNameId.Get(v);
                if (o != null)
                {
                    if (!(await o.NotInterestingToShowAsync()))
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = o.SocialInfoTitle(),
                            body = o.SocialInfoBody(),
                            footer = o.SocialInfoFooter(),
                            subtitle = o.SocialInfoSubTitle(),
                            img = o.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }

            }
            else if (id?.ToLower() == "smlouva")
            {
                Smlouva s = await SmlouvaRepo.LoadAsync(v);
                if (s != null)
                {
                    if (!s.NotInterestingToShow())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = s.SocialInfoTitle(),
                            body = s.SocialInfoBody(),
                            footer = s.SocialInfoFooter(),
                            subtitle = s.SocialInfoSubTitle(),
                            img = s.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }
            }
            else if (id?.ToLower() == "insolvence")
            {
                var s = (await InsolvenceRepo.LoadFromEsAsync(v, false, true))?.Rizeni;
                if (s != null)
                {
                    if (!s.NotInterestingToShow())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = s.SocialInfoTitle(),
                            body = s.SocialInfoBody(),
                            footer = s.SocialInfoFooter(),
                            subtitle = s.SocialInfoSubTitle(),
                            img = s.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }

            }
            else if (id?.ToLower() == "dataset")
            {
                var s = DataSet.CachedDatasets.Get(v);
                if (s != null)
                {
                    if (!s.NotInterestingToShow())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = await s.SocialInfoTitleAsync(),
                            body = s.SocialInfoBody(),
                            footer = s.SocialInfoFooter(),
                            subtitle = s.SocialInfoSubTitle(),
                            img = s.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }

            }
            else if (id?.ToLower() == "quote")
            {
                url = mainUrl + "/imagebannercore/quote"
                    + "?title=" + System.Net.WebUtility.UrlEncode(t)
                    + "&subtitle=" + System.Net.WebUtility.UrlEncode(st)
                    + "&body=" + System.Net.WebUtility.UrlEncode(b)
                    + "&footer=" + System.Net.WebUtility.UrlEncode(f)
                    + "&img=" + System.Net.WebUtility.UrlEncode(img)
                    + "&color=" + col
                    + "&ratio=" + rat;
                v = Devmasters.Crypto.Hash.ComputeHashToHex(url);
            }
            else if (id?.ToLower() == "kindex")
            {
                data = RemoteUrlFromWebCache.GetBinary(mainUrl + "/kindex/banner/" + v, "kindex-banner-" + v, HttpContext.Request.Query["refresh"] == "1");
            }
            else if (id?.ToLower() == "page" && string.IsNullOrEmpty(v) == false)
            {
                var pageUrl = v;
                string socialTitle = "";
                string socialHtml = "";
                string socialFooter = "";
                string socialSubFooter = "";
                string socialFooterImg = "";
                using (Devmasters.Net.HttpClient.URLContent net = new(pageUrl))
                {
                    net.Timeout = 40000;
                    var cont = net.GetContent().Text;
                    socialTitle = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil
                        .GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_title\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                    socialHtml = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil
                        .GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_html\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                    socialFooter = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil.GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_footer\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                    socialSubFooter = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil.GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_subfooter\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                    socialFooterImg = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil.GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_footerimg\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                }
                if (string.IsNullOrEmpty(socialHtml))
                    return File(@"content\icons\largetile.png", "image/png");
                else
                    url = mainUrl + "/imagebannercore/quote"
                        + "?title=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialTitle))
                        + "&subtitle=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialSubFooter))
                        + "&body=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialHtml))
                        + "&footer=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialFooter))
                        + "&img=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialFooterImg))
                        + "&color=" + col
                        + "&ratio=" + rat;
            }

            try
            {
                if (data == null && !string.IsNullOrEmpty(url))
                {

                    data = RemoteUrlFromWebCache.GetScreenshot(url, (id?.ToLower() ?? "null") + "-" + rat + "-" + v, HttpContext.Request.Query["refresh"] == "1");
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Manager Save");
            }
            if (data == null || data.Length == 0)
                return File(@"content\icons\largetile.png", "image/png");
            else
                return File(data, "image/png");

        }

        private string GetSocialBannerUrl(ImageBannerCoreData si, bool ratio1x1 = false, bool localUrl = true)
        {
            string url = "";
            if (localUrl == false)
                url = "https://www.hlidacstatu.cz";

            url = url + "/imagebannercore/social"
                      + "?title=" + System.Net.WebUtility.UrlEncode(si.title)
                      + "&subtitle=" + System.Net.WebUtility.UrlEncode(si.subtitle)
                      + "&body=" + System.Net.WebUtility.UrlEncode(si.body)
                      + "&footer=" + System.Net.WebUtility.UrlEncode(si.footer)
                      + "&img=" + System.Net.WebUtility.UrlEncode(si.img)
                      + "&ratio=" + (ratio1x1 ? "1x1" : "16x9")
                      + "&color=" + si.color;


            return url;
        }

        public ActionResult Tip(string id)
        {
            string? url;
            using (DbEntities db = new())
            {
                url = db.TipUrl.AsQueryable().Where(m => m.Name == id).Select(m => m.Url).FirstOrDefault();
                url ??= "/";
            }

            try
            {
                var path = "/tip/" + id;
                Visit.AddVisit(path,
                    Visit.IsCrawler(Request.Headers[HeaderNames.UserAgent]) ?
                        Visit.VisitChannel.Crawler : Visit.VisitChannel.Web);
            }
            catch (Exception e)
            {
                _logger.Information(e, "VisitImg base64 encoding error");
            }

            return Redirect(url);
        }
        public ActionResult Status()
        {
            return View(Models.HealthCheckStatusModel.CurrentData.Get());
        }
        public ActionResult Tmp()
        {
            return View();
        }
    }
}