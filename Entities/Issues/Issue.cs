using System;
using System.Linq;

namespace HlidacStatu.Entities.Issues
{

    [Serializable]
    public class Issue
    {
        public Issue(
            IIssueAnalyzer analyzer, int issueTypeId, string title, string description, ImportanceLevel? importance = null, dynamic affectedParams = null, bool publicVisible = true, bool permanent = false)
        {
            IssueTypeId = issueTypeId;

            AnalyzerType = analyzer != null ? analyzer.GetType().FullName : "Manual";
            Title = title;
            TextDescription = description;
            Importance = (importance.HasValue ? importance.Value :  IssueType.IssueImportance(issueTypeId));
            Public = publicVisible;
            Permanent = permanent;
            if (affectedParams != null)
            {
                AffectedParams = ((object)affectedParams).GetType()
                        .GetProperties()
                        .ToDictionary(p => p.Name, p => p.GetValue(affectedParams, null))
                        .Select(m => new KeyValue() { Key= m.Key, Value= m.Value.ToString() })
                        .ToArray();
            }
        }
        public Issue() { }

        public int IssueTypeId { get; set; } = 0;

        [Nest.Date]
        public DateTime Created { get; set; } = DateTime.Now;

        [Nest.Keyword]
        public string Title { get; set; }

        [Nest.Text]
        public string TextDescription { get; set; }

        public bool Public { get; set; } = true;
        public bool Permanent { get; set; } = false;

        public ImportanceLevel Importance { get; set; }


        public class KeyValue
        {
            [Nest.Keyword]
            public string Key { get; set; }
            [Nest.Text]
            public string Value { get; set; }
        }
        public KeyValue[] AffectedParams { get; set; }

        [Nest.Keyword]
        public string AnalyzerType { get; set; }
    }
}
