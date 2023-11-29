using System;
using System.ComponentModel;

namespace HlidacStatu.Entities
{
    public partial class DocTables
    {



        string _id = null;

        [Description("Unikátní ID zaznamu. Nevyplňujte, ID se vygeneruje samo.")]
        [Nest.Keyword]
        public string Id
        {
            get
            {
                if (_id == null)
                    _id = GetId();

                return this._id;
            }
            set
            {
                _id = value;
            }
        }

        public string GetId()
        {
            return GetId(this.SmlouvaId, this.PrilohaId);
        }
        public static string GetId(string smlouvaId, string prilohaId)
        {
            if (string.IsNullOrEmpty(smlouvaId) || string.IsNullOrEmpty(prilohaId))
                return null;
            return $"{smlouvaId}_{prilohaId}";
        }


        [Nest.Date]
        public DateTime Updated { get; set; } = DateTime.Now;
        [Nest.Keyword]
        public string SmlouvaId { get; set; }
        [Nest.Keyword]
        public string PrilohaId { get; set; }

        public HlidacStatu.DS.Api.TablesInDoc.Result[] Tables { get; set; }



    }
}
