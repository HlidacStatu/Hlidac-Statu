using Force.DeepCloner;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.Insolvence
{

    [Nest.ElasticsearchType(IdProperty = nameof(RecordId))]
    public class SearchableDocument : Dokument
    {
        public static SearchableDocument[] CreateSearchableDocuments(Rizeni rizeni)
        {
            var docs = rizeni.Dokumenty.ToList();
            rizeni.Dokumenty = null;
            List<SearchableDocument> res = new List<SearchableDocument>();
            bool clean = false;
            foreach (var d in docs)
            {
                res.Add(CreateSearchableDocument(rizeni, d, clean));
                clean = true;
            }
            return res.ToArray();
        }
        public static SearchableDocument CreateSearchableDocument(Rizeni rizeni, Dokument dokument, bool cleanData)
        {


            SearchableDocument sdoc = new SearchableDocument();
            _= dokument.DeepCloneTo(sdoc);

            sdoc.RecordId = rizeni.NormalizedId() + "_" + dokument.Id; ;
            sdoc.SpisovaZnacka = rizeni.SpisovaZnacka;
            sdoc.internalTopHit = cleanData ? 0 : 1;
            sdoc.Rizeni = rizeni.DeepClone();
            if (sdoc.Rizeni.Dokumenty!= null)
                sdoc.Rizeni.Dokumenty = null;
            if (cleanData)
            {
                sdoc.internalVeriteleCount = sdoc.Rizeni.Veritele.Count;
                sdoc.Rizeni.Veritele = sdoc.Rizeni.Veritele.Take(2).ToList();
            }
            return sdoc;
        }
        public SearchableDocument() { }

        [Keyword]
        public string SpisovaZnacka { get; set; }

        [Keyword]        
        public string RecordId { get; set; }

        public Rizeni Rizeni { get; set; }

        [Nest.Number]
        public int internalTopHit { get; set; } = 0;
        [Nest.Number]
        public int internalVeriteleCount { get; set; } = 0;
    }

}
