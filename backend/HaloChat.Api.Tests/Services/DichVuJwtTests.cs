using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using HaloChat.Api.Models;
using HaloChat.Api.Options;
using HaloChat.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuJwtTests
{
    private static DichVuJwt TaoDichVu() => new(Microsoft.Extensions.Options.Options.Create(new TuyChonJwt
    {
        ChuoiBiMat = "khoa-bi-mat-du-dai-danh-cho-kiem-thu-toi-thieu-32-ky-tu",
        NguoiPhatHanh = "HaloChat",
        DoiTuong = "HaloChatNguoiDung",
        SoPhutHetHan = 60,
    }));

    [Fact]
    public void TaoJwt_TraVeTokenChuaThongTinNguoiDung()
    {
        var dichVu = TaoDichVu();
        var nguoiDung = new NguoiDung { Id = "123", TenTaiKhoan = "NguyenAn", Email = "nguyenan@gmail.com" };

        var token = dichVu.TaoJwt(nguoiDung);
        var payload = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("123", payload.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("NguyenAn", payload.Claims.First(c => c.Type == "tenTaiKhoan").Value);
        Assert.True(payload.ValidTo > DateTime.UtcNow);
    }
}
