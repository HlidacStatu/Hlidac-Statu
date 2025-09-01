using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{
    public partial class FeedbackModalTagHelper
    {
        public class FormData
        {
            public string ButtonText { get; set; }
            public string? SelectOption { get; set; }
            public string? Style { get; set; }
            public string? IdPrefix { get; set; }
            public string[]? Options { get; set; }
            public bool MustAuth { get; set; }
            public string AddData { get; set; }
            public FormData(string buttonText,
                string? selectOption = null,
                string? style = null,
                string? idPrefix = null,
                string[]? options = null,
                bool mustAuth = false,
                string addData = "")
            {
                ButtonText = buttonText;
                SelectOption = selectOption;
                Style = style;
                IdPrefix = idPrefix;
                Options = options;
                MustAuth = mustAuth;
                AddData = addData;
            }
        }
    }
}
