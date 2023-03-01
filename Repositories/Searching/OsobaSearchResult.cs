using HlidacStatu.Entities;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories.Searching
{
    public class OsobaSearchResult : SearchDataResult<Osoba>
    {
        public OsobaSearchResult()
            : base(getVZOrderList)
        {
        }

        public IEnumerable<Osoba> Results { get; set; }

        public override int MaxResultWindow()
        {
            return 100;
        }

        public new object ToRouteValues(int page)
        {
            return new
            {
                Q = Query,
                Page = page,
            };
        }


        protected static Func<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> getVZOrderList = () =>
        {
            return
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem[] { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "---" } }
                .Union(
                    Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(OsobaRepo.Searching.OrderResult))
                    .Select(
                        m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = m.Id.ToString() , Text = "Řadit " + m.Id }
                    )
                )
                .ToList();
        };



    }
}
