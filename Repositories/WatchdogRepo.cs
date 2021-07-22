using System.Linq;
using System.Net.Mail;
using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories
{
    public static class WatchdogRepo
    {
        public static void Delete(this WatchDog watchDog)
        {
            using (DbEntities db = new DbEntities())
            {
                if (watchDog.Id == 0)
                    return;
                
                db.WatchDogs.Attach(watchDog);
                db.Entry(watchDog).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public static WatchDog Load(int watchdogId)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.WatchDogs.AsQueryable().FirstOrDefault(m => m.Id == watchdogId);
            }
        }

        public static void Save(this WatchDog watchDog)
        {
            using (DbEntities db = new DbEntities())
            {
                if (watchDog.Id == 0)
                {
                    db.WatchDogs.Add(watchDog);
                }
                else
                {
                    db.WatchDogs.Attach(watchDog);
                    db.Entry(watchDog).State = EntityState.Modified;
                }

                db.SaveChanges();
            }
        }

        public static void DisableWDBySystem(this WatchDog watchDog, WatchDog.DisabledBySystemReasons reason, bool repeated = false)
        {
            watchDog.StatusId = 0;
            Save(watchDog);
            var dataSetId = watchDog.DataType.Replace("DataSet.", "");

            //send info about disabled wd
            var uuser = watchDog.UnconfirmedUser();
            if (uuser != null && Devmasters.TextUtil.IsValidEmail(uuser.Email))
            {
                using (MailMessage msg = new MailMessage())
                {
                    if (reason == WatchDog.DisabledBySystemReasons.NoDataset)
                        msg.Body = "Dobrý den.\n"
                                   + $"Váš hlídač '{watchDog.Name}' hlídající nové záznamy byl deaktivován.\n"
                                   + "\n"
                                   + $"Důvodem je neexistující (nejspíše nedávno smazaný) dataset '{dataSetId}'.\n"
                                   + "\n"
                                   + "S pozdravem\n"
                                   + "\n"
                                   + "Hlídač státu";
                    else if (reason == WatchDog.DisabledBySystemReasons.NoConfirmedEmail)
                    {
                        msg.Body = "Dobrý den.\n"
                                   + $"Váš hlídač '{watchDog.Name}' hlídající nové záznamy byl deaktivován.\n"
                                   + "\n"
                                   + $"Důvodem je nepotvrzená emailová adresa z vaší registrace. Dnes jsme vám znovu poslali email s žádostí o potvrzení emailu z registrace. Stačí kliknout na odkaz v tomto mailu.\n"
                                   + "\n"
                                   + "S pozdravem\n"
                                   + "\n"
                                   + "Hlídač státu";
                        if (repeated == false) //send only once
                            new System.Net.WebClient()
                                .DownloadString(
                                    $"http://www.hlidacstatu.cz/api/v1/ResendConfirmationMail?id={uuser.Id}&Authorization=" +
                                    uuser.GetAPIToken());
                    }
                    else if (reason == WatchDog.DisabledBySystemReasons.InvalidQuery)
                    {
                    }

                    msg.Subject = "Deaktivovaný hlídač nových záznamů na Hlídači státu";
                    msg.To.Add(uuser.Email);
                    msg.BodyEncoding = System.Text.Encoding.UTF8;
                    msg.SubjectEncoding = System.Text.Encoding.UTF8;
                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = Devmasters.Config.GetWebConfigValue("SmtpHost");
                        Util.Consts.Logger.Info("Sending email to " + msg.To);
                        msg.Bcc.Add("michal@michalblaha.cz");
                        smtp.Send(msg);
                    }
                }
            }
        }
        
        public static ApplicationUser UnconfirmedUser(this WatchDog watchDog)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.Users.FirstOrDefault(m => m.Id == watchDog.UserId);
            }
        }
    }
}