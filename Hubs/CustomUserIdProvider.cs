using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?
            .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }
}
