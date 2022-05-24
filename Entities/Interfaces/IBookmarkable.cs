using System.Threading.Tasks;

namespace HlidacStatu.Entities
{
    public interface IBookmarkable
        : IAuditable
    {
        string GetUrl(bool local);
        string GetUrl(bool local, string foundWithQuery);
        string BookmarkName();
    }

}
