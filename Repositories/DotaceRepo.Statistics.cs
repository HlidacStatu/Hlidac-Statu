using HlidacStatu.Lib.Analytics;
using System.Linq;
using static HlidacStatu.Repositories.DotaceRepo;

namespace HlidacStatu.Repositories
{
    public static partial class DotaceRepo
    {

        public class Statistics
        {
            public class TypeStatsPerYear : StatisticsPerYear<SimpleStat>                
            {
                public HlidacStatu.Entities.Dotace.Hint.Type DotaceType { get; set; }

            }
            public class CategoryStatsPerYear : StatisticsPerYear<SimpleStat>
            {
                public HlidacStatu.Entities.Dotace.Hint.CalculatedCategories Category { get; set; }

            }

            public class PerYearResultMore<T> : CoreStat, IAddable<PerYearResultMore<T>>
            {
                public long Count { get; set; }
                public decimal Sum { get; set; }
                public T More { get; set; }

                public PerYearResultMore<T> Add(PerYearResultMore<T> other)
                {
                    return new PerYearResultMore<T>
                    {
                        Count = this.Count + other.Count,
                        Sum = this.Sum + other.Sum,
                        More = this.More,
                    };
                }

                public override int NewSeasonStartMonth()
                {
                    return 1;
                }

                public PerYearResultMore<T> Subtract(PerYearResultMore<T> other)
                {
                    return new PerYearResultMore<T>
                    {
                        Count = this.Count - other.Count,
                        Sum = this.Sum - other.Sum,
                        More = this.More,
                    };
                }

                public override int UsualFirstYear()
                {
                    return DotaceRepo.DefaultLimitedYears.Min();
                }
            }
        }
    }
}