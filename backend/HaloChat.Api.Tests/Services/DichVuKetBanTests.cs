using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuKetBanTests
{
    private static (DichVuKetBan DichVu, LoiMoiKetBanGiaLap KhoLoiMoi, NguoiDungGiaLap KhoNguoiDung) TaoDichVu()
    {
        var khoLoiMoi = new LoiMoiKetBanGiaLap();
        var khoNguoiDung = new NguoiDungGiaLap();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "1", TenTaiKhoan = "NguoiA" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiB" });
        var dichVu = new DichVuKetBan(khoLoiMoi, khoNguoiDung);
        return (dichVu, khoLoiMoi, khoNguoiDung);
    }

    [Fact]
    public async Task GuiLoiMoiAsync_TuGuiChoChinhMinh_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<KhongTheTuKetBanException>(() => dichVu.GuiLoiMoiAsync("1", "1"));
    }

    [Fact]
    public async Task GuiLoiMoiAsync_NguoiNhanKhongTonTai_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiDuocMoiKhongTonTaiException>(() => dichVu.GuiLoiMoiAsync("1", "khong-ton-tai"));
    }

    [Fact]
    public async Task GuiLoiMoiAsync_DaTonTaiLoiMoiDangHoatDong_NemNgoaiLe()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { NguoiGuiId = "1", NguoiNhanId = "2" });

        await Assert.ThrowsAsync<LoiMoiKetBanDaTonTaiException>(() => dichVu.GuiLoiMoiAsync("1", "2"));
    }

    [Fact]
    public async Task GuiLoiMoiAsync_HopLe_TraVeDungThongTin()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();

        var ketQua = await dichVu.GuiLoiMoiAsync("1", "2");

        Assert.Equal("NguoiA", ketQua.NguoiGui.TenTaiKhoan);
        Assert.Equal("NguoiB", ketQua.NguoiNhan.TenTaiKhoan);
        Assert.Equal("ChoDuyet", ketQua.TrangThai);
        Assert.Single(khoLoiMoi.DanhSach);
    }

    [Fact]
    public async Task ChapNhanAsync_KhongPhaiNguoiNhan_NemNgoaiLe()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = "loimoi1", NguoiGuiId = "1", NguoiNhanId = "2" });

        await Assert.ThrowsAsync<KhongCoQuyenXuLyLoiMoiException>(() => dichVu.ChapNhanAsync("1", "loimoi1"));
    }

    [Fact]
    public async Task ChapNhanAsync_LoiMoiKhongTonTai_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<LoiMoiKetBanKhongTonTaiException>(() => dichVu.ChapNhanAsync("2", "khong-ton-tai"));
    }

    [Fact]
    public async Task ChapNhanAsync_DaXuLyTruocDo_NemNgoaiLe()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = "loimoi1", NguoiGuiId = "1", NguoiNhanId = "2", TrangThai = TrangThaiLoiMoiKetBan.DaTuChoi });

        await Assert.ThrowsAsync<LoiMoiKetBanDaXuLyException>(() => dichVu.ChapNhanAsync("2", "loimoi1"));
    }

    [Fact]
    public async Task ChapNhanAsync_HopLe_CapNhatTrangThaiDaChapNhan()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = "loimoi1", NguoiGuiId = "1", NguoiNhanId = "2" });

        var ketQua = await dichVu.ChapNhanAsync("2", "loimoi1");

        Assert.Equal("DaChapNhan", ketQua.TrangThai);
        Assert.Equal(TrangThaiLoiMoiKetBan.DaChapNhan, khoLoiMoi.DanhSach.Single().TrangThai);
    }

    [Fact]
    public async Task TuChoiAsync_HopLe_CapNhatTrangThaiDaTuChoi()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = "loimoi1", NguoiGuiId = "1", NguoiNhanId = "2" });

        await dichVu.TuChoiAsync("2", "loimoi1");

        Assert.Equal(TrangThaiLoiMoiKetBan.DaTuChoi, khoLoiMoi.DanhSach.Single().TrangThai);
    }

    [Fact]
    public async Task LayBanBeAsync_SauKhiChapNhan_TraVeDanhSachCoBanBe()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = "loimoi1", NguoiGuiId = "1", NguoiNhanId = "2", TrangThai = TrangThaiLoiMoiKetBan.DaChapNhan });

        var banBeCuaA = await dichVu.LayBanBeAsync("1");
        var banBeCuaB = await dichVu.LayBanBeAsync("2");

        Assert.Equal("NguoiB", Assert.Single(banBeCuaA).TenTaiKhoan);
        Assert.Equal("NguoiA", Assert.Single(banBeCuaB).TenTaiKhoan);
    }

    [Fact]
    public async Task LayLoiMoiDenAsync_TraVeDungLoiMoiChoDuyet()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { NguoiGuiId = "1", NguoiNhanId = "2" });

        var loiMoiDen = await dichVu.LayLoiMoiDenAsync("2");

        Assert.Single(loiMoiDen);
    }
}
