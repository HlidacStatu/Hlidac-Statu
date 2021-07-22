using System;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;
using Nest;

namespace HlidacStatu.Repositories
{
    public static class ErrorEnvelopeRepo
    {
        public static void Save(ErrorEnvelope errorEnvelope, ElasticClient client = null)
        {
            errorEnvelope.LastUpdate = DateTime.Now;
            var es = client ?? Manager.GetESClient_VerejneZakazkyNaProfiluConverted();
            es.IndexDocument<ErrorEnvelope>(errorEnvelope);
        }
    }
}