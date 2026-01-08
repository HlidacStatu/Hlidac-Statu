namespace HlidacStatu.Searching
{
    public class Firmy_OVMKategorie
        : RuleBase
    {
        public Firmy_OVMKategorie(Dictionary<int, string[]> validValues, bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        {
            this.validValues = validValues;
        }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { "kategorieid:", "kategorie:" };
            }
        }

        private readonly Dictionary<int, string[]> validValues;

        protected override Task<RuleResult> processQueryPartAsync(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (part.Prefix.Equals("kategorieid:", StringComparison.InvariantCultureIgnoreCase))
            {
                var katId = part.Value;
                foreach (var key in validValues.Keys)
                {
                    if (katId.Equals(key.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        string icosQuery = $" ( {string.Join(" OR ", validValues[key].Select(t => $"ico:{t}"))} ) ";
                        return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($"{icosQuery}"), NextStep));
                    }
                }
            }


            return null;
        }

    }
}
