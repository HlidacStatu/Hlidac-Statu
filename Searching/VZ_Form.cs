namespace HlidacStatu.Searching
{
    public class VZ_Form
        : RuleBase
    {
        public VZ_Form(bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        { }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { "form:" };
            }
        }


        protected override Task<RuleResult> processQueryPartAsync(SplittingQuery.Part part)
        {
            if (part == null)
                return Task.FromResult<RuleResult>(null);

            if (part.Prefix.Equals("form:", StringComparison.InvariantCultureIgnoreCase))
            {
                string form = part.Value;

                if (!string.IsNullOrEmpty(form))
                {
                    string[] forms = form.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                    string q_form = "";
                    if (forms.Length > 0)
                        q_form = $"formulare.druh:({string.Join(" OR ",forms.Select(s => s + "*"))})";

                    return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($" ( {q_form} ) "), NextStep));
                }
            }


            return Task.FromResult<RuleResult>(null);
        }

    }
}
