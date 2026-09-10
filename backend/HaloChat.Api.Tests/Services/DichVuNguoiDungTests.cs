using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuNguoiDungTests
{
    private static (DichVuNguoiDung DichVu, NguoiDungGiaLap Kho) TaoDichVu()
    {
        var kho = new NguoiDungGiaLap();
        var dichVu = new DichVuNguoiDung(kho, new DichVuMatKhau());
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
}
