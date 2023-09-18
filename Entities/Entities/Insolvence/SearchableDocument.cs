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
            foreach (var d in docs)
            {
                res.Add(CreateSearchableDocument(rizeni, d));
            }
            return res.ToArray();
        }
        public static SearchableDocument CreateSearchableDocument(Rizeni rizeni, Dokument dokument)
        {


            SearchableDocument sdoc = new SearchableDocument();
            _= dokument.DeepCloneTo(sdoc);

            sdoc.RecordId = rizeni.NormalizedId() + "_" + dokument.Id; ;
            sdoc.SpisovaZnacka = rizeni.SpisovaZnacka;
            sdoc.Rizeni = rizeni.DeepClone();
            if (sdoc.Rizeni.Dokumenty!= null)
                sdoc.Rizeni.Dokumenty = null;

            sdoc.internalVeriteleCount = sdoc.Rizeni.Veritele.Count;
            sdoc.Rizeni.Veritele = sdoc.Rizeni.Veritele.Take(2).ToList();
            
            return sdoc;
        }
        public SearchableDocument() { }

        [Keyword]
        public string SpisovaZnacka { get; set; }

        [Keyword]        
        public string RecordId { get; set; }

        public Rizeni Rizeni { get; set; }

        [Nest.Number]
        public int internalVeriteleCount { get; set; } = 0;
    }

}
