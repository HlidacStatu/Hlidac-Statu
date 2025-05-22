using System;

namespace HlidacStatu.Entities.Entities.PoliticiSelfAdmin;

public class PoliticiEditorUser
{
    public int Id { get; set; }
    public string? NameId { get; set; }
    public string? Email { get; set; }
    public string? EmailUpper { get; private set; }
    public string? EmailHash { get; private set; }
    
    public string? PhoneNumber { get; set; }
    public string? Name { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool IsLockedOut { get; set; }
    public bool IsApproved { get; set; }
    public string? RegistrationInfo { get; set; }
    public string? BirthYearOrDate { get; set; }

    public void SetEmailProperties()
    {
        if(Email is null)
            return;
        
        Email = Email.Trim();
        EmailUpper = Email.Trim().ToUpperInvariant();
        EmailHash = GetEmailHash(Email);
    }
    
    public static string GetEmailHash(string email)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant());
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
    
}