using System;
using System.Security.Cryptography;

namespace HlidacStatu.Entities.Entities.PoliticiSelfAdmin;

// Add profile data for application users by adding properties to the ApplicationUser class
public class PoliticiLoginToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }

    public static PoliticiLoginToken CreateTokenForUser(int userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        return new PoliticiLoginToken
        {
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddHours(3),
            Used = false,
            Token = token,
            UserId = userId
        };
    }
}