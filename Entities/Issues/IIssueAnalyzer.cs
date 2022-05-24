using System.Collections.Generic;
using System.Threading.Tasks;

namespace HlidacStatu.Entities.Issues
{


    public interface IIssueAnalyzer
    {
        string Name { get; }
        string Description { get; }
        Task<IEnumerable<Issue>> FindIssuesAsync(Smlouva item);
    }


}
