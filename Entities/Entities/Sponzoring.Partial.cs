using System;
using System.Collections.Generic;
using System.Linq;
using Devmasters.Enums;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities
{
    public partial class Sponzoring
        : IAuditable //, IValidatableObject
    {

        public Sponzoring()
        {
            Created = DateTime.Now;
        }

        [ShowNiceDisplayName()]
        public enum TypDaru
        {
            [NiceDisplayName("Finanční dar")]
            FinancniDar = 0,
            [NiceDisplayName("Nefinanční dar")]
            NefinancniDar = 1,
            [NiceDisplayName("Dar firmy")]
            DarFirmy = 2,

        }

        public Sponzoring ShallowCopy()
        {
            return (Sponzoring)MemberwiseClone();
        }
        
        public string ToAuditJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public string ToAuditObjectTypeName()
        {
            return "Sponzoring";
        }

        public string ToAuditObjectId()
        {
            return Id.ToString();
        }

        

        
    }
}


