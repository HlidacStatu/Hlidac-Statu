using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{
    [HtmlTargetElement("alert-modal")]
    public class AlertModalTagHelper : TagHelper
    {
        public string OpenModalBtnText { get; set; }
        public string HeaderText { get; set; }
        public string AlertText { get; set; }
        public string CloseModalBtnText { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            output.Content.SetHtmlContent($@"
<button type=""button""
        class=""btn btn-primary btn-sm""
        data-bs-toggle=""modal""
        data-bs-target=""#eventModal"">
    {OpenModalBtnText}
</button>
<div class=""modal fade"" id=""eventModal"" tabindex=""-1"" role=""dialog"" aria-labelledby=""eventModalLabel"" aria-hidden=""true"">
    <div class=""modal-dialog"" role=""document"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <h4 class=""modal-title"" id=""eventModalLabel"">{HeaderText}</h4>
            </div>
            <div class=""modal-body"">
                <p>
                    {AlertText}
                </p>
            </div>
            <div class=""modal-footer"">
                <button type=""button"" class=""btn btn-secondary"" data-dismiss=""modal"">{CloseModalBtnText}</button>
            </div>
        </div>
    </div>
</div>");



        }

    }
}