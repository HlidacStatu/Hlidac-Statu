using System;

namespace HlidacStatu.Entities
{
    public partial class AspNetUserApiToken
    {
        public string Id { get; set; }
        public Guid Token { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastAccess { get; set; }
        public int Count { get; set; }

        public AspNetUserApiToken()
        {
        }

        public AspNetUserApiToken(ApplicationUser user) : this(user.Id) { }

        public AspNetUserApiToken(string userId)
        {
            Id = userId;
            Count = 0;
            Created = DateTime.Now;
            LastAccess = null;
            Token = Guid.NewGuid();
        }

    }
}