using Devmasters.Enums;

namespace HlidacStatu.Entities.Issues
{

    [ShowNiceDisplayName()]
    [Sortable(SortableAttribute.SortAlgorithm.BySortValue)]
    public enum ImportanceLevel : int
    {
        [Disabled()]
        NeedHumanReview = -1,


        [NiceDisplayName("V pořádku")]
        Ok = 0,
        [NiceDisplayName("Formální problém")]
        Formal = 1,
        [NiceDisplayName("Malý nedostatek")]
        Minor = 5,
        [NiceDisplayName("Vážný nedostatek")]
        Major = 20,
        [NiceDisplayName("Zásadní nedostatek s vlivem na platnost smlouvy")]
        Fatal = 100,
    }

    public class Importance {

        public static string GetCssClass(ImportanceLevel imp, bool withpreDash)
        {
            string res = "";
            if (withpreDash)
                res = "-";

            switch (imp)
            {
                case ImportanceLevel.Formal:
                    return string.Empty;
                case ImportanceLevel.Minor:
                    return res + "info";
                case ImportanceLevel.Major:
                    return res + "warning";
                case ImportanceLevel.Fatal:
                    return res + "danger";
                case ImportanceLevel.NeedHumanReview:
                default:
                    return string.Empty;
            }
        }

        public static string GetIcon(ImportanceLevel imp, string sizeInCss = "90%;", string glyphiconSymbol = "exclamation-sign")
        {
            string res = "<span class=\"text{0} glyphicon glyphicon-{3}\" style=\"font-size:{1}\" aria-hidden=\"true\" title=\"{2}\"></span>";

            return string.Format(res,GetCssClass(imp, true), sizeInCss, imp.ToNiceDisplayName(), glyphiconSymbol );
        }

    }
}
