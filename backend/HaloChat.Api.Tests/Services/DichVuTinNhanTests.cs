using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuTinNhanTests
{
    private static (DichVuTinNhan DichVu, TinNhanGiaLap KhoTinNhan, NguoiDungGiaLap KhoNguoiDung) TaoDichVu()
    {
        var khoTinNhan = new TinNhanGiaLap();
        var khoNguoiDung = new NguoiDungGiaLap();
        var dichVu = new DichVuTinNhan(khoTinNhan, khoNguoiDung);
        return (dichVu, khoTinNhan, khoNguoiDung);
    }

    [Fact]
    public async Task GuiTinNhanAsync_NguoiNhanKhongTonTai_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiNhanKhongTonTaiException>(() =>
            dichVu.GuiTinNhanAsync("1", "khong-ton-tai", "Text", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NoiDungTextRong_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync("1", "2", "Text", "   ", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiKhongHopLe_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync("1", "2", "KhongTonTai", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiAnhThieuDuongDanFile_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync("1", "2", "Anh", "", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_HopLe_LuuVaTraVeTinNhan()
    {
        var (dichVu, khoTinNhan, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });

        var ketQua = await dichVu.GuiTinNhanAsync("1", "2", "Text", "Xin chào", null, null, null, null);

        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
        Assert.Equal("1", ketQua.NguoiGuiId);
        Assert.Equal("2", ketQua.NguoiNhanId);
        Assert.False(ketQua.DaDoc);
        var daLuu = Assert.Single(khoTinNhan.DanhSach);
        Assert.Equal(LoaiTinNhan.Text, daLuu.LoaiTinNhan);
    }

    [Fact]
    public async Task LayLichSuAsync_TraVeCaHaiChieuGuiVaNhan()
    {
        var (dichVu, _, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "1", TenTaiKhoan = "NguoiGui" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });
        await dichVu.GuiTinNhanAsync("1", "2", "Text", "Chào A gửi", null, null, null, null);
        await dichVu.GuiTinNhanAsync("2", "1", "Text", "Chào B gửi", null, null, null, null);

        var lichSu = await dichVu.LayLichSuAsync("1", "2", null, 30);

        Assert.Equal(2, lichSu.Count);
    }

    [Fact]
    public async Task DanhDauDaDocAsync_DanhDauTinNhanCuaNguoiGuiDaDoc()
    {
        var (dichVu, khoTinNhan, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "1", TenTaiKhoan = "NguoiHienTai" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });
        await dichVu.GuiTinNhanAsync("2", "1", "Text", "Chào", null, null, null, null);

        await dichVu.DanhDauDaDocAsync("1", "2");

        Assert.True(khoTinNhan.DanhSach.Single().DaDoc);
    }
}
