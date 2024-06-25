using HlidacStatu.AI.LLM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.PermanentLLM
{
    public class Summary : BaseItem<PrivateLLM.SumarizaceJSON>
    {
        public const string DOCUMENTTYPE = "smlouva";
        public Summary()
        {
            throw new NotImplementedException();
        }
        public Summary(string smlouvaId, string fileId, IEnumerable<PrivateLLM.SumarizaceJSON.Item> records)
        : this(smlouvaId,fileId,new PrivateLLM.SumarizaceJSON() { sumarizace = records.ToArray() })
        {

        }
        public Summary(string smlouvaId, string fileId, PrivateLLM.SumarizaceJSON record)
        {
            this.DocumentID = smlouvaId;
            this.FileID = fileId;
            this.DocumentType = DOCUMENTTYPE;
            this.Created = DateTime.Now;
            this.GeneratorVersion = "1.0.1";
            this.GeneratorName = "aya:35b-hlidac.perSection";            
            this.Parts = record;
        }
    }
}
