using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuTinNhanTests
{
    private const string IdNguoiGui = "507f1f77bcf86cd799439011";
    private const string IdNguoiNhan = "507f1f77bcf86cd799439012";

    private static (DichVuTinNhan DichVu, TinNhanGiaLap KhoTinNhan, NguoiDungGiaLap KhoNguoiDung, LoiMoiKetBanGiaLap KhoLoiMoiKetBan) TaoDichVu()
    {
        var khoTinNhan = new TinNhanGiaLap();
        var khoNguoiDung = new NguoiDungGiaLap();
        var khoLoiMoiKetBan = new LoiMoiKetBanGiaLap();
        var dichVu = new DichVuTinNhan(khoTinNhan, khoNguoiDung, khoLoiMoiKetBan);
        return (dichVu, khoTinNhan, khoNguoiDung, khoLoiMoiKetBan);
    }

    // Hầu hết test dưới đây KHÔNG kiểm tra chính sách bạn bè (đã có nhóm test
    // riêng ở cuối file) — người nhận tạo với ChoPhepTinNhanTuNguoiLa = true
    // để bỏ qua yêu cầu bạn bè, giữ mỗi test tập trung đúng vào điều nó đặt tên.
    private static NguoiDung TaoNguoiNhanChoPhepNguoiLa() =>
        new() { Id = IdNguoiNhan, TenTaiKhoan = "NguoiNhan", ChoPhepTinNhanTuNguoiLa = true };

    [Fact]
    public async Task GuiTinNhanAsync_NguoiNhanKhongTonTai_NemNgoaiLe()
    {
        var (dichVu, _, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiNhanKhongTonTaiException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, "khong-ton-tai", "Text", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NoiDungTextRong_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "Text", "   ", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiKhongHopLe_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "KhongTonTai", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiAnhThieuDuongDanFile_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "Anh", "", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_DuongDanFileSaiDinhDang_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "Anh", "", "../../../etc/passwd", null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_DuongDanFileDungDinhDang_ThanhCong()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(
            IdNguoiGui, IdNguoiNhan, "Anh", "", "/uploads/3f2a1b4c-5d6e-7f80-9a1b-2c3d4e5f6789.png", "anh.png", 1024, "image/png");

        Assert.Equal("/uploads/3f2a1b4c-5d6e-7f80-9a1b-2c3d4e5f6789.png", ketQua.DuongDanFile);
    }

    [Fact]
    public async Task GuiTinNhanAsync_NguoiNhanIdKhongPhaiObjectIdHopLe_NemNgoaiLe()
    {
        var (dichVu, _, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiNhanKhongTonTaiException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, "khong-phai-object-id", "Text", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_HopLe_LuuVaTraVeTinNhan()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "Text", "Xin chào", null, null, null, null);

        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
        Assert.Equal(IdNguoiGui, ketQua.NguoiGuiId);
        Assert.Equal(IdNguoiNhan, ketQua.NguoiNhanId);
        Assert.False(ketQua.DaDoc);
        var daLuu = Assert.Single(khoTinNhan.DanhSach);
        Assert.Equal(LoaiTinNhan.Text, daLuu.LoaiTinNhan);
    }

    [Fact]
    public async Task LayLichSuAsync_TraVeCaHaiChieuGuiVaNhan()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "Text", "Chào A gửi", null, null, null, null);
        await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, "Text", "Chào B gửi", null, null, null, null);

        var lichSu = await dichVu.LayLichSuAsync(IdNguoiGui, IdNguoiNhan, null, 30);

        Assert.Equal(2, lichSu.Count);
    }

    [Fact]
    public async Task DanhDauDaDocAsync_DanhDauTinNhanCuaNguoiGuiDaDoc()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiHienTai", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, "Text", "Chào", null, null, null, null);

        await dichVu.DanhDauDaDocAsync(IdNguoiGui, IdNguoiNhan);

        Assert.True(khoTinNhan.DanhSach.Single().DaDoc);
    }

    // --- Chính sách bạn bè (GĐ5b-1, spec §10.1) ---

    [Fact]
    public async Task GuiTinNhanAsync_KhongPhaiBanBeVaNguoiNhanKhongChoPhepNguoiLa_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiNhan, TenTaiKhoan = "NguoiNhan", ChoPhepTinNhanTuNguoiLa = false });

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "Text", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LaBanBe_ChoPhepGuiDuKhiNguoiNhanKhongChoPhepNguoiLa()
    {
        var (dichVu, _, khoNguoiDung, khoLoiMoiKetBan) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiNhan, TenTaiKhoan = "NguoiNhan", ChoPhepTinNhanTuNguoiLa = false });
        khoLoiMoiKetBan.DanhSach.Add(new LoiMoiKetBan
        {
            NguoiGuiId = IdNguoiGui,
            NguoiNhanId = IdNguoiNhan,
            TrangThai = TrangThaiLoiMoiKetBan.DaChapNhan,
        });

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "Text", "Xin chào", null, null, null, null);

        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
    }

    [Fact]
    public async Task GuiTinNhanAsync_KhongPhaiBanBeNhungNguoiNhanChoPhepNguoiLa_ThanhCong()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "Text", "Xin chào", null, null, null, null);

        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
    }

    [Fact]
    public async Task LayDanhSachHoiThoaiAsync_TraVeTinNhanCuoiVaSoChuaDoc()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, "Text", "Tin đầu", null, null, null, null);
        await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, "Text", "Tin cuối", null, null, null, null);

        var hoiThoai = await dichVu.LayDanhSachHoiThoaiAsync(IdNguoiGui);

        var duyNhat = Assert.Single(hoiThoai);
        Assert.Equal(IdNguoiNhan, duyNhat.NguoiDung.Id);
        Assert.Equal("Tin cuối", duyNhat.TinNhanCuoi);
        Assert.Equal(2, duyNhat.SoTinChuaDoc);
    }

    [Fact]
    public async Task LayDanhSachHoiThoaiAsync_KhongCoTinNhan_TraVeDanhSachRong()
    {
        var (dichVu, _, _, _) = TaoDichVu();

        var hoiThoai = await dichVu.LayDanhSachHoiThoaiAsync(IdNguoiGui);

        Assert.Empty(hoiThoai);
    }
}
