using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog;

namespace HlidacStatu.Lib.Analytics
{
    public class GlobalStatisticsPerYear<T>
        where T : CoreStat, IAddable<T>, new()
    {
        private readonly ILogger _logger = Log.ForContext<GlobalStatisticsPerYear<T>>();
        
        public int[] CalculatedYears = null;

        // Ordered List by neměl být asi úplně ordered list
        public List<PropertyYearPercentiles> StatisticData { get; set; } =
            new List<PropertyYearPercentiles>();

        [Obsolete("Only for JSON deserialization")]
        public GlobalStatisticsPerYear() { }

        static Func<T, bool> alwaysTrue = c => true;

        public GlobalStatisticsPerYear(int[] calculatedYears, IEnumerable<StatisticsSubjectPerYear<T>> dataForAllIcos,
            Func<T, bool> allowedItems = null)
        {
            CalculatedYears = calculatedYears;
            allowedItems = allowedItems ?? alwaysTrue;

            // kdyby nás někoho náhodou napadlo dát do statistik string, tak tohle by to mělo pohlídat
            var numericProperties = typeof(T).GetProperties().Where(p => IsNumericType(p.PropertyType));

            //todo: asi by se dalo zrychlit, kdyby se nejelo po jednotlivých property, ale všechny property najednou
            // dneska na to už ale mentálně nemam :)
            // případně by se dalo paralelizovat do threadů (udělat paralel foreach a jet každý rok v samostatném threadu)
            // musel by se jen zamykat zápis do statistic data (třeba v setteru)
            _logger.Debug($"Starting calculation of all properties for {string.Join(",", CalculatedYears)}");

            foreach (var year in CalculatedYears)
            {
                Devmasters.Batch.Manager.DoActionForAll<PropertyInfo>(numericProperties,
                    property =>
                    {
                        _logger.Debug($"Starting property {property} for {year}");
                        IEnumerable<decimal> globalData = dataForAllIcos
                            .Select(d => d[year])
                            .Where(allowedItems)
                            .Select(d =>
                                GetDecimalValueOfNumericProperty(property, d))
                                .Where(d => d.HasValue)
                                .Select(d => d.Value);
                        _logger.Debug($"calc percentiles for property {property} for {year}");
                        var val = new PropertyYearPercentiles(property.Name, year, globalData);
                        StatisticData.Add(val);
                        _logger.Debug($"Done property {property} for {year}");

                        return new Devmasters.Batch.ActionOutputData();
                    }, true);
            }
            _logger.Debug($"Done calculation of all properties for {string.Join(",", CalculatedYears)}");

        }

        public virtual PropertyYearPercentiles GetPropertyPercentiles(int year, string propertyName)
        {
            return StatisticData.Where(sd => sd.Year == year && sd.PropertyName == propertyName)
                .FirstOrDefault();
        }

        #region helper funcions
        private static HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(uint),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        private static bool IsNumericType(Type type)
        {
            return NumericTypes.Contains(type) ||
                   NumericTypes.Contains(Nullable.GetUnderlyingType(type));
        }

        private static decimal? GetDecimalValueOfNumericProperty(PropertyInfo property, T obj)
        {
            if (obj == null)
                return null;
            return Convert.ToDecimal(property.GetValue(obj, null));
        }
        #endregion
    }
}
