using System;

namespace HlidacStatu.Entities
{

    public class ErrorEnvelope
    {

        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string UserId { get; set; } = null;
        public string apiCallJson { get; set; } = null;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? LastUpdate { get; set; } = null;
        public string Error { get; set; } = null;
        public string Data { get; set; } = null;




    }
}
