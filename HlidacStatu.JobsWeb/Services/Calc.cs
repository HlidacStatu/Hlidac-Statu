namespace HlidacStatu.JobsWeb.Services
{
    public class Calc
    {
        public static string PercentChangeHtml(decimal firstVal, decimal changedVal, string title = "procentní rozdíl oproti celkovému trhu")
        { 
            var change = PercentChange(firstVal, changedVal);
            var color = PercentChangeHtmlColorCSS(change);
            return $"<span title='{title}' style='font-size:90%;{color}'>{HlidacStatu.Util.RenderData.NicePercent(change,1,true)}</style>";
        }

            public static decimal PercentChange(decimal firstVal, decimal changedVal)
        {

            if (firstVal == 0)
                return 0m;

            decimal percentage = changedVal / firstVal;
            return percentage;
        }

        public static string PercentChangeHtmlColorCSS(decimal change)
        {
            if (change == 1)
                return "color:inherit";

            Devmasters.Imaging.RGBA color = null;
            if (change < 1)
            {
                color = Devmasters.Imaging.RGBAGradient.GetGradientOfTwoColor((1-change)*3,
                    new Devmasters.Imaging.RGBA("000000"),
                    new Devmasters.Imaging.RGBA("198754")
                    );
            }
            else
            {
                color = Devmasters.Imaging.RGBAGradient.GetGradientOfTwoColor((change-1)*3,
                    new Devmasters.Imaging.RGBA("000000"),
                    new Devmasters.Imaging.RGBA("dc3545"));

            }

            return $"color:{color.ToHex()}";
        }
    }
}
