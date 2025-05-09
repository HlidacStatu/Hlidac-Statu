using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HlidacStatu.Lib.Analytics
{
    [JsonConverter(typeof(StatisticsPerYearConverter))]
    public class StatisticsPerYear<T> : IEnumerable<(int Year, T Value)>
        where T : CoreStat, IAddable<T>, new()
    {
        [JsonInclude]
        protected Dictionary<int, T> Years { get; set; } = new Dictionary<int, T>();

        /// <summary>
        /// Tenhle měsíc určuje, za který rok se AKTUÁLNÍ data mají zobrazovat.
        /// Pokud chceme pro nějakou datovou sadu nastavit sezónu jinak, je potřeba ho změnit
        /// </summary>
        public StatisticsPerYear()
        {
        }

        public StatisticsPerYear(Func<T, int> yearSelector, IEnumerable<T> data)
        {
            if (yearSelector == null)
                throw new ArgumentNullException(nameof(yearSelector));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            foreach (var item in data)
            {
                Years.Add(yearSelector(item), item);
            }
        }

        /// <summary>
        /// Creates new statistics
        /// </summary>
        /// <param name="years">Dictionary where key = Year, value = T</param>
        public StatisticsPerYear(Dictionary<int, T> years)
        {
            Years = years ?? throw new ArgumentNullException(nameof(years));
        }

        /// <summary>
        /// Copy constructor to create children objects
        /// </summary>
        public StatisticsPerYear(StatisticsPerYear<T> other)
        {
            Years = other.Years;
        }

        public T this[int year]
        {
            get { return Years.TryGetValue(year, out var value) ? value : new T(); }
            set { Years[year] = value; }
        }

        public bool HasStatistics
        {
            get { return Years.Count != 0; }
        }

        public int FirstYear()
        {
            if (Years == null)
                return 0;
            if (Years.Count == 0)
                return 0;
            return Years.Keys.Min();
        }

        public int LastYear()
        {
            if (Years == null)
                return 0;
            if (Years.Count == 0)
                return 0;
            return Years.Keys.Max();
        }

        public Dictionary<int,T> Filter(Func<KeyValuePair<int,T>,bool> yearPredicate)
        {
            return Years.Where(yearPredicate)
                .ToDictionary(k=>k.Key, v=>v.Value);
        }

        public int Sum(Func<T, int> selector)
        {
            return Years.Values.Sum(selector);
        }

        public long Sum(Func<T, long> selector)
        {
            return Years.Values.Sum(selector);
        }

        public decimal Sum(Func<T, decimal> selector)
        {
            return Years.Values.Sum(selector);
        }

        public int Sum(int[] forYears, Func<T, int> selector)
        {
            return Years
                .Where(y => forYears.Contains(y.Key))
                .Select(v => v.Value)
                .Sum(selector);
        }

        public long Sum(int[] forYears, Func<T, long> selector)
        {
            return Years
                .Where(y => forYears.Contains(y.Key))
                .Select(v => v.Value)
                .Sum(selector);
        }

        public decimal Sum(int[] forYears, Func<T, decimal> selector)
        {
            return Years
                .Where(y => forYears.Contains(y.Key))
                .Select(v => v.Value)
                .Sum(selector);
        }


        public int[] YearsAfter2016()
        {
            return Years
                .Select(y => y.Key)
                .Where(y => y >= 2016)
                .ToArray();
        }

        public int[] YearsBefore2016()
        {
            return Years
                .Select(y => y.Key)
                .Where(y => y < 2016)
                .ToArray();
        }

        public (decimal change, decimal? percentage) ChangeBetweenYears(int forYear,
            Func<T, decimal> selector)
        {
            return ChangeBetweenYears(forYear - 1, forYear, selector);
        }

        public (decimal change, decimal? percentage) ChangeBetweenYears(int firstYear,
            int lastYear,
            Func<T, decimal> selector)
        {
            var firstStat = this[firstYear];
            var lastStat = this[lastYear];

            var firstValue = selector(firstStat);
            var lastValue = selector(lastStat);


            decimal change = lastValue - firstValue;
            decimal? percentage = (firstValue == 0) ? (decimal?)null : change / firstValue;

            return (change, percentage);
        }

        public virtual int CurrentSeasonYear()
        {
            var obj = new T();

            return DateTime.Now.Month >= obj.NewSeasonStartMonth() ? DateTime.Now.Year : DateTime.Now.Year - 1;
        }

        public virtual int CurrentYear()
        {
            return DateTime.Now.Year;
        }

        public T CurrentSeasonStatistics()
        {
            return this[CurrentSeasonYear()];
        }

        public T StatisticsForYear(int year)
        {
            return this[year];
        }

        public static StatisticsPerYear<T> AggregateStats(IEnumerable<StatisticsPerYear<T>> statistics,
            int[] onlyYears = null)
        {
            var aggregatedStatistics = new StatisticsPerYear<T>();
            if (statistics is null)
                return aggregatedStatistics;

            var years = statistics.Where(x => x != null).SelectMany(x => x.Years.Keys.Select(k => k)).Distinct();
            if (onlyYears != null && onlyYears.Count() > 0)
                years = years.Where(y => onlyYears.Contains(y));
            foreach (var year in years)
            {
                var statsForYear = statistics.Where(s => s != null).Select(s => s[year]);
                var val = statsForYear.Aggregate(new T(), (acc, s) => acc.Add(s));

                aggregatedStatistics.Years.Add(year, val);
            }

            return aggregatedStatistics;
        }

        public static StatisticsPerYear<T> SubtractFromStats(IEnumerable<StatisticsPerYear<T>> statistics,
            int[] onlyYears = null)
        {
            var aggregatedStatistics = new StatisticsPerYear<T>();
            if (statistics is null)
                return aggregatedStatistics;

            var years = statistics.Where(x => x != null).SelectMany(x => x.Years.Keys.Select(k => k)).Distinct();
            if (onlyYears != null && onlyYears.Count() > 0)
                years = years.Where(y => onlyYears.Contains(y));
            foreach (var year in years)
            {
                var statsForYear = statistics.Where(s => s != null).Select(s => s[year]);
                var val = statsForYear.Aggregate(new T(), (acc, s) => acc.Subtract(s));

                aggregatedStatistics.Years.Add(year, val);
            }

            return aggregatedStatistics;
        }

        /// <summary>
        /// Summarize all selected years to one - Good if you need global percentage stats.
        /// </summary>
        /// <param name="years"></param>
        /// <returns></returns>
        public T Summary(int year) => Summary(new int[] { year });

        public T Summary(int[] years)
        {
            var val = Years.Where(y => years.Contains(y.Key))
                .Select(y => y.Value)
                .Aggregate(new T(), (acc, s) => acc.Add(s));

            return val;
        }

        public T Summary(CoreStat.UsualYearsInterval summType)
        {
            CoreStat cs = new T();

            int[] years = cs.YearsFromUsual(summType);
            if (years == null)
                return Summary();
            else
                return Summary(years);
        }


        /// <summary>
        /// Summarize all years to one - Good if you need global percentage stats.
        /// </summary>
        /// <returns></returns>
        public virtual T Summary()
        {
            var val = Years
                .Select(y => y.Value)
                .Aggregate(new T(), (acc, s) => acc.Add(s));

            return val;
        }

        public IEnumerator<(int Year, T Value)> GetEnumerator()
        {
            foreach (var element in Years)
            {
                yield return (element.Key, element.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class StatisticsPerYearConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            // Check if the type is a Statistics<> instance
            return 
                (
                   typeToConvert.IsGenericType &&
                   typeToConvert.GetGenericTypeDefinition() == typeof(StatisticsPerYear<>))
               ||
               (
                   typeToConvert.BaseType?.IsGenericType == true &&
                   typeToConvert.BaseType.GetGenericTypeDefinition() == typeof(StatisticsPerYear<>)
               );
        }

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            // Create an instance of the target type
            var instance = Activator.CreateInstance(typeToConvert);

            // Read the JSON object
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return instance;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    var property = typeToConvert.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    // Skip the property if it's not found or if it's an indexer
                    if (property == null || property.GetSetMethod(true) is null)
                    {
                        reader.Skip();
                        continue;
                    }

                    reader.Read();

                    var propertyValue = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                    property.SetValue(instance, propertyValue);
                }
            }
            
            throw new JsonException("JSON does not match the expected format.");

        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            var valueType = value.GetType();
            
            // načíst publicProperties
            var publicProperties =
                valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CustomAttributes.All(a => a.AttributeType != typeof(JsonIgnoreAttribute)));
            // načíst privateProperties
            var privateProperties =
                valueType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(JsonIncludeAttribute)));

            var allProps = publicProperties.Union(privateProperties)
                .Where(prop => prop.GetIndexParameters().Length == 0); // odstranit indexer
            
            writer.WriteStartObject();

            foreach (var property in allProps)
            {
                var val = property.GetValue(value);
                writer.WritePropertyName(property.Name);
                JsonSerializer.Serialize(writer, val, options);
            }
                
            writer.WriteEndObject();
        }
    }
}