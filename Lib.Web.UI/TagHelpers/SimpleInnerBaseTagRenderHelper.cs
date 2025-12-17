using Microsoft.AspNetCore.Razor.TagHelpers;
using static HlidacStatu.Lib.Web.UI.TagHelpers.SimpleInnerBaseTagRenderHelper;
using static HlidacStatu.Lib.Web.UI.TagHelpers.ToggleableTagHelper;

namespace HlidacStatu.Lib.Web.UI.TagHelpers
{


    public abstract class SimpleInnerBaseTagRenderHelper : TagHelper
    {
        public class InnerContext
        {
            public int Num { get; set; }
            public string ContentName { get; set; }

            public string Content { get; set; }
        }

        public static string GetContentFromContext(TagHelperContext context, string contentName)
        {
            if (context.Items.TryGetValue(typeof(SimpleInnerBaseTagRenderHelper).Name + $"_{contentName}_content", out var contentObj))
            {
                return contentObj as string ?? "";
            }
            return "";
        }

        public abstract string contentName { get; }

        protected List<InnerContext> toggleableContent = null;


        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = (await output.GetChildContentAsync()).GetContent();

            //context.Items.TryAdd(typeof(SimpleInnerBaseTagRenderHelper).Name + $"_{this.contentMode}_content", content);

            toggleableContent = (List<InnerContext>)context.Items[typeof(List<InnerContext>)];

            toggleableContent.Add(new InnerContext() { ContentName = contentName, Content = content, Num = toggleableContent.Count });


            output.SuppressOutput(); // Prevent outputting the tag itself.
        }

    }

}