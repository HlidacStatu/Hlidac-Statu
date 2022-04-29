using Devmasters;

using HlidacStatu.Connectors.External.ProfilZadavatelu;
using HlidacStatu.Entities.VZ;

using System;
using System.Net;
using System.Threading;
using System.Xml.Serialization;

namespace HlidacStatu.Repositories.ProfilZadavatelu
{
    public class Parser
    {
        private static Semaphore sem = null;
        private static object lockObj = new object();
        private static Parser instance = null;
        private static int defaultMaxNumberOfThreads = 2;

        static Parser()
        {
            int threads;
            int.TryParse(Config.GetWebConfigValue("ProfilZadavatelu.Parser"), out threads);
            if (threads < 1)
                threads = defaultMaxNumberOfThreads;
            sem = new Semaphore(threads, threads, "HlidacStatu.Lib.Data.External.ProfilZadavatelu.Parsers");
        }
        public static Parser Instance()
        {
            lock (lockObj)
            {
                if (instance == null)
                {
                    instance = new Parser();
                }
            }
            return instance;
        }

        private Parser()
        {
        }

        public void ProcessProfileZadavatelu(ProfilZadavatele profil, DateTime from)
        {
            ProcessProfileZadavatelu(profil, from, DateTime.Now);
        }
        public void ProcessProfileZadavatelu(ProfilZadavatele profil, DateTime from, DateTime to)
        {

            var di = new DateTimeInterval(from, to);
            var intervals = di.Split(DateTimeInterval.Interval.Days, 30, true);


            foreach (var interv in intervals)
            {
                DateTime end = interv.End;
                if (interv.End > DateTime.Now)
                    end = DateTime.Now;
                var log = _processReqProfiluZadavatel(profil, interv.Start, end);
                if (log.HttpValid == false || log.XmlValid == false)
                {
                    break;
                }
            }
        }

        private Entities.Logs.ProfilZadavateleDownload _processReqProfiluZadavatel(ProfilZadavatele profil, DateTime from, DateTime to)
        {
            string xmlUrlTemp = profil.Url;
            if (profil.Url?.EndsWith("/") == true)
                xmlUrlTemp = xmlUrlTemp + "XMLdataVZ?od={0:ddMMyyy}&do={1:ddMMyyyy}";
            else
                xmlUrlTemp = xmlUrlTemp + "/XMLdataVZ?od={0:ddMMyyy}&do={1:ddMMyyyy}";


            var xml = "";
            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
            sw.Start();
            var surl = string.Format(xmlUrlTemp, from, to);
            var ReqLog = new Entities.Logs.ProfilZadavateleDownload() { Date = DateTime.Now, ProfileId = profil.Id, RequestedUrl = surl };
            try
            {
                sem.WaitOne();
                using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent(surl))
                {
                    //net.TimeInMsBetweenTries = 20*1000;
                    //net.Tries = 1;
                    net.Timeout = 60 * 1000;
                    xml = net.GetContent().Text;
                    ReqLog.HttpValid = true;
                }
            }
            catch (Devmasters.Net.HttpClient.UrlContentException ex)
            {
                ReqLog.HttpValid = false;
                ReqLog.HttpError = ex.ToString();

                if (ex.InnerException != null && ex.InnerException.GetType() == typeof(WebException))
                {
                    var wex = (WebException)ex.InnerException;
                    ReqLog.HttpError = wex.ToString();
                    if (wex.Status == WebExceptionStatus.ProtocolError && wex.Response != null)
                    {
                        ReqLog.HttpErrorCode = (int)(((HttpWebResponse)wex.Response).StatusCode);
                    }
                }
                await ProfilZadavateleDownloadRepo.SaveAsync(ReqLog);
                profil.LastAccessResult = ProfilZadavatele.LastAccessResults.HttpError;
                profil.LastAccess = DateTime.Now;
                await ProfilZadavateleRepo.SaveAsync(profil);
                return ReqLog;

            }
            catch (WebException wex)
            {
                ReqLog.HttpValid = false;
                ReqLog.HttpError = wex.ToString();
                if (wex.Status == WebExceptionStatus.ProtocolError && wex.Response != null)
                {
                    ReqLog.HttpErrorCode = (int)(((HttpWebResponse)wex.Response).StatusCode);
                }
                await ProfilZadavateleDownloadRepo.SaveAsync(ReqLog);
                profil.LastAccessResult = ProfilZadavatele.LastAccessResults.HttpError;
                profil.LastAccess = DateTime.Now;
                await ProfilZadavateleRepo.SaveAsync(profil);
                return ReqLog;

            }
            catch (Exception e)
            {
                ReqLog.HttpValid = false;
                ReqLog.HttpError = e.ToString();
                await ProfilZadavateleDownloadRepo.SaveAsync(ReqLog);
                profil.LastAccessResult = ProfilZadavatele.LastAccessResults.HttpError;
                profil.LastAccess = DateTime.Now;
                await ProfilZadavateleRepo.SaveAsync(profil);
                return ReqLog;
            }
            finally
            {
                sem.Release();
                sw.Stop();
                ReqLog.ResponseMs = sw.ElapsedMilliseconds;
            }


            ProfilStructure prof = null;
            try
            {
                prof = ParserXml(xml);
                ReqLog.XmlValid = true;
            }
            catch (Exception e)
            {
                ReqLog.XmlValid = false;
                ReqLog.XmlError = e.ToString();
                ReqLog.XmlInvalidContent = xml;
                await ProfilZadavateleDownloadRepo.SaveAsync(ReqLog);

                profil.LastAccessResult = ProfilZadavatele.LastAccessResults.XmlError;
                profil.LastAccess = DateTime.Now;
                await ProfilZadavateleRepo.SaveAsync(profil);
                return ReqLog;
            }
            if (prof != null)
            {
                var cli = ES.Manager.GetESClient_VerejneZakazkyNaProfiluRaw();

                foreach (var zak in prof.zakazka)
                {
                    ZakazkaRaw myZak = new ZakazkaRaw(zak, profil);
                    await myZak.SaveAsync();
                }
                await ProfilZadavateleDownloadRepo.SaveAsync(ReqLog);
                profil.LastAccessResult = ProfilZadavatele.LastAccessResults.OK;
                profil.LastAccess = DateTime.Now;
                await ProfilZadavateleRepo.SaveAsync(profil);

            }
            return ReqLog;


        }

        private ProfilStructure ParserXml(string xml)
        {
            using (var xmlReader = new System.IO.StringReader(xml))
            {

                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ProfilStructure));
                    // Use the Deserialize method to restore the object's state.
                    var d = (ProfilStructure)serializer.Deserialize(xmlReader);
                    return d;
                }
                catch (Exception)
                {
                    throw;
                }

            }

        }
    }
}
