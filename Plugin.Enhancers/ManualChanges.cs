using Devmasters.Collections;

using HlidacStatu.Entities;
using HlidacStatu.Entities.Enhancers;

using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Plugin.Enhancers
{



    public class ManualChanges : IEnhancer
    {
        public int Priority => 10;

        public const string NastaveniSmluvnichStran = "Ruční nastavení smluvních stran";
        public string Description
        {
            get
            {
                return "ManualChanges Enhancer";
            }
        }

        public string Name
        {
            get
            {
                return Description;
            }
        }
        public void SetInstanceData(object data)
        {
        }

        public bool Update(ref Smlouva item)
        {
            return false;
        }

        public bool UpdateSmluvniStrany(ref Smlouva item, Smlouva.Subjekt platce, Smlouva.Subjekt[] prijemce)
        {
            item.Enhancements = item.Enhancements.AddOrUpdate(
                new Enhancement(NastaveniSmluvnichStran, "Ručně nastaven plátce", "item.Platce", Ser(item.Platce), Ser(platce), this)
                );
            item.Platce = platce;

            item.Enhancements = item.Enhancements.AddOrUpdate(
                new Enhancement(NastaveniSmluvnichStran, "Ručně nastaven příjemce", "item.Prijemce", Ser(item.Prijemce), Ser(prijemce), this)
                );
            item.Prijemce = prijemce;

            return true;
        }


        private string Ser(Smlouva.Subjekt subj)
        {
            if (subj == null)
                return null;
            return Newtonsoft.Json.JsonConvert.SerializeObject(new { ico = subj.ico, nazev = subj.nazev });
        }
        private string Ser(IEnumerable<Smlouva.Subjekt> subj)
        {
            if (subj == null)
                return null;
            return Newtonsoft.Json.JsonConvert.SerializeObject(
                    subj
                    .Select(m => Ser(m))
                    .ToArray()
                );
        }


    }
}
