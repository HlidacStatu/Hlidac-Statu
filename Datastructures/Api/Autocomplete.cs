using System;

namespace HlidacStatu.DS.Api
{
    public class Autocomplete : IEquatable<Autocomplete>
    {
        public string Id { get; set; }
        public string Text { get; set; }

        public string DisplayText { get; set; }
        public string AdditionalHiddenSearchText { get; set; }
        public string ImageElement { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string KIndex { get; set; }
        public float PriorityMultiplier { get; set; }

        public string Prefix { get; set; }
        public string PrefixValue { get; set; }

        public CategoryEnum Category { get; set; } = CategoryEnum.None;

        public Autocomplete Clone() => (Autocomplete)MemberwiseClone();

        public bool Equals(Autocomplete other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Autocomplete)obj);
        }

        public override int GetHashCode()
        {
            return (Id != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(Id) : 0);
        }

        public string RenderText()
        {
            return string.IsNullOrWhiteSpace(DisplayText) ? Text : DisplayText;
        }

        public enum CategoryEnum
        {
            None = 0,
            Company,
            StateCompany,
            Authority,
            City,
            Person,
            Oblast,
            Synonym,
            Operator,
            Hint,
            Kindex
        }
    }


}
