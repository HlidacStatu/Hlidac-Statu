using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using PoliticiEditor.Data;

namespace PoliticiEditor.Components.Account;

internal sealed class UserHelper(AuthenticationStateProvider authenticationStateProvider)
{
    public async Task<ClaimsPrincipal> GetCurrentUserAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User;
    }
}

internal static class UserExtensions
{
    public static List<Claim> CreateClaims(this PoliticiEditorUser user)
    {
        return
        [
            new(ClaimTypes.NameIdentifier, user.NameId),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.Name ?? ""),
        ];
    }
    
    public static string? GetNameId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
    
    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email);
    }
    
    public static string? GetName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Name);
    }
    
}