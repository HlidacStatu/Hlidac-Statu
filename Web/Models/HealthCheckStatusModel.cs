using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Models
{
    public class HealthCheckStatusModel
    {

        public static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<HealthCheckStatusModel> CurrentData =
            new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<HealthCheckStatusModel>(TimeSpan.FromSeconds(5),
                "HealthCheckStatusModel.CurrentData",
                _ =>
                {
                    try
                    {
                        using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent(
                            Devmasters.Config.GetWebConfigValue("HealtChecksUiAPI")
                            )
                        )
                        {
                            
                            net.Timeout = 10 * 1000;
                            net.Tries = 2;
                            var json = net.GetContent(System.Text.Encoding.UTF8).Text;
                            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<HealthCheckStatusModel[]>(json);
                            if (data.Count() > 0)
                                return data[0];
                        }
                    }
                    catch (Exception ex)
                    {
                        return NoData(ex.ToString());
                    }
                    return NoData();
                }
                );

        public static HealthCheckStatusModel NoData(string description = null)
        {
            return new HealthCheckStatusModel()
            {
                id = 0,
                status = "Unhealthy",
                onStateFrom = DateTime.Now,
                lastExecuted = DateTime.Now,
                uri = "",
                name = "NoData",
                entries = new Entry[] {
                new Entry(){
                            id = 0,
                            name = "Healtcheck endpoint",
                            description = description ?? "No data available",
                            duration = "00:00:00.0000",
                            tags = new string[]{ },
                            status = "Unhealthy"
                }
            }
            };
        }

        public int id { get; set; }
        public string status { get; set; }
        public DateTime onStateFrom { get; set; }
        public DateTime lastExecuted { get; set; }
        public string uri { get; set; }
        public string name { get; set; }
        public object discoveryService { get; set; }
        public Entry[] entries { get; set; }
        public HistoryItem[] history { get; set; }

        public string czStatus => ToCZStatus(this.status);
        public string htmlIcon => ToHtmlIcon(this.status);

        public static string ToHtml(string txt)
        {
            return txt?.Replace("\n", "<br/>") ?? "";
        }

        private static string ToCZStatus(string status)
        {
            switch (status.ToLower())
            {
                case "healthy": return "V pořádku";
                case "unhealthy": return "Velké problémy";
                case "degraded": return "Menší problémy";
                default: return status;
            }
        }

        private static string ToHtmlIcon(string status)
        {
            switch (status.ToLower())
            {
                case "healthy": return "<i class='fas fa-check-circle text-success'></i>";
                case "unhealthy": return "<i class='fas fa-exclamation-triangle text-danger' ></i>";
                case "degraded": return "<i class='far fa-exclamation-triangle text-warning' ></i>";
                default: return status;
            }

        }

        public class HistoryItem
        {
            public int id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string status { get; set; }
            public DateTime on { get; set; }
            public string czStatus => ToCZStatus(this.status);
            public string htmlIcon => ToHtmlIcon(this.status);
        }

        public class Entry
        {
            public int id { get; set; }
            public string name { get; set; }
            public string status { get; set; }
            public string description { get; set; }
            public string duration { get; set; }
            public string[] tags { get; set; }
            public string czStatus => ToCZStatus(this.status);
            public string htmlIcon => ToHtmlIcon(this.status);

            public string firstTag => (this.tags?.Count() > 0 ? this.tags[0] : "");

        }

    }
}
