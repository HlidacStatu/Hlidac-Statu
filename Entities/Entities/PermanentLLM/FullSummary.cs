using HlidacStatu.AI.LLM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.PermanentLLM
{
    public class FullSummary : BaseItem<SumarizaceJSON>
    {
        public const string DOCUMENTTYPE = "smlouva";
        public const string PARTTYPE = "SumarizaceJSON";
        public FullSummary()
        {
            //throw new NotImplementedException();
        }
        public FullSummary(string smlouvaId, string fileId, string generatorName, IEnumerable<SumarizaceJSON.Item> records)
        : this(smlouvaId,fileId,generatorName, new SumarizaceJSON() { sumarizace = records.ToArray() })
        {

        }
        public override string PartType { get; set; } = PARTTYPE;

        public FullSummary(string smlouvaId, string fileId,string generatorName, SumarizaceJSON record)
        {
            this.DocumentID = smlouvaId;
            this.FileID = fileId;
            this.DocumentType = DOCUMENTTYPE;
            this.Created = DateTime.Now;
            this.GeneratorVersion = "1.0.2";
            this.GeneratorName = generatorName;            
            this.Parts = record;
            this.PartType = PARTTYPE;

        }
    }
}
