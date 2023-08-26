using System;

namespace HlidacStatu.Entities.Enhancers
{
    public class Enhancement : IEquatable<Enhancement>
    {

        public Enhancement() { }

        public Enhancement(string title, string description, string changedParameter,
            string changedOldValue, string changedNewValue, IEnhancer enhancer
            )
            : this(title, description, changedParameter, changedOldValue, changedNewValue, enhancer.GetType().FullName)
        {
        }

        public Enhancement(string title, string description, string changedParameter,
            string changedOldValue, string changedNewValue, string enhancerName
            )
        {
            Title = title;
            Description = description;
            Changed = new Change()
            {
                ParameterName = changedParameter,
                PreviousValue = changedOldValue,
                NewValue = changedNewValue
            };
            EnhancerType = enhancerName;

        }
        public class Change
        {
            [Nest.Keyword]
            public string ParameterName { get; set; }

            [Nest.Keyword]
            public string PreviousValue { get; set; }
            [Nest.Keyword]
            public string NewValue { get; set; }

        }
        [Nest.Date]
        public DateTime Created { get; set; } = DateTime.Now;

        [Nest.Keyword]
        public string Title { get; set; }

        [Nest.Keyword]
        public string Description { get; set; }

        public Change Changed { get; set; } = new Change();

        public bool Public { get; set; } = true;


        [Nest.Keyword]
        public string EnhancerType { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Enhancement);
        }
        public bool Equals(Enhancement other) // todo: use GetHashCode for comparison instead of comparing strings
        {
            if (other == null)
                return false;
            if (EnhancerType != other.EnhancerType)
                return false;
            if (Title != other.Title)
                return false;
            if (Changed != null && other.Changed != null)
            {
                if (Changed.ParameterName != other.Changed.ParameterName)
                    return false;
            }
            if (
                (Changed != null && other.Changed == null)
                ||
                (Changed == null && other.Changed != null)
                )
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + EnhancerType.GetHashCode();
                hash = hash * 23 + Title.GetHashCode();
                hash = hash * 23 + Changed?.ParameterName.GetHashCode() ?? 0;
                return hash;
            }
        }

        public static bool operator ==(Enhancement enh1, Enhancement enh2)
        {
            if (((object)enh1) == null || ((object)enh2) == null)
                return Object.Equals(enh1, enh2);

            return enh1.Equals(enh2);
        }

        public static bool operator !=(Enhancement enh1, Enhancement enh2)
        {
            if (((object)enh1) == null || ((object)enh2) == null)
                return !Object.Equals(enh1, enh2);

            return !(enh1.Equals(enh2));
        }
    }
}
