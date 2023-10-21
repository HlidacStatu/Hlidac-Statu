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
            bool first = true;
            foreach (var d in docs)
            {
                res.Add(CreateSearchableDocument(rizeni, d,first));
                first = false;
            }
            return res.ToArray();
        }
        public static string GetDocumentId(Rizeni rizeni, Dokument dokument)
        {
            return GetDocumentId(rizeni.NormalizedId(), dokument.Id);
        }
        public static string GetDocumentId(string normalizedSpisovaZnacka, string dokumentId)
        {
            if (normalizedSpisovaZnacka.Contains(' '))
                normalizedSpisovaZnacka = Rizeni.NormalizedId(normalizedSpisovaZnacka);
            return normalizedSpisovaZnacka + "_" + dokumentId;
        }

        public static SearchableDocument CreateSearchableDocument(Rizeni rizeni, Dokument dokument, bool fullRizeni)
        {
            SearchableDocument sdoc = new SearchableDocument();
            _= dokument.DeepCloneTo(sdoc);

            sdoc.RecordId = GetDocumentId(rizeni,dokument);
            sdoc.SpisovaZnacka = rizeni.SpisovaZnacka;
            sdoc.Rizeni = rizeni.DeepClone();
            if (sdoc.Rizeni.Dokumenty!= null)
                sdoc.Rizeni.Dokumenty = null;

            sdoc.internalVeriteleCount = sdoc.Rizeni.Veritele.Count;
            if (fullRizeni == false)
                sdoc.Rizeni.Veritele = sdoc.Rizeni.Veritele.Take(2).ToList();
            else
                sdoc.internalVeriteleFullList = true;
            sdoc.IsFullRecord = true;
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
        [Nest.Boolean]
        public bool internalVeriteleFullList { get; set; } = false;

        [Object(Ignore = true)]
        public bool IsFullRecord { get; set; } = false;

    }

}
