﻿using HlidacStatu.Entities;
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

            return x.UniqueKey == y.UniqueKey;
        }

        public int GetHashCode([DisallowNull] RecalculateItem obj)
        {
            return obj.UniqueKey.GetHashCode();
        }
    }

    [Table("RecalculateItemQueue")]
    [PrimaryKey("Id","ItemType","StatisticsType")]
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

        [Key, Column(Order =0)]
        [StringLength(256)]
        public string Id { get; set; }

        [Key]
        [Column(Order = 1,TypeName = "int")]
        public ItemTypeEnum ItemType { get; set; }

        [Key]
        [Column(Order = 2,TypeName = "int")]
        public StatisticsTypeEnum StatisticsType { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
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