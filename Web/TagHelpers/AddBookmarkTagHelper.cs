using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Framework;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HlidacStatu.Web.TagHelpers
{
    /// <summary>
    /// Přidání ikonky k zabookmarkování stránky. Je potřeba buď vyplnit atribut item="".
    /// Nebo můžeme vyplnit atributy name="" a url="".
    /// </summary>
    public class AddBookmarkTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// BookMark Item - v případě že odkazujeme na objekt. Nevyplňujte name, ani url.
        /// </summary>
        public object? Item { get; set; }

        /// <summary>
        /// BookMark name - vyplňte spolu s url="".
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// BookMark url - vyplňte spolu s name="".
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Bookmark ItemId - vyplňte pouze pokud vypisujete záložky ze seznamu.
        /// </summary>
        public string? ItemId { get; set; }

        /// <summary>
        /// Bookmark ItemType - vyplňte pouze pokud vypisujete záložky ze seznamu.
        /// </summary>
        public int? ItemType { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var bookmarkableItem = Item as IBookmarkable;

            if (bookmarkableItem == null && string.IsNullOrWhiteSpace(Url))
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "a";
            output.Content.AppendHtml("<i class=\"fad fa-bookmark\"></i>");
            output.Attributes.Add("href", "#");

            //if user is not authenticated not authenticated 
            if (!ViewContext.IsAuthenticatedRequest())
            {
                string desc = "Uložit do záložek. Všechny uložené položky najdete na jednom míste ve vašem profilu a hlavičce.";

                output.Attributes.Add("class", "bookmark bookmarkOff");
                output.Attributes.Add("alt", desc);
                output.Attributes.Add("title", desc);
                output.Attributes.Add("data-toggle", "modal");
                output.Attributes.Add("data-target", "#bookmarkInfoAnon");
                output.Attributes.Add("onclick", "return false;");

                return;
            }

            bool isBookmarked;
            if (bookmarkableItem is null)
            {
                ItemId ??= Devmasters.Crypto.Hash.ComputeHashToHex(ViewContext.GetDisplayUrl());
                ItemType ??= (int)Bookmark.ItemTypes.Url;
                var currentItemType = (Bookmark.ItemTypes)ItemType;
                isBookmarked = BookmarkRepo.IsItemBookmarked(currentItemType, ItemId, ViewContext.GetUserIdentity()!.Name);
            }
            else
            {
                ItemId = Bookmark.GetBookmarkId(bookmarkableItem);
                ItemType = (int)Bookmark.ItemTypes.SingleItem;
                isBookmarked = BookmarkRepo.IsItemBookmarked(bookmarkableItem, ViewContext.GetUserIdentity()!.Name);
                Name = bookmarkableItem.BookmarkName();
                Url = bookmarkableItem.GetUrl(true);
            }

            SetOutput(isBookmarked, output);

        }

        private void SetOutput(bool isBookmarked, TagHelperOutput output)
        {
            string cssVal = "bookmark ";
            string descr;
            if (isBookmarked)
            {
                cssVal += "bookmarkOn";
                descr = "Uloženo v záložkách. Klikem ze záložek odstraníte.";
            }
            else
            {
                cssVal += "bookmarkOff";
                descr = "Uložit do záložek. Všechny uložené položky najdete na jednom míste ve vašem profilu a hlavičce.";
            }

            output.Attributes.Add("class", cssVal);
            output.Attributes.Add("alt", descr);
            output.Attributes.Add("title", descr);
            output.Attributes.Add("bmname", Name);
            output.Attributes.Add("bmurl", Url);
            output.Attributes.Add("bmid", ItemId);
            output.Attributes.Add("bmtype", ItemType);
            output.Attributes.Add("onclick", $"javascript: _my_event('send', 'event', 'bookmark', '{ViewContext.GetRequestPath()}', 'authenticated');ChangeBookmark(this);return false;");
        }

    }
}