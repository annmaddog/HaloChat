using HaloChat.Api.Models;
using HaloChat.Api.Options;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuNguoiDungTests
{
    private static (DichVuNguoiDung DichVu, NguoiDungGiaLap Kho) TaoDichVu()
    {
        var kho = new NguoiDungGiaLap();
        var dichVuJwt = new DichVuJwt(Microsoft.Extensions.Options.Options.Create(new TuyChonJwt
        {
            ChuoiBiMat = "khoa-bi-mat-du-dai-danh-cho-kiem-thu-toi-thieu-32-ky-tu",
        }));
        var dichVu = new DichVuNguoiDung(kho, new DichVuMatKhau(), dichVuJwt);
        return (dichVu, kho);
    }

    [Fact]
    public async Task DangKyTaiKhoan_TenTaiKhoanDaTonTai_TraVeThatBai()
    {
        var (dichVu, kho) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "NguyenAn", Email = "khac@gmail.com" });

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_EmailDaTonTai_TraVeThatBai()
    {
        var (dichVu, kho) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "Khac", Email = "nguyenan@gmail.com" });

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_HopLe_LuuMatKhauDaBamKhongLuuBanRo()
    {
        var (dichVu, kho) = TaoDichVu();

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.True(ketQua.ThanhCong);
        var daLuu = Assert.Single(kho.DanhSach);
        Assert.Equal("NguyenAn", daLuu.TenTaiKhoan);
        Assert.NotEqual("MatKhau123", daLuu.MatKhauBam);
        Assert.NotEmpty(daLuu.Salt);
    }

    [Fact]
    public async Task DangNhap_SaiMatKhau_TraVeNull()
    {
        var (dichVu, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var token = await dichVu.DangNhap("NguyenAn", "SaiMatKhau");

        Assert.Null(token);
    }

    [Fact]
    public async Task DangNhap_TaiKhoanKhongTonTai_TraVeNull()
    {
        var (dichVu, _) = TaoDichVu();

        var token = await dichVu.DangNhap("KhongTonTai", "MatKhau123");

        Assert.Null(token);
    }

    [Fact]
    public async Task DangNhap_DungMatKhauBangTenTaiKhoan_TraVeToken()
    {
        var (dichVu, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var token = await dichVu.DangNhap("NguyenAn", "MatKhau123");

        Assert.NotNull(token);
    }

    [Fact]
    public async Task DangNhap_DungMatKhauBangEmail_TraVeToken()
    {
        var (dichVu, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var token = await dichVu.DangNhap("nguyenan@gmail.com", "MatKhau123");

        Assert.NotNull(token);
    }
}
