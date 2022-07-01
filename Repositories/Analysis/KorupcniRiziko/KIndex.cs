using HlidacStatu.Entities.Entities.Analysis;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util.Cache;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Analysis.KorupcniRiziko
{
    public static class KIndex
    {
        public static async Task<KIndexData> GetAsync(string ico, bool useTemp = false)
        {
            if (string.IsNullOrEmpty(ico))
                return null;
            var f = await KIndexData.GetDirectAsync((ico, useTemp));
            if (f == null || f.Ico == "-")
                return null;
            return f;
        }

        public static string PlannedKIndexHash(string ico, int rok)
        {
            string salt = string.Format(Devmasters.Config.GetWebConfigValue("KIndexSaltTemplate"), ico, rok);
            string hash = Devmasters.Crypto.Hash.ComputeHashToHex(salt);
            return Devmasters.Core.Right(hash, 15);
        }

        public static async IAsyncEnumerable<KIndexData> YieldExistingKindexesAsync(string scrollTimeout = "2m", int scrollSize = 300, bool? useTempDb = null)
        {
            useTempDb = useTempDb ?? !string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("UseKindexTemp"));

            var client = await Manager.GetESClient_KIndexAsync();
            if (useTempDb.Value)
                client = await Manager.GetESClient_KIndexTempAsync();


            ISearchResponse<KIndexData> initialResponse = await client.SearchAsync<KIndexData>
                (scr => scr.From(0)
                     .Take(scrollSize)
                     .MatchAll()
                     .Scroll(scrollTimeout));

            if (!initialResponse.IsValid || string.IsNullOrEmpty(initialResponse.ScrollId))
                throw new Exception(initialResponse.ServerError.Error.Reason);

            if (initialResponse.Documents.Any())
                foreach (var document in initialResponse.Documents)
                {
                    // filter to get only existing calculated Kindexes
                    if (document.roky.Any(m => m.KIndexReady))
                        yield return document;
                }

            string scrollid = initialResponse.ScrollId;
            bool isScrollSetHasData = true;
            while (isScrollSetHasData)
            {
                ISearchResponse<KIndexData> loopingResponse = await client.ScrollAsync<KIndexData>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    foreach (var document in loopingResponse.Documents)
                    {
                        // filter to get only existing calculated Kindexes
                        if (document.roky.Any(m => m.KIndexReady))
                            yield return document;
                    }
                    scrollid = loopingResponse.ScrollId;
                }
                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            await client.ClearScrollAsync(new ClearScrollRequest(scrollid));

        }

        public static async Task<bool> HasKIndexValueAsync(string ico, bool useTemp)
        {
            var kidx = await GetAsync(ico, useTemp);
            if (kidx == null)
                return false;
            else
            {
                return kidx.roky.Any(m => m.KIndexReady);
            }
        }

        public static async Task<Tuple<int?, KIndexData.KIndexLabelValues>> GetLastLabelAsync(string ico, bool useTemp=false)
        {
            if (Consts.KIndexExceptions.Contains(ico) && useTemp == false)
                return new Tuple<int?, KIndexData.KIndexLabelValues>(null, KIndexData.KIndexLabelValues.None);

            var kidx = await GetAsync(ico, useTemp);
            if (kidx != null)
            {
                var lbl = kidx.LastKIndexLabel(out int? rok);
                return new Tuple<int?, KIndexData.KIndexLabelValues>(rok, lbl);
            }

            return new Tuple<int?, KIndexData.KIndexLabelValues>(null, KIndexData.KIndexLabelValues.None);
        }


        public static decimal Average(int year)
        {
            var stat = Statistics.KIndexStatTotal.Get().FirstOrDefault(m => m.Rok == year);
            if (stat == null)
                return 0;
            else
                return stat.AverageKindex;
        }
        public static decimal Average(int year, KIndexData.KIndexParts part)
        {
            var stat = Statistics.KIndexStatTotal.Get().FirstOrDefault(m => m.Rok == year);
            if (stat == null)
                return 0;
            else
                return stat.AverageParts.Radky.First(m => m.Velicina == (int)part).Hodnota;
        }
        public static Riziko.RizikoValues ToRiziko(KIndexData.KIndexLabelValues val)
        {
            switch (val)
            {
                case KIndexData.KIndexLabelValues.None:
                    return Riziko.RizikoValues.None;
                case KIndexData.KIndexLabelValues.A:
                    return Riziko.RizikoValues.A;
                case KIndexData.KIndexLabelValues.B:
                    return Riziko.RizikoValues.B;
                case KIndexData.KIndexLabelValues.C:
                    return Riziko.RizikoValues.C;
                case KIndexData.KIndexLabelValues.D:
                    return Riziko.RizikoValues.D;
                case KIndexData.KIndexLabelValues.E:
                    return Riziko.RizikoValues.E;
                case KIndexData.KIndexLabelValues.F:
                    return Riziko.RizikoValues.F;
                default:
                    return Riziko.RizikoValues.None;
            }
        }
        public static Riziko.RizikoValues AsRiziko(this KIndexData.KIndexLabelValues val)
        {
            return ToRiziko(val);
        }

    }
}
