using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class UptimeServer
    {

        public const string NotInGroup = "other";

        public static Dictionary<string, string> TabsOnWeb = new Dictionary<string, string>()
        {
            {"ustredni","Služby nejdůležitějších úřadů" },
            {"registr","Rejstříky, registry a důležité databáze" },
            {"sluzba","Důležité služby a weby veřejné správy" },
            {"mesta","Městské weby" },
            {"kraj","Krajské weby" },
            {"opendata","Open data, open source" },
            {"geo","Mapy, geografické služby" },
            {"api","API rozhraní digitálních služeb státu" },
            {NotInGroup ,"Ostatní weby" },

        };
        public static string TableGroupsTitle(string groupName)
        {
            if (TabsOnWeb.ContainsKey(groupName.ToLower()))
                return TabsOnWeb[groupName.ToLower()];
            else
                return "";
        }

        string[] _groupArray = null;
        public string[] GroupArray()
        {
            if (_groupArray == null)
            {
                if (string.IsNullOrEmpty(this.Groups))
                    _groupArray = new string[] { };
                else
                    _groupArray = this.Groups.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .ToArray();
            }
            return _groupArray;
        }


        //string _hash = null;
        //public string Hash
        //{
        //    get
        //    {
        //        if (_hash == null)
        //            _hash = Devmasters.Crypto.Hash.ComputeHashToHex(Id + "xxttxx" + Id);
        //        return _hash;
        //    }
        //}
        //public bool ValidHash(string h)
        //{
        //    return h == Hash;
        //}

        Uri _uri = null;
        public string HostDomain()
        {
            InitUri();
            return _uri?.Host;
        }

        private void InitUri()
        {
            if (_uri == null)
            {
                Uri.TryCreate(this.PublicUrl, UriKind.Absolute, out _uri);
            }
        }

        public string opendataUrl { get { return "https://api.hlidacstatu.cz/api/v2/Weby/" + this.Id; } }
        public string pageUrl { get { return "https://www.hlidacstatu.cz/StatniWeby/Info/" + this.pageUrlIdParams; } }
        public string socialBannerUrl { get { return "https://www.hlidacstatu.cz/StatniWeby/banner/" + this.pageUrlIdParams; } }
        public string pageUrlIdParams { get { return this.Id.ToString(); } }


        public UptimeSSL.Statuses? LastUpdateStatusToUptimeStatus()
        {
            if (this.LastUptimeStatus.HasValue)
                return (UptimeSSL.Statuses)this.LastUptimeStatus.Value;
            else return null;
        }
        public Availability.SimpleStatuses LastUptimeStatusToSimpleStatus()
        {
            return Availability.ToSimpleStatus(
                this.LastUpdateStatusToUptimeStatus()
                );
        }
    }
}
