using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{


    public partial class FeedbackModalTagHelper
    {

        public static string AcceptDataUrl = "/__FeedbackModal/Submit";
        public async static Task<IResult> AcceptDataDelegateAsync (HttpContext context)
        {
            string typ = context.Request.Form["typ"];
            string email = context.Request.Form["email"];
            string txt = context.Request.Form["txt"];
            string url = context.Request.Form["url"];
            if (bool.TryParse(context.Request.Form["url"], out bool auth) == false)
                auth = false;

            string data= context.Request.Form["url"];

            string to = "podpora@hlidacstatu.cz";

            string subject = "Zprava z HlidacStatu.cz: " + typ;

            if (auth == null || auth == false || (auth == true && context.User?.Identity?.IsAuthenticated == true))
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
                        //_logger.Fatal(string.Format("{0}|{1}|{2}", email, url, txt, ex));
                        return Results.BadRequest(ex.Message);
                    }
                }
            }

            return Results.Ok("");
        }
        
    }
}