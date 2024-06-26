﻿using HlidacStatu.AI.LLM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.PermanentLLM
{
    public class FullSummary : BaseItem<PrivateLLM.SumarizaceJSON>
    {
        public const string DOCUMENTTYPE = "smlouva";
        public const string PARTTYPE = "SumarizaceJSON";
        public FullSummary()
        {
            //throw new NotImplementedException();
        }
        public FullSummary(string smlouvaId, string fileId, IEnumerable<PrivateLLM.SumarizaceJSON.Item> records)
        : this(smlouvaId,fileId,new PrivateLLM.SumarizaceJSON() { sumarizace = records.ToArray() })
        {

        }
        public override string PartType { get; set; } = "SumarizaceJSON";

        public FullSummary(string smlouvaId, string fileId, PrivateLLM.SumarizaceJSON record)
        {
            this.DocumentID = smlouvaId;
            this.FileID = fileId;
            this.DocumentType = DOCUMENTTYPE;
            this.Created = DateTime.Now;
            this.GeneratorVersion = "1.0.1";
            this.GeneratorName = "aya:35b-hlidac.perSection";            
            this.Parts = record;
            this.PartType = PARTTYPE;

        }
    }
}