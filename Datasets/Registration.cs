using HlidacStatu.Entities;

using System;
using System.Security.Claims;

namespace HlidacStatu.Datasets
{

    public partial class Registration : IAuditable
    {
        public string id { get { return datasetId; } }
        public string name { get; set; }
        public string datasetId { get; set; }
        public string origUrl { get; set; }
        public string sourcecodeUrl { get; set; }
        public string description { get; set; }
        public string jsonSchema { get; set; }

        Newtonsoft.Json.Schema.JSchema _schema = null;
        public Newtonsoft.Json.Schema.JSchema GetSchema()
        {
            if (_schema == null)
            {
                _schema = Newtonsoft.Json.Schema.JSchema.Parse(jsonSchema);
            }

            return _schema;
        }


        //public string registration_elasticmapping { get; set; } = null;
        public string createdBy { get; set; }
        public DateTime created { get; set; } = DateTime.Now;

        public bool betaversion { get; set; } = false;
        public bool allowWriteAccess { get; set; } = false;

        public bool hidden { get; set; } = false;

        public Template searchResultTemplate { get; set; }
        public Template detailTemplate { get; set; }

        public string defaultOrderBy { get; set; } = null;

        public const string DbCreatedLabel = "Datumu importu do db";

        string[,] _orderList = new string[,] { { DbCreatedLabel, "DbCreated" } };
        public string[,] orderList
        {
            get { return _orderList; }
            set
            {
                string[,] tmp = value;
                for (int i = 0; i < tmp.GetLength(0); i++)
                {
                    tmp[i, 0] = tmp[i, 0].Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
                    tmp[i, 1] = tmp[i, 1].Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
                }
                _orderList = tmp;
            }
        }

        public void NormalizeShortName()
        {
            char[] invalidchars = @" #,\/*?""<>|_\+.[]".ToCharArray();
            string invalidregex = @"[" + String.Join("", invalidchars) + "]*";

            string shortName = datasetId;
            if (string.IsNullOrEmpty(shortName))
                shortName = name;
            if (string.IsNullOrEmpty(shortName))
                return;

            shortName = Devmasters.TextUtil.RemoveDiacritics(shortName).Trim();

            shortName = System.Text.RegularExpressions.Regex.Replace(shortName, invalidregex, "-").ToLower();
            shortName = Devmasters.TextUtil.ReplaceDuplicates(shortName, "-");
            shortName = System.Text.RegularExpressions.Regex.Replace(shortName, "^(-)*", "");
            shortName = System.Text.RegularExpressions.Regex.Replace(shortName, "(-)*$", "");


            if (shortName.Length > 120)
                shortName = shortName.Substring(0, 120);
            if (shortName.Length < 5)
                shortName = shortName + "-" + Devmasters.TextUtil.GenRandomString(10);
            datasetId = shortName;
        }


        public DataSet GetDataset()
        {
            if (string.IsNullOrEmpty(datasetId))
                return null;
            else
                return DataSet.CachedDatasets.Get(datasetId);
        }

        public string ToAuditJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public string ToAuditObjectTypeName()
        {
            return "DatasetRegistration";
        }

        public string ToAuditObjectId()
        {
            return datasetId;
        }

        public bool HasAdminAccess(ClaimsPrincipal user)
        {
            if (user is null)
                return false;

            if (user.IsInRole("Admin"))
                return true;

            string email = user.Identity?.Name;

            if (string.IsNullOrEmpty(email))
                return false;

            email = email.ToLower();

            if (string.IsNullOrEmpty(this.createdBy))
                return false; //only superadmins have access

            return this.createdBy.ToLower() == email;
        }

    }

}
