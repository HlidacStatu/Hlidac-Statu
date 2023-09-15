using HlidacStatu.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HlidacStatu.Repositories;

namespace HlidacStatu.Lib.Analysis.KorupcniRiziko
{
    public class SubjectWithKIndex : Firma.Zatrideni.Item
    {
        public decimal KIndex { get; set; }
    }


    public class SubjectWithKIndexTrend : Firma.Zatrideni.Item
    {
        public decimal KIndex { get; set; }

        public Dictionary<int, decimal> Roky { get; set; }
    }

    public class SubjectWithKIndexAnnualData : Firma.Zatrideni.Item
    {
        public SubjectWithKIndexAnnualData()
        {
        }

        public SubjectWithKIndexAnnualData(Firma.Zatrideni.Item item)
        {
            Group = item.Group;
            Ico = item.Ico;
            Jmeno = item.Jmeno;
            Kraj = item.Kraj;
            KrajId = item.KrajId;
        }

        public async Task PopulateWithAnnualDataAsync(int year)
        {
            if (string.IsNullOrWhiteSpace(Ico))
                throw new Exception("Ico is missing");

            year = KIndexRepo.FixKindexYear(year);
            var kindex = await KIndex.GetAsync(Ico);

            if (kindex != null)
            {
                AnnualData = kindex.ForYear(year);
            }
            else
                AnnualData = KIndexData.Annual.Empty(year);
        }

        public KIndexData.Annual AnnualData { get; set; }
    }
}