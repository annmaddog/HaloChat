using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Api.Services;

/// <summary>
/// SignalR mặc định lấy user id từ claim ClaimTypes.NameIdentifier để phục vụ
/// Clients.User(id). Token của HaloChat phát hành claim "sub" (JwtRegisteredClaimNames.Sub,
/// vì options.MapInboundClaims = false nên claim giữ nguyên tên gốc) — provider này đọc
/// đúng claim đó thay vì dựa vào mặc định.
/// </summary>
public class NguoiDungIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
