using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace HlidacStatu.MCPServer.Controllers;

public class AuthorizationController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _applicationManager = applicationManager;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken] // For simplicity in this example
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // If the user is not authenticated, redirect them to the login page.
        // This is where you would prompt them for their username and password.
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            // You can pass along the original OpenIddict request to the login page
            // so it can redirect back here after a successful sign-in.
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Authorize))
            });
        }
        
        // Consent is not required, proceed to issue the authorization code.
        // Create a new ClaimsIdentity containing the claims that will be used to create the tokens.
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.SetClaim(OpenIddictConstants.Claims.Subject, _userManager.GetUserId(User));

        // Add claims from the authenticated user principal.
        identity.AddClaims(User.Claims);

        // Tell OpenIddict what scopes the user has consented to.
        var claimsPrincipal = new ClaimsPrincipal(identity);
        claimsPrincipal.SetScopes(request.GetScopes());
        claimsPrincipal.SetResources(request.GetResources());

        // This is a crucial step. It tells OpenIddict to build a response
        // and redirect the user agent back to the client with the authorization code.
        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token"), Produces("application/json")]
    [IgnoreAntiforgeryToken] // Only for API endpoints
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
        {
            // The client credentials grant type is used for machine-to-machine communication.
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                              throw new InvalidOperationException("The application cannot be found.");

            // Create a new ClaimsIdentity for the client application itself.
            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType,
                OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);

            // Use the client_id as the subject identifier.
            identity.SetClaim(OpenIddictConstants.Claims.Subject,
                await _applicationManager.GetClientIdAsync(application));
            identity.SetClaim(OpenIddictConstants.Claims.Name,
                await _applicationManager.GetDisplayNameAsync(application));
            identity.SetDestinations(static claim => claim.Type switch
            {
                OpenIddictConstants.Claims.Name when claim.Subject.HasScope(OpenIddictConstants.Scopes.Profile)
                    => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
                _ => [OpenIddictConstants.Destinations.AccessToken]
            });

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        else if (request.IsAuthorizationCodeGrantType())
        {
            // The authorization code grant type is used for user-based access.
            // Retrieve the claims principal stored in the authorization code.
            var claimsPrincipal =
                (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme))
                .Principal;
            if (claimsPrincipal is null)
            {
                throw new InvalidOperationException("The authorization code cannot be retrieved.");
            }

            // Return an authentication result to OpenIddict.
            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new NotImplementedException("The specified grant is not implemented.");
    }

    [HttpPost("~/connect/introspect"), Produces("application/json")]
    [IgnoreAntiforgeryToken] // Only for API endpoints
    public async Task<IActionResult> Introspect()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (string.IsNullOrEmpty(request.Token))
        {
            throw new InvalidOperationException("The token cannot be found.");
        }

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result.Succeeded)
        {
            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return Forbid(new AuthenticationProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        // The default OpenIddict middleware will handle the protocol-specific logout and redirection.
        return SignOut(new AuthenticationProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
    
    [Consumes("application/json")]
    [HttpPost("~/connect/register"), Produces("application/json")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Register([FromBody] JsonElement payload)
    {
        // Check for required fields in the JSON payload.
        if (!payload.TryGetProperty("redirect_uris", out var redirectUrisElement) || redirectUrisElement.ValueKind != JsonValueKind.Array)
        {
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The 'redirect_uris' parameter is missing or invalid."
            });
        }
        
        if (!payload.TryGetProperty("client_name", out var clientNameElement) || clientNameElement.ValueKind != JsonValueKind.String)
        {
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "The 'client_name' parameter is missing or invalid."
            });
        }

        // Create a new application descriptor.
        var descriptor = new OpenIddictApplicationDescriptor
        {
            // The client_id is a unique identifier. We generate a new one.
            ClientId = Guid.NewGuid().ToString(),
            // The client_secret is used for client authentication. We also generate a new one.
            ClientSecret = Guid.NewGuid().ToString(),
            
            // Set the client's display name.
            DisplayName = clientNameElement.GetString(),
            
            // The client is public, as it's not a confidential client.
            ClientType = OpenIddictConstants.ClientTypes.Public,
            
            // Add the allowed grant types. For a standard web app, this would be authorization code.
            // You can add more grant types as needed, e.g., OpenIddictConstants.GrantTypes.Implicit.
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                OpenIddictConstants.Permissions.Prefixes.Scope + "mcp:tools",
                OpenIddictConstants.Permissions.Prefixes.Scope + "email"
            }
        };

        // Add the redirect URIs from the request.
        foreach (var uriElement in redirectUrisElement.EnumerateArray())
        {
            if (uriElement.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(uriElement.GetString()))
            {
                descriptor.RedirectUris.Add(new Uri(uriElement.GetString()!));
            }
        }
        
        try
        {
            // Use the application manager to create the new client in the database.
            await _applicationManager.CreateAsync(descriptor);
        }
        catch (Exception ex)
        {
            // Handle creation errors.
            return BadRequest(new
            {
                error = OpenIddictConstants.Errors.ServerError,
                error_description = $"An error occurred during client registration: {ex.Message}"
            });
        }
        
        // Return a JSON response with the new client_id and client_secret.
        return Created(
            uri: string.Empty,
            value: new
            {
                client_id = descriptor.ClientId,
                client_secret = descriptor.ClientSecret,
                client_name = descriptor.DisplayName,
                redirect_uris = descriptor.RedirectUris.Select(uri => uri.AbsoluteUri).ToArray()
            });
    }
}