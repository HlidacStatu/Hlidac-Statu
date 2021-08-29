using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using System;

namespace HlidacStatu.Extensions
{
    public static class ReviewExtension
    {
        public static void Accepted(this Review review, string user)
        {
            review.ReviewedBy = user;
            review.Reviewed = DateTime.Now;
            review.ReviewResult = (int)Review.ReviewAction.Accepted;

            switch (review.ItemType)
            {
                case Review.ItemTypes.osobaPhoto:
                    var data = Newtonsoft.Json.Linq.JObject.Parse(review.NewValue);
                    Osoba o = Osoby.GetByNameId.Get(data.Value<string>("nameId"));
                    if (o != null)
                    {
                        var path = Init.OsobaFotky.GetFullPath(o, "original.uploaded.jpg");
                        var pathSmall = Init.OsobaFotky.GetFullPath(o, "small.uploaded.jpg");
                        Devmasters.IO.IOTools.MoveFile(pathSmall, Init.OsobaFotky.GetFullPath(o, "small.jpg"));

                        if (Devmasters.TextUtil.IsValidEmail(review.CreatedBy))
                        {
                            using (System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient())
                            {
                                smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                                var m = new System.Net.Mail.MailMessage()
                                {
                                    From = new System.Net.Mail.MailAddress("podpora@hlidacstatu.cz"),
                                    Subject = o.FullName() + " - nová fotka schválena.",
                                    IsBodyHtml = false,
                                    Body = "Vámi nahraná fotka byla právě schválena. Děkujeme za pomoc.\n\nHlídač Státu"
                                };
                                m.BodyEncoding = System.Text.Encoding.UTF8;
                                m.SubjectEncoding = System.Text.Encoding.UTF8;

                                m.To.Add(review.CreatedBy);
                                m.Bcc.Add("michal@michalblaha.cz");
                                smtp.Send(m);
                            }
                        }
                    }

                    break;
                default:
                    break;
            }

            ReviewRepo.Save(review);
        }

        public static void Denied(this Review review, string user, string reason)
        {
            review.ReviewedBy = user;
            review.Reviewed = DateTime.Now;
            review.ReviewResult = (int)Review.ReviewAction.Denied;
            review.Comment = reason;
            if (Devmasters.TextUtil.IsValidEmail(review.CreatedBy))
            {
                switch (review.ItemType)
                {
                    case Review.ItemTypes.osobaPhoto:
                        using (System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient())
                        {
                            smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                            var data = Newtonsoft.Json.Linq.JObject.Parse(review.NewValue);
                            Osoba o = Osoby.GetByNameId.Get(data.Value<string>("nameId"));
                            if (o != null)
                            {
                                var m = new System.Net.Mail.MailMessage()
                                {
                                    From = new System.Net.Mail.MailAddress("podpora@hlidacstatu.cz"),
                                    Subject = o.FullName() + " - nová fotka neschválena.",
                                    IsBodyHtml = false,
                                    Body = "Vámi nahraná fotka nebyla schválena k uveřejnění.\nDůvod:" + reason +
                                           "\n\nDěkujeme za pomoc.\n\nHlídač Státu"
                                };
                                m.BodyEncoding = System.Text.Encoding.UTF8;
                                m.SubjectEncoding = System.Text.Encoding.UTF8;
                                m.To.Add(review.CreatedBy);
                                m.Bcc.Add("michal@michalblaha.cz");
                                smtp.Send(m);
                            }
                        }

                        break;
                    default:
                        break;
                }
            }

            ReviewRepo.Save(review);
        }

        public static string RenderNewValueToHtml(this Review review)
        {
            return review.RenderValueToHtml(false);
        }
        public static string RenderOldValueToHtml(this Review review)
        {
            return review.RenderValueToHtml(true);
        }
        private static string RenderValueToHtml(this Review review, bool oldValue)
        {
            switch (review.ItemType)
            {
                case Review.ItemTypes.osobaPhoto:
                    var data = Newtonsoft.Json.Linq.JObject.Parse(review.NewValue);
                    Osoba o = Osoby.GetByNameId.Get(data.Value<string>("nameId"));
                    if (o != null)
                    {
                        if (oldValue)
                            return "<img style='width:150px;height:auto; border:solid #d0d0d0 1px; margin:5px;' src='" + o.GetPhotoUrl() + "' width='150' height='150' />" + o.FullNameWithNarozeni();
                        else
                        {
                            var fn = Init.OsobaFotky.GetFullPath(o, "small.uploaded.jpg");
                            if (System.IO.File.Exists(fn))
                                return "<img style='width:150px;height:auto; border:solid #d0d0d0 1px; margin:5px;' src='data:image/jpeg;base64,"
                                    + Convert.ToBase64String(System.IO.File.ReadAllBytes(fn), Base64FormattingOptions.None) + "' />" + o.FullNameWithNarozeni();
                            else
                                return "Žádná fotka" + o.FullNameWithNarozeni();


                        }
                    }
                    return "Osoba nenalezena";
                case Review.ItemTypes.osobaPopis:
                    if (oldValue)
                    {
                        var data1 = Newtonsoft.Json.Linq.JObject.Parse(review.OldValue);
                        var osobaid = data1.Value<string>("id");
                        if (!string.IsNullOrEmpty(osobaid))
                        {
                            var o1 = Osoby.GetByNameId.Get(data1.Value<string>("id"));
                            if (o1 != null)
                            {
                                return $"<a href='{o1.GetUrl()}'>{o1.FullNameWithNarozeni()}</a>";
                            }
                        }
                        return "(neznama osoba)";
                    }
                    else
                        return review.NewValue;
                default:
                    return string.Empty;
            }

        }
    }
}