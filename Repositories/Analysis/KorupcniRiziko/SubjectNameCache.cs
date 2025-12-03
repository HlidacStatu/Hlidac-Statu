using System;

namespace HlidacStatu.Repositories.Analysis.KorupcniRiziko
{
    public class SubjectNameCache : IEquatable<SubjectNameCache>
    {
        public SubjectNameCache(string name, string ico)
        {
            Name = name;
            Ico = ico;
        }

        public string Name { get; set; }
        public string Ico { get; set; }

        public bool Equals(SubjectNameCache other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Ico == other.Ico;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SubjectNameCache)obj);
        }

        public override int GetHashCode()
        {
            return (Ico != null ? Ico.GetHashCode() : 0);
        }
    }
}
