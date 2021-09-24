using Nest;

namespace HlidacStatu.Entities.OsobyES
{
    [ElasticsearchType(IdProperty = nameof(NameId))]
    public class OsobaES
    {
        [Keyword]
        public string NameId { get; set; }
        [Text]
        public string ShortName { get; set; }
        [Text]
        public string FullName { get; set; }
        [Number]
        public int? BirthYear { get; set; }
        [Number]
        public int? DeathYear { get; set; }
        [Object]
        public PoliticalFunction[] PoliticalFunctions { get; set; }
        [Text]
        public string PoliticalParty { get; set; }
        [Text]
        public string StatusText { get; set; }
        [Number]
        public int Status { get; set; }
        [Keyword]
        public string PhotoUrl { get; set; }
    }

    public class PoliticalFunction
    {
        [Text]
        public string Name { get; set; }
        [Text]
        public string Organisation { get; set; }
    }
}
