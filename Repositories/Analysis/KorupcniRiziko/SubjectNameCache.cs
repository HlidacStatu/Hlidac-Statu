using FullTextSearch;

using HlidacStatu.Connectors;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Analysis.KorupcniRiziko
{
    public class SubjectNameCache : IEquatable<SubjectNameCache>
    {
        public SubjectNameCache(string name, string ico)
        {
            Name = name;
            Ico = ico;
        }

        public static Devmasters.Cache.File.Cache<Dictionary<string, SubjectNameCache>> CachedCompanies =
            new Devmasters.Cache.File.Cache<Dictionary<string, SubjectNameCache>>(
                Init.WebAppDataPath, TimeSpan.Zero, "KIndexCompanies",
                    (o) =>
                    {
                        return ListCompaniesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    });

        private static async Task<Dictionary<string, SubjectNameCache>> ListCompaniesAsync()
        {
            Dictionary<string, SubjectNameCache> companies = new Dictionary<string, SubjectNameCache>();
            await foreach (var kindexRecord in KIndex.YieldExistingKindexesAsync())
            {
                companies.Add(kindexRecord.Ico, new SubjectNameCache(kindexRecord.Jmeno, kindexRecord.Ico));
            }

            return companies;
        }

        public static Dictionary<string, SubjectNameCache> GetCompanies()
        {
            return CachedCompanies.Get();
        }

        [Search]
        public string Name { get; set; }
        [Search]
        public string Ico { get; set; }

        public bool Equals(SubjectNameCache other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Ico == other.Ico;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SubjectNameCache)obj);
        }

        public override int GetHashCode()
        {
            return (Ico != null ? Ico.GetHashCode() : 0);
        }
    }
}
