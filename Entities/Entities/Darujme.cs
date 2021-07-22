namespace HlidacStatu.Entities
{
    public class Darujme
    {


        public class Stats
        {
            public Projectstats projectStats { get; set; }
            public class Projectstats
            {
                public class Collectedamountestimate
                {
                    public long cents { get; set; }
                    public string currency { get; set; }
                }

                public long projectId { get; set; }
                public Collectedamountestimate collectedAmountEstimate { get; set; }
                public long donorsCount { get; set; }
            }

        }

    }
}
