using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("OsobaExternalId")]
    [Microsoft.EntityFrameworkCore.Index(nameof(ExternalId), nameof(ExternalSource), Name = "idx_OsobaExternalId_external")]
    public partial class OsobaExternalId
    {
        public int OsobaId { get; set; }
        
        [StringLength(50)]
        public string ExternalId { get; set; }
        public int ExternalSource { get; set; }
        
        public enum Source
        {
            Merk = 1,
            Firmo = 2,
            HlidacSmluvGuid = 3,


        }
        public OsobaExternalId()
        {
        }
        public OsobaExternalId(int osobaId, string externalId, Source externalsource)
        {
            OsobaId = osobaId;
            ExternalId = externalId;
            ExternalSource = (int)externalsource;
        }
    }
}
