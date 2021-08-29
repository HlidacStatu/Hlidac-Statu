using System;

namespace HlidacStatu.Entities
{

    public abstract class DateUserOption
        : UserOptions<DateTime>
    {

        public DateUserOption()
        {
        }

        public DateUserOption(ParameterType option, int? languageId = null)
            : base(option, languageId)
        {
        }

        public DateUserOption(ApplicationUser user, ParameterType option, int? languageId = null)
            : base(user, option, languageId)
        {
        }

        protected override string SerializeToString(DateTime value)
        {
            return value.Ticks.ToString();
        }

        protected override DateTime DeserializeFromString(string value)
        {
            return new DateTime(Convert.ToInt64(value));
        }

    }
}
