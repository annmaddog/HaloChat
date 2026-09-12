using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuKetBanTests
{
    private const string IdA = "507f1f77bcf86cd799439001";
    private const string IdB = "507f1f77bcf86cd799439002";
    private const string IdLoiMoi1 = "507f1f77bcf86cd799439003";

    private static (DichVuKetBan DichVu, LoiMoiKetBanGiaLap KhoLoiMoi, NguoiDungGiaLap KhoNguoiDung) TaoDichVu()
    {
        var khoLoiMoi = new LoiMoiKetBanGiaLap();
        var khoNguoiDung = new NguoiDungGiaLap();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdA, TenTaiKhoan = "NguoiA" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdB, TenTaiKhoan = "NguoiB" });
        var dichVu = new DichVuKetBan(khoLoiMoi, khoNguoiDung);
        return (dichVu, khoLoiMoi, khoNguoiDung);
    }

    [Fact]
    public async Task GuiLoiMoiAsync_TuGuiChoChinhMinh_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<KhongTheTuKetBanException>(() => dichVu.GuiLoiMoiAsync(IdA, IdA));
    }

    [Fact]
    public async Task GuiLoiMoiAsync_NguoiNhanKhongTonTai_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiDuocMoiKhongTonTaiException>(() => dichVu.GuiLoiMoiAsync(IdA, "507f1f77bcf86cd799439099"));
    }

    [Fact]
    public async Task GuiLoiMoiAsync_NguoiNhanIdKhongPhaiObjectIdHopLe_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiDuocMoiKhongTonTaiException>(() => dichVu.GuiLoiMoiAsync(IdA, "khong-phai-object-id"));
    }

    [Fact]
    public async Task GuiLoiMoiAsync_DaTonTaiLoiMoiDangHoatDong_NemNgoaiLe()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { NguoiGuiId = IdA, NguoiNhanId = IdB });

        await Assert.ThrowsAsync<LoiMoiKetBanDaTonTaiException>(() => dichVu.GuiLoiMoiAsync(IdA, IdB));
    }

    [Fact]
    public async Task GuiLoiMoiAsync_HopLe_TraVeDungThongTin()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();

        var ketQua = await dichVu.GuiLoiMoiAsync(IdA, IdB);

        Assert.Equal("NguoiA", ketQua.NguoiGui.TenTaiKhoan);
        Assert.Equal("NguoiB", ketQua.NguoiNhan.TenTaiKhoan);
        Assert.Equal("ChoDuyet", ketQua.TrangThai);
        Assert.Single(khoLoiMoi.DanhSach);
    }

    [Fact]
    public async Task ChapNhanAsync_KhongPhaiNguoiNhan_NemNgoaiLe()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = IdLoiMoi1, NguoiGuiId = IdA, NguoiNhanId = IdB });

        await Assert.ThrowsAsync<KhongCoQuyenXuLyLoiMoiException>(() => dichVu.ChapNhanAsync(IdA, IdLoiMoi1));
    }

    [Fact]
    public async Task ChapNhanAsync_LoiMoiKhongTonTai_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<LoiMoiKetBanKhongTonTaiException>(() => dichVu.ChapNhanAsync(IdB, "507f1f77bcf86cd799439099"));
    }

    [Fact]
    public async Task ChapNhanAsync_IdLoiMoiKhongPhaiObjectIdHopLe_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<LoiMoiKetBanKhongTonTaiException>(() => dichVu.ChapNhanAsync(IdB, "khong-phai-object-id"));
    }

    [Fact]
    public async Task ChapNhanAsync_DaXuLyTruocDo_NemNgoaiLe()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = IdLoiMoi1, NguoiGuiId = IdA, NguoiNhanId = IdB, TrangThai = TrangThaiLoiMoiKetBan.DaTuChoi });

        await Assert.ThrowsAsync<LoiMoiKetBanDaXuLyException>(() => dichVu.ChapNhanAsync(IdB, IdLoiMoi1));
    }

    [Fact]
    public async Task ChapNhanAsync_HopLe_CapNhatTrangThaiDaChapNhan()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = IdLoiMoi1, NguoiGuiId = IdA, NguoiNhanId = IdB });

        var ketQua = await dichVu.ChapNhanAsync(IdB, IdLoiMoi1);

        Assert.Equal("DaChapNhan", ketQua.TrangThai);
        Assert.Equal(TrangThaiLoiMoiKetBan.DaChapNhan, khoLoiMoi.DanhSach.Single().TrangThai);
    }

    [Fact]
    public async Task TuChoiAsync_HopLe_CapNhatTrangThaiDaTuChoi()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = IdLoiMoi1, NguoiGuiId = IdA, NguoiNhanId = IdB });

        await dichVu.TuChoiAsync(IdB, IdLoiMoi1);

        Assert.Equal(TrangThaiLoiMoiKetBan.DaTuChoi, khoLoiMoi.DanhSach.Single().TrangThai);
    }

    [Fact]
    public async Task LayBanBeAsync_SauKhiChapNhan_TraVeDanhSachCoBanBe()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = IdLoiMoi1, NguoiGuiId = IdA, NguoiNhanId = IdB, TrangThai = TrangThaiLoiMoiKetBan.DaChapNhan });

        var banBeCuaA = await dichVu.LayBanBeAsync(IdA);
        var banBeCuaB = await dichVu.LayBanBeAsync(IdB);

        Assert.Equal("NguoiB", Assert.Single(banBeCuaA).TenTaiKhoan);
        Assert.Equal("NguoiA", Assert.Single(banBeCuaB).TenTaiKhoan);
    }

    [Fact]
    public async Task LayLoiMoiDenAsync_TraVeDungLoiMoiChoDuyet()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { NguoiGuiId = IdA, NguoiNhanId = IdB });

        var loiMoiDen = await dichVu.LayLoiMoiDenAsync(IdB);

        Assert.Single(loiMoiDen);
    }
}
