using HlidacStatu.Repositories.ES;
using Nest;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;

namespace HlidacStatu.Repositories
{
    public static class SkutecniMajiteleRepo
    {
        public static DateTime PlatnyOd = new DateTime(2021, 6, 1);
        
        public static HashSet<int> KodyPravniFormyKtereMajiVyjimku = new()
        {
            100, 101, 102, 103, 104, 105, 106, 107, 108, 145, 
            301, 302, 312, 313, 314, 325, 331, 332, 352, 353, 361, 362, 381, 382, 
            421, 424, 521, 525, 601, 641, 661, 
            703, 706, 707, 711, 721, 722, 723, 733, 741, 745, 761, 771, 
            801, 804, 805, 811, 907, 937, 941, 952
        };
        
        public static HashSet<int> StatniFirmyKdeNeniPotrebaSkutecnyMajitel = new()
        {
            1, 2, 10, 20
        };

        private static ElasticClient _skmClient = Manager.GetESClientAsync("skutecni-majitele", idxType: Manager.IndexType.DataSource)
            .ConfigureAwait(false).GetAwaiter().GetResult();

        public static bool PodlehaSkm(Firma firma, DateTime datumPocatku)
        {
            if (datumPocatku < PlatnyOd)
                return false;
            
            if(KodyPravniFormyKtereMajiVyjimku.Contains(firma.Kod_PF ?? 0))
                return false;
                
            if(StatniFirmyKdeNeniPotrebaSkutecnyMajitel.Contains(firma.Typ ?? 0))
                return false;

            return true;
        }

        public static async Task<bool> MaSkutecnehoMajiteleAsync(string ico, DateTime datumPocatku)
        {
            if (string.IsNullOrWhiteSpace(ico)) throw new ArgumentNullException(nameof(ico));
            var firma = FirmaRepo.FromIco(ico);

            if (PodlehaSkm(firma, datumPocatku))
            {
                var result = await GetAsync(firma.ICO);

                //skm nenalezen
                if (result == null)
                    return false;
            }

            return true;
        }
        
        public static async Task<SkutecnyMajitel> GetAsync(string ico)
        {
            if (string.IsNullOrWhiteSpace(ico)) throw new ArgumentNullException(nameof(ico));

            var response = await _skmClient.GetAsync<SkutecnyMajitel>(ico);

            return response.IsValid
                ? response.Source
                : null;
        }
        

    }
}