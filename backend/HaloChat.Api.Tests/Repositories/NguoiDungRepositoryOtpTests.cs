using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class NguoiDungRepositoryOtpTests
{
    [Fact]
    public async Task LuuOtpAsync_GhiDungCaBonFieldVaResetSoLanThuSai()
    {
        var kho = new NguoiDungGiaLap();
        var nguoiDung = new NguoiDung { SoLanThuSai = 3 };
        kho.DanhSach.Add(nguoiDung);
        var hetHan = DateTime.UtcNow.AddMinutes(10);
        var guiLucNao = DateTime.UtcNow;

        await kho.LuuOtpAsync(nguoiDung.Id, "ma-bam", hetHan, guiLucNao);

        Assert.Equal("ma-bam", nguoiDung.MaOtpBam);
        Assert.Equal(hetHan, nguoiDung.MaOtpHetHan);
        Assert.Equal(guiLucNao, nguoiDung.MaOtpGuiLucNao);
        Assert.Equal(0, nguoiDung.SoLanThuSai);
    }

    [Fact]
    public async Task TangSoLanThuSaiOtpAsync_TangDung1DonVi()
    {
        var kho = new NguoiDungGiaLap();
        var nguoiDung = new NguoiDung { SoLanThuSai = 2 };
        kho.DanhSach.Add(nguoiDung);

        await kho.TangSoLanThuSaiOtpAsync(nguoiDung.Id);

        Assert.Equal(3, nguoiDung.SoLanThuSai);
    }

    [Fact]
    public async Task DatLaiMatKhauAsync_CapNhatMatKhauVaXoaSachOtp()
    {
        var kho = new NguoiDungGiaLap();
        var nguoiDung = new NguoiDung
        {
            MatKhauBam = "cu", Salt = "salt-cu",
            MaOtpBam = "ma-bam", MaOtpHetHan = DateTime.UtcNow, MaOtpGuiLucNao = DateTime.UtcNow, SoLanThuSai = 4,
        };
        kho.DanhSach.Add(nguoiDung);

        await kho.DatLaiMatKhauAsync(nguoiDung.Id, "moi", "salt-moi");

        Assert.Equal("moi", nguoiDung.MatKhauBam);
        Assert.Equal("salt-moi", nguoiDung.Salt);
        Assert.Null(nguoiDung.MaOtpBam);
        Assert.Null(nguoiDung.MaOtpHetHan);
        Assert.Null(nguoiDung.MaOtpGuiLucNao);
        Assert.Equal(0, nguoiDung.SoLanThuSai);
    }
}
