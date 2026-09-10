using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HaloChat.Api.Models;
using HaloChat.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HaloChat.Api.Services;

public class DichVuJwt : IDichVuJwt
{
    private readonly TuyChonJwt _tuyChon;

    public DichVuJwt(IOptions<TuyChonJwt> tuyChon)
    {
        _tuyChon = tuyChon.Value;
    }

    public string TaoJwt(NguoiDung nguoiDung)
    {
        var danhSachClaim = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, nguoiDung.Id),
            new Claim("tenTaiKhoan", nguoiDung.TenTaiKhoan),
            new Claim(JwtRegisteredClaimNames.Email, nguoiDung.Email),
        };

        var khoaKy = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tuyChon.ChuoiBiMat));
        var thongTinKy = new SigningCredentials(khoaKy, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _tuyChon.NguoiPhatHanh,
            audience: _tuyChon.DoiTuong,
            claims: danhSachClaim,
            expires: DateTime.UtcNow.AddMinutes(_tuyChon.SoPhutHetHan),
            signingCredentials: thongTinKy);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
