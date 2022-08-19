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

    public class Importance
    {

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
                    return res + "warning bg-opacity-25";
                case ImportanceLevel.Major:
                    return res + "warning";
                case ImportanceLevel.Fatal:
                    return res + "danger";
                case ImportanceLevel.NeedHumanReview:
                default:
                    return string.Empty;
            }
        }

        public static string GetIcon(ImportanceLevel imp, string sizeInCss = "90%;", string glyphiconSymbol = "exclamation-circle")
        {
            
            return $"<span class=\"text{GetCssClass(imp, true)} fas fa-{glyphiconSymbol}\" style=\"font-size:{sizeInCss}\" aria-hidden=\"true\" title=\"{imp.ToNiceDisplayName()}\"></span>";

        }

    }
}
