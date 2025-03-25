using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace HlidacStatu.Entities
{

    public class RecalculateItemEqComparer : IEqualityComparer<RecalculateItem>
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

            //ItemType}_{Id}_{StatisticsType
            return x.ItemType == y.ItemType
                && x.Id == y.Id
                && x.StatisticsType == y.StatisticsType;
                
        }

        public int GetHashCode([DisallowNull] RecalculateItem obj)
        {
            return obj.UniqueKey.GetHashCode();
        }
    }

    [Table("RecalculateItemQueue")]
    [PrimaryKey("Pk")]
    public class RecalculateItem
    {
        public class DotaceOptions
        {
            public enum ChangeEnum
            {
                None = 0,
                Insert = 1, Update = 2, Delete = 3,
            }
            public int Version { get; set; } = 1;
            public string PoskytovatelIco { get; set; }
            public string PrijemceIco { get; set; }
            public bool ForceRecalculate { get; set; } = false;
            public ChangeEnum Change { get; set; }
        }

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
        public RecalculateItem(Osoba o, StatisticsTypeEnum statsType, DotaceOptions dotaceOptions, string provokeBy = null)
        {
            this.Id = o.NameId;
            this.ItemType = ItemTypeEnum.Person;
            this.StatisticsType = statsType;
            this.ProvokedBy = provokeBy;
            this.Options = Newtonsoft.Json.JsonConvert.SerializeObject(dotaceOptions, Newtonsoft.Json.Formatting.None);
        }
        public enum ItemTypeEnum : int
        {
            Subjekt = 1,
            Person = 2,
        }
        public enum StatisticsTypeEnum : int
        {
            Smlouva = 1,
            VZ = 2,
            Dotace = 3,
        }

        [Key(), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Pk { get; set; }

        [StringLength(256)]
        public string Id { get; set; }

        public string Options { get; set; }

        [Column(TypeName = "int")]
        public ItemTypeEnum ItemType { get; set; }

        [Column(TypeName = "int")]
        public StatisticsTypeEnum StatisticsType { get; set; }

        [Required]
        public DateTime Created { get; set; } = DateTime.Now;


        public DateTime? Started { get; set; } = null;
        public DateTime? Finished { get; set; } = null;

        [Required]
        [StringLength(256)]
        public string ProvokedBy { get; set; }

        public string UniqueKey
        {
            get
            {
                return $"{ItemType}_{Id}_{StatisticsType}";
            }
        }
    }
}
