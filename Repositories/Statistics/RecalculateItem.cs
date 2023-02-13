using HlidacStatu.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HlidacStatu.Repositories.Statistics
{

    internal class RecalculateItemEqComparer : IEqualityComparer<RecalculateItem>
    {
        public bool Equals(RecalculateItem x, RecalculateItem y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null)
            {
                return false;
            }
            if (y == null)
            {
                return false;
            }

            return x.UniqueKey == y.UniqueKey;
        }

        public int GetHashCode([DisallowNull] RecalculateItem obj)
        {
            return obj.UniqueKey.GetHashCode();
        }
    }
    public class RecalculateItem
    {
        public RecalculateItem() { }
        public RecalculateItem(Firma f, StatisticsTypeEnum statsType, string provokeBy = null)
        {
            this.Id = f.ICO;
            this.ItemType = ItemTypeEnum.Subjekt;
            this.StatisticsType = statsType;
            this.ProvokedBy = provokeBy;
        }
        public RecalculateItem(Osoba o, StatisticsTypeEnum statsType, string provokeBy = null)
        {
            this.Id = o.NameId;
            this.ItemType = ItemTypeEnum.Person;
            this.StatisticsType = statsType;
            this.ProvokedBy = provokeBy;
        }
        public enum ItemTypeEnum
        {
            Subjekt = 1,
            Person = 2,
        }
        public enum StatisticsTypeEnum
        {
            Smlouva = 1,
            VZ = 2,
            Dotace = 3,
        }

        public string Id { get; set; }
        public ItemTypeEnum ItemType { get; set; }
        public StatisticsTypeEnum StatisticsType { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public string ProvokedBy { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public string UniqueKey
        {
            get
            {
                return $"{ItemType}_{Id}_{StatisticsType}";
            }
        }
    }
}
