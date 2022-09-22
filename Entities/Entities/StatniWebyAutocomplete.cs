using System;

namespace HlidacStatu.Entities
{
    public class StatniWebyAutocomplete : IEquatable<StatniWebyAutocomplete>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Ico { get; set; }
        public string Url { get; set; }
        public string HostDomain { get; set; }

        public StatniWebyAutocomplete()
        { }
        
        public StatniWebyAutocomplete(UptimeServer uptimeServer)
        {
            Id = uptimeServer.Id;
            Name = uptimeServer.Name;
            Description = uptimeServer.Description;
            Ico = uptimeServer.ICO;
            Url = uptimeServer.PublicUrl;
            HostDomain = uptimeServer.HostDomain();

        }
        
        public bool Equals(StatniWebyAutocomplete other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StatniWebyAutocomplete)obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

   
    }


}
