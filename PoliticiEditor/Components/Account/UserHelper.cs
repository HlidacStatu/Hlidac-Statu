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

    public List<Claim> CreateClaims(PoliticiEditorUser user)
    {
        return
        [
            new(ClaimTypes.NameIdentifier, user.NameId),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.Name ?? ""),
        ];
    }

    public string? GetNameId(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
    
    public string? GetEmail(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email);
    }
    
    public string? GetName(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Name);
    }

   
}