using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Schema.NET;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace HlidacStatu.Lib.Web.UI.TagHelpers
{

    [HtmlTargetElement("accordion")]
    [RestrictChildren("content")]
    public class AccordionTagHelper : TagHelper
    {
        public const string REPLACE_WITH_ID_MARK = "$$_ID_$$";
        public class InnerContext
        {
            public bool Collapsed { get; set; }

            public string Content { get; set; }
        }


        [HtmlAttributeName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [HtmlAttributeName("css")]
        public string Css { get; set; } = "";
        [HtmlAttributeName("style")]
        public string Style { get; set; } = "";
        [HtmlAttributeName("theme")]
        public string Theme { get; set; } = "";

        // Allows access to ViewContext if needed
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var innerContext = new List<InnerContext>();
            context.Items.Add(typeof(List<InnerContext>), innerContext);
            var childContent = await output.GetChildContentAsync();
            var innerContent = (List<InnerContext>)context.Items[typeof(List<InnerContext>)];


            // Build the script to wire up the toggling behavior.
            var sb = new StringBuilder(1024);
            switch (this.Theme)
            {
                case "primary":
                case "secondary":
                case "success":
                case "danger":
                case "dark":
                    sb.AppendLine($"<style>");
                    sb.AppendLine($".accordion-theme-{this.Id} {{");
                    sb.AppendLine($"  --bs-accordion-active-bg: var(--bs-{this.Theme});");
                    sb.AppendLine($"  --bs-accordion-btn-bg: var(--bs-{this.Theme}-bg-subtle);");
                    sb.AppendLine($"  --bs-accordion-active-color: #fff;");
                    sb.AppendLine($"  --bs-accordion-btn-color: #000;");
                    sb.AppendLine($"}}</style>");
                    break;

                case "info":
                case "warning":
                case "light":
                    sb.AppendLine($"<style>");
                    sb.AppendLine($".accordion-theme-{this.Id} {{");
                    sb.AppendLine($"  --bs-accordion-active-bg: var(--bs-{this.Theme});");
                    sb.AppendLine($"  --bs-accordion-btn-bg: var(--bs-{this.Theme}-bg-subtle);");
                    sb.AppendLine($"  --bs-accordion-active-color: #000;");
                    sb.AppendLine($"  --bs-accordion-btn-color: #000;");
                    sb.AppendLine($"}}</style>");
                    break;
                default:
                    break;
            }


            sb.AppendLine($"<div class=\"accordion accordion-theme-{this.Id} {Css}\" style=\"{Style}\" id=\"{this.Id}\">");
            foreach (var item in innerContent)
            {
                sb.AppendLine(item.Content
                        .Replace(REPLACE_WITH_ID_MARK, this.Id)
                    );
            }
            sb.AppendLine($"</div");

            // Remove the outer tag and set the generated content.
            output.TagName = null;
            output.Content.SetHtmlContent(sb.ToString());
        }
    }



    [HtmlTargetElement("content", ParentTag = "accordion")]
    public class AccordionInnerBaseTagHelper : TagHelper
    {
        [HtmlAttributeName("collapsed")]
        public bool Collapsed { get; set; } = true;

        [HtmlAttributeName("style-title")]
        public string StyleTitle { get; set; } = "";

        [HtmlAttributeName("css-title")]
        public string CssTitle { get; set; } = "";

        [HtmlAttributeName("title")]
        public string Title { get; set; }

        [HtmlAttributeName("style-body")]
        public string StyleBody { get; set; } = "";

        [HtmlAttributeName("css-body")]
        public string CssBody { get; set; } = "";

        protected List<AccordionTagHelper.InnerContext> innerContent = null;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            innerContent = (List<AccordionTagHelper.InnerContext>)context.Items[typeof(List<AccordionTagHelper.InnerContext>)];
            var num = innerContent.Count;

            var content = (await output.GetChildContentAsync()).GetContent();

            StringBuilder sb = new StringBuilder(512);
            sb.AppendLine($"  <div class=\"accordion-item\">");
            sb.AppendLine($"    <h2 class=\"accordion-header\" style=\"{StyleTitle}\" >");
            sb.AppendLine($"            <button class=\"accordion-button {(this.Collapsed?" collapsed ":"")} {CssTitle}\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#{AccordionTagHelper.REPLACE_WITH_ID_MARK}_{num}\" aria-expanded=\"true\" aria-controls=\"{AccordionTagHelper.REPLACE_WITH_ID_MARK}_{num}\">");
            sb.AppendLine($"       {this.Title}");
            sb.AppendLine($"      </button>");
            sb.AppendLine($"    </h2>");

            sb.AppendLine($"    <div id=\"{AccordionTagHelper.REPLACE_WITH_ID_MARK}_{num}\" class=\"accordion-collapse collapse {(this.Collapsed ? "" : "show")}\">");
            sb.AppendLine($"       <div class=\"accordion-body {CssBody}\" style=\"{StyleBody}\">");
            sb.AppendLine(content);
            sb.AppendLine($"       </div>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"  </div>");



            innerContent.Add(new AccordionTagHelper.InnerContext()
            {
                Collapsed = this.Collapsed,
                Content = sb.ToString()
            });
            output.SuppressOutput(); // Prevent outputting the tag itself.
        }

    }
}