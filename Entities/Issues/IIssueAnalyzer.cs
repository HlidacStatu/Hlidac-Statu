using System.Collections.Generic;

namespace HlidacStatu.Entities.Issues
{


    public interface IIssueAnalyzer
    {
        string Name { get; }
        string Description { get; }
        IEnumerable<Issue> FindIssues(Smlouva item);
    }


}
