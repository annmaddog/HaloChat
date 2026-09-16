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
        var khoNhom = new NhomGiaLap();
        var khoDocNhom = new DocNhomGiaLap();
        var quanLyKetNoi = new QuanLyKetNoiChat();
        var dichVu = new DichVuTinNhan(khoTinNhan, khoNguoiDung, khoLoiMoiKetBan, khoNhom, khoDocNhom, quanLyKetNoi);
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
            dichVu.GuiTinNhanAsync(IdNguoiGui, "khong-ton-tai", null, "Text", "Xin chào", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NoiDungTextRong_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "   ", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiKhongHopLe_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "KhongTonTai", "Xin chào", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiAnhThieuDuongDanFile_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_DuongDanFileSaiDinhDang_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "../../../etc/passwd", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_DuongDanFileDungDinhDang_ThanhCong()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(
            IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/api/tinnhan/file/507f1f77bcf86cd799439099", "anh.png", 1024, "image/png", null);

        Assert.Equal("/api/tinnhan/file/507f1f77bcf86cd799439099", ketQua.DuongDanFile);
    }

    [Fact]
    public async Task GuiTinNhanAsync_DuongDanFileKieuCuUploadsKhongConHopLe_NemNgoaiLe()
    {
        // [Sửa lỗi] File giờ lưu qua GridFS, trả về đường dẫn dạng
        // /api/tinnhan/file/<id> — định dạng /uploads/<guid>.<ext> cũ (ổ đĩa
        // container, đã bỏ vì Render xóa sạch mỗi lần deploy) không còn hợp lệ.
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() => dichVu.GuiTinNhanAsync(
            IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/uploads/3f2a1b4c-5d6e-7f80-9a1b-2c3d4e5f6789.png", "anh.png", 1024, "image/png", null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NguoiNhanIdKhongPhaiObjectIdHopLe_NemNgoaiLe()
    {
        var (dichVu, _, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiNhanKhongTonTaiException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, "khong-phai-object-id", null, "Text", "Xin chào", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_HopLe_LuuVaTraVeTinNhan()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

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
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Chào A gửi", null, null, null, null, null);
        await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "Chào B gửi", null, null, null, null, null);

        var lichSu = await dichVu.LayLichSuAsync(IdNguoiGui, IdNguoiNhan, null, 30);

        Assert.Equal(2, lichSu.Count);
    }

    [Fact]
    public async Task DanhDauDaDocAsync_DanhDauTinNhanCuaNguoiGuiDaDoc()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiHienTai", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "Chào", null, null, null, null, null);

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
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null));
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

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
    }

    [Fact]
    public async Task GuiTinNhanAsync_KhongPhaiBanBeNhungNguoiNhanChoPhepNguoiLa_ThanhCong()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
    }

    [Fact]
    public async Task LayDanhSachHoiThoaiAsync_TraVeTinNhanCuoiVaSoChuaDoc()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "Tin đầu", null, null, null, null, null);
        await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "Tin cuối", null, null, null, null, null);

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

    // --- Chuẩn hóa "" == null cho nguoiNhanId/nhomId (bugfix review) ---

    [Fact]
    public async Task GuiTinNhanAsync_NhomIdChuoiRong_VanGuiThanhCong1_1KhongNemNhomKhongTonTai()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, "", "Text", "Xin chào", null, null, null, null, null);

        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
        Assert.Equal(IdNguoiNhan, ketQua.NguoiNhanId);
        Assert.Null(ketQua.NhomId);
        Assert.Single(khoTinNhan.DanhSach);
    }

    // --- Trả lời tin nhắn (GĐ6a) ---

    [Fact]
    public async Task GuiTinNhanAsync_CoTraLoiHopLe_LuuSnapshotDungThongTin()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinA = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin gốc", null, null, null, null, null);

        var tinB = await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "Tin trả lời", null, null, null, null, tinA.Id);

        Assert.NotNull(tinB.TraLoi);
        Assert.Equal(tinA.Id, tinB.TraLoi!.Id);
        Assert.Equal("NguoiGui", tinB.TraLoi.TenNguoiGui);
        Assert.Equal("Tin gốc", tinB.TraLoi.NoiDungTomTat);
        Assert.Equal("Text", tinB.TraLoi.LoaiTinNhan);
    }

    [Fact]
    public async Task GuiTinNhanAsync_TraLoiTinKhongTonTai_NemTinNhanKhongHopLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Trả lời", null, null, null, null, "507f1f77bcf86cd799439099"));
    }

    [Fact]
    public async Task GuiTinNhanAsync_TraLoiTinThuocCuocTroChuyenKhac_NemTinNhanKhongHopLe()
    {
        const string IdNguoiThuBa = "507f1f77bcf86cd799439013";
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiThuBa, TenTaiKhoan = "NguoiThuBa", ChoPhepTinNhanTuNguoiLa = true });
        var tinGiuaGuiVaNhan = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin giữa A-B", null, null, null, null, null);

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiThuBa, null, "Text", "Trả lời sai cuộc trò chuyện", null, null, null, null, tinGiuaGuiVaNhan.Id));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NoiDungTextDaiHon80KyTu_RutGonConDauBaChamCuoi()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var noiDungDai = new string('a', 100);
        var tinA = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", noiDungDai, null, null, null, null, null);

        var tinB = await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "Trả lời", null, null, null, null, tinA.Id);

        Assert.Equal(new string('a', 80) + "…", tinB.TraLoi!.NoiDungTomTat);
    }

    [Fact]
    public async Task GuiTinNhanAsync_TraLoiTinCuaCuocTroChuyenKhacTrongCuocTuNhanTin_NemTinNhanKhongHopLe()
    {
        var (dichVu, _, khoNguoiDung, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinAB = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin A gửi B", null, null, null, null, null);

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiGui, null, "Text", "Trả lời trong cuộc tự nhắn tin", null, null, null, null, tinAB.Id));
    }
}
