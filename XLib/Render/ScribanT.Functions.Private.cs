using HlidacStatu.Entities;

using Scriban.Runtime;

namespace HlidacStatu.XLib.Render
{
    public partial class ScribanT
    {
        public partial class Functions : ScriptObject
        {
            //PRIVATE functions

            public static string fn_Smlouva_GetConfidenceHtml(dynamic smlouva)
            {
                var s = smlouva as Smlouva;
                if (s != null)
                {
                    string tmplt = "<span color='{0}' style='padding:3px 6px 3px 6px;margin-right:4px;font-weight:bold;background-color:{0};color:white;'><b>!</b></span>";
                    var confLevel = s.GetConfidenceLevel();
                    switch (confLevel)
                    {
                        case Entities.Issues.ImportanceLevel.Minor:
                            return string.Format(tmplt, "#31708f");
                        case Entities.Issues.ImportanceLevel.Major:
                            return string.Format(tmplt, "#8a6d3b");
                        case Entities.Issues.ImportanceLevel.Fatal:
                            return string.Format(tmplt, "#a94442");
                        case Entities.Issues.ImportanceLevel.NeedHumanReview:
                        case Entities.Issues.ImportanceLevel.Ok:
                        case Entities.Issues.ImportanceLevel.Formal:
                        default:
                            return "";
                    }
                }
                else
                    return "";
            }

        }
    }
}
