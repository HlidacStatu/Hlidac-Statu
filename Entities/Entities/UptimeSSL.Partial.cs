using Devmasters.Enums;

using System;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class UptimeSSL
    {
        public SSLGrades SSLGrade()
        {
            return ToSSLGrades(this.OverallGrade);
        }

        public static SSLGrades ToSSLGrades(string textGrade)
        {
            if (string.IsNullOrEmpty(textGrade))
                return SSLGrades.X;

            switch (textGrade.ToUpper())
            {
                case "A+":
                    return SSLGrades.Aplus;
                case "A":
                    return SSLGrades.A;
                case "A-":
                    return SSLGrades.Aminus;
                case "B":
                    return SSLGrades.B;
                case "C":
                    return SSLGrades.C;
                case "D":
                    return SSLGrades.D;
                case "E":
                    return SSLGrades.E;
                case "F":
                    return SSLGrades.F;
                case "T":
                    return SSLGrades.T;
                case "M":
                    return SSLGrades.M;
                default:
                    return SSLGrades.X;
            }
        }
        public enum Statuses
        {
            OK = 0,
            Pomalé = 1,
            Nedostupné = 2,
            TimeOuted = 98,
            BadHttpCode = 99,
            Unknown = 1000,
        }

        [ShowNiceDisplayName()]
        public enum SSLGrades
        {
            [NiceDisplayName("A+")]
            Aplus = 1,
            A = 2,
            [NiceDisplayName("A-")]
            Aminus = 3,
            B = 5,
            C = 6,
            D = 7,
            E = 8,
            F = 9,
            [NiceDisplayName("T")]
            T = 20,
            [NiceDisplayName("M")]
            M = 50,
            [NiceDisplayName("X")]
            X = 100

        }

        public static string StatusStyleColor(SSLGrades? grade)
        {
            string color = "";
            if (grade.HasValue)
            {
                switch (grade)
                {
                    case SSLGrades.Aplus:
                    case SSLGrades.A:
                    case SSLGrades.Aminus:
                        color = "success";
                        break;
                    case SSLGrades.B:
                    case SSLGrades.C:
                    case SSLGrades.D:
                    case SSLGrades.E:
                        color = "warning";
                        break;
                    case SSLGrades.F:
                    case SSLGrades.T:
                    case SSLGrades.M:
                    case SSLGrades.X:
                        color = "danger";
                        break;
                }
            }
            else
                color = "muted";

            return color;
        }
        public static string StatusDescription(SSLGrades? grade, bool longVersion = false)
        {
            string txt = "";
            if (grade.HasValue)
            {
                switch (grade)
                {
                    case SSLGrades.Aplus:
                    case SSLGrades.A:
                    case SSLGrades.Aminus:
                        txt = "Všechno je v nejlepším pořádku.";
                        if (longVersion)
                            txt = "Všechno je v nejlepším pořádku a web se drží doporučených postupů.";
                        break;
                    case SSLGrades.B:
                    case SSLGrades.C:
                    case SSLGrades.D:
                    case SSLGrades.E:
                        txt = "Služba se nedrží doporučených postupů.";
                        if (longVersion)
                            txt = "Služba se nedrží doporučených postupů a jeho nastavení je zastaralé. Sice to neznamená bezprostřední a snadno zneužitelné ohrožení, ale je to znak špatně spravovaného serveru a útok je za určitých okolností možný.";
                        break;
                    case SSLGrades.F:
                        txt = "Web sice podporuje HTTPS, ale špatně.";
                        if (longVersion)
                            txt = "Web sice podporuje HTTPS, ale jeho parametry jsou nastavené tak špatně, že je skoro spíš na škodu, protože vzbuzuje falešný pocit bezpečí.";
                        break;
                    case SSLGrades.T:
                        txt = "HTTPS používá nedůveryhodný certifikát";
                        if (longVersion)
                            txt = "Služba je chráněna certifikátem od certifikační autority, kterou hlavní prohlížeče neznají hlavní prohlížeče neznají nebo jí nedůvěřují. V českých podmínkách to znamená nejspíše certifikáty vydané našimi vnitrostátními autoritami (ICA, PostSignum a eIdentity), které sice stát zákonem prohlásil za důvěryhodné, ale které nesplňují mezinárodní podmínky nutné proto, aby je za důvěryhodné pokládali i autoři prohlížečů.";
                        break;
                    case SSLGrades.M:
                        txt = "Služba používá certifikát pro jiný server.";
                        if (longVersion)
                            txt = "Služba používá certifikát, který byl vystaven pro jiný název serveru. Může se jednat o chybu v konfiguraci serveru, ale také o to, že na dané IP adrese běží jiný HTTPS web, než jaký testujeme.";
                        break;
                    case SSLGrades.X:
                        txt = "Služba HTTPS nepodporuje vůbec.";
                        break;
                }
            }
            else
                txt = "muted";

            return txt;
        }

        public static string StatusOrigColor(SSLGrades? grade)
        {
            string color = "";
            if (grade.HasValue)
            {
                switch (grade)
                {
                    case SSLGrades.Aplus:
                    case SSLGrades.A:
                    case SSLGrades.Aminus:
                        color = "#7ed84d";
                        break;
                    case SSLGrades.B:
                    case SSLGrades.C:
                    case SSLGrades.D:
                    case SSLGrades.E:
                        color = "#ffa100";
                        break;
                    case SSLGrades.F:
                    case SSLGrades.T:
                    case SSLGrades.M:
                    case SSLGrades.X:
                        color = "#ef251e";
                        break;
                }
            }
            else
                color = "#777777";

            return color;
        }

        public static string StatusHtml(SSLGrades? grade, int size = 40)
        {
            string color = StatusOrigColor(grade);

            return string.Format(
                @"<div class='center-block' style='background:{2};font-family: Arial, Helvetica, sans-serif;text-align: center;width: {0}px;height: {0}px;font-size: {1}px;line-height: {0}px;font-weight: bold;color: #ffffff;'>"
                + (grade?.ToNiceDisplayName() ?? "?")
                + "</div>", size, size / 2, color);

        }

        public static UptimeSSL CreateFromSteps(UptimeSSL.Step[] steps, string filename = null)
        {

            UptimeSSL uptimeSSL = new UptimeSSL();
            uptimeSSL.Steps = steps;
            uptimeSSL.OverallGrade = steps.FirstOrDefault(m => m.id == "overall_grade")?.finding ?? "X";
            uptimeSSL.OverallScore = Devmasters.ParseText.ToDecimal(steps.FirstOrDefault(m => m.id == "final_score")?.finding) ?? 0m;
            uptimeSSL.Created = DateTime.Now;
            if (filename != null)
            {
                var dt = Devmasters.RegexUtil.GetRegexGroupValue(filename, @".*_p\d{1,5}-(?<dt>\d{8}-\d{2,4})", "dt");
                //20220210-1755
                DateTime? date = Devmasters.DT.Util.ToDateTime(dt, "yyyyMMdd-HHmm", "yyyyMMdd-Hm");
                if (date.HasValue)
                    uptimeSSL.Created = date.Value;
            }
            uptimeSSL.Domain = steps.First(m => !string.IsNullOrEmpty(m.ip)).ip.Split('/')[0];

            return uptimeSSL;
        }


    }
}
