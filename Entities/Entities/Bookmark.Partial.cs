using Devmasters.Enums;

using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities
{
    public partial class Bookmark
    {

        [ShowNiceDisplayName()]
        [Sortable(SortableAttribute.SortAlgorithm.BySortValue)]
        public enum ItemTypes
        {
            [Disabled()]
            SingleItem = 0,

            [Disabled()]
            Url = 1,

        }

        static string IdDelimiter = "|";

        public static string GetBookmarkId(IBookmarkable item)
        {
            return item.ToAuditObjectTypeName() + IdDelimiter + item.ToAuditObjectId();
        }



        [Nest.Object(Ignore = true)]
        [NotMapped]
        public ItemTypes BookmarkType
        {
            get { return (ItemTypes)ItemType; }
            set { ItemType = (int)value; }
        }


    }
}
