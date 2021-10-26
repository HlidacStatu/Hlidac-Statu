using System;

namespace HlidacStatu.Entities
{
    public partial class Audit
    {
        [Nest.Keyword]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [Nest.Date]
        public DateTime date { get; set; }
        [Nest.Keyword]
        public string userId { get; set; }
        [Nest.Keyword]
        public string operation { get; set; }
        
        /// <summary>
        /// Name of object (mostly in db) Eg. OsobaEvent, sponzoring,...
        /// </summary>
        [Nest.Keyword]
        public string objectType { get; set; }
        /// <summary>
        /// Id of object in object's type (objectType)
        /// </summary>
        [Nest.Keyword]
        public string objectId { get; set; }
        public string valueBefore { get; set; }
        public string valueAfter { get; set; }
        [Nest.Keyword]
        public string IP { get; set; }
        [Nest.Keyword]
        public string requestUrl { get; set; }
        [Nest.Keyword]
        public string traceId { get; set; }
        [Nest.Keyword]
        public string method { get; set; }
        [Nest.Keyword]
        public string additionalData { get; set; }
        [Nest.Keyword]
        public int statusCode { get; set; }
        

        public enum Operations
        {
            Read,
            Update,
            Delete,
            Create,
            Other,
            InvalidAccess,
            Call,
            Search,
            UserSearch
        }

    }
}
