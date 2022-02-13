using System;

namespace HlidacStatu.Entities
{
    public partial class UptimeServer
    {

        public string[] GroupArray()
        {
            if (string.IsNullOrEmpty(this.Groups))
                return new string[] { };
            else
                return this.Groups.Split('|', StringSplitOptions.RemoveEmptyEntries);
        }


        public string Hash()
        {
            return Devmasters.Crypto.Hash.ComputeHashToHex(Host() + "xxttxx" + Host());

        }
        public bool ValidHash(string h)
        {
            return h == Hash();
        }

        Uri _uri = null;
        public string Host()
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
    }
}
