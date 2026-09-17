using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using HaloChat.Security;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuTinNhanTests
{
    private const string IdNguoiGui = "507f1f77bcf86cd799439011";
    private const string IdNguoiNhan = "507f1f77bcf86cd799439012";

    private static (DichVuTinNhan DichVu, TinNhanGiaLap KhoTinNhan, NguoiDungGiaLap KhoNguoiDung, LoiMoiKetBanGiaLap KhoLoiMoiKetBan, NhomGiaLap KhoNhom, TinNhanAnGiaLap KhoTinNhanAn) TaoDichVu()
    {
        var khoTinNhan = new TinNhanGiaLap();
        var khoNguoiDung = new NguoiDungGiaLap();
        var khoLoiMoiKetBan = new LoiMoiKetBanGiaLap();
        var khoNhom = new NhomGiaLap();
        var khoDocNhom = new DocNhomGiaLap();
        var khoTinNhanAn = new TinNhanAnGiaLap();
        var quanLyKetNoi = new QuanLyKetNoiChat();
        var dichVu = new DichVuTinNhan(khoTinNhan, khoNguoiDung, khoLoiMoiKetBan, khoNhom, khoDocNhom, quanLyKetNoi, khoTinNhanAn, new DichVuMaHoa());
        return (dichVu, khoTinNhan, khoNguoiDung, khoLoiMoiKetBan, khoNhom, khoTinNhanAn);
    }

    // [GĐ6] Người dùng CÓ cặp khóa RSA thật — dùng khi test cần kiểm tra
    // đúng hành vi mã hóa/giải mã (khác với TaoNguoiNhanChoPhepNguoiLa(),
    // vốn cố tình để trống khóa để giữ các test khác không liên quan tới
    // mã hóa không bị ảnh hưởng — thiếu khóa ở bất kỳ bên nào thì
    // DichVuTinNhan tự rơi về lưu plaintext như trước GĐ6).
    private static NguoiDung TaoNguoiDungCoKhoaRsa(string id, string tenTaiKhoan)
    {
        var (khoaCongKhai, khoaBiMat) = new DichVuMaHoa().SinhCapKhoaRsa();
        return new NguoiDung
        {
            Id = id, TenTaiKhoan = tenTaiKhoan, ChoPhepTinNhanTuNguoiLa = true,
            KhoaCongKhai = khoaCongKhai, KhoaBiMat = khoaBiMat,
        };
    }

    // Hầu hết test dưới đây KHÔNG kiểm tra chính sách bạn bè (đã có nhóm test
    // riêng ở cuối file) — người nhận tạo với ChoPhepTinNhanTuNguoiLa = true
    // để bỏ qua yêu cầu bạn bè, giữ mỗi test tập trung đúng vào điều nó đặt tên.
    private static NguoiDung TaoNguoiNhanChoPhepNguoiLa() =>
        new() { Id = IdNguoiNhan, TenTaiKhoan = "NguoiNhan", ChoPhepTinNhanTuNguoiLa = true };

    [Fact]
    public async Task GuiTinNhanAsync_NguoiNhanKhongTonTai_NemNgoaiLe()
    {
        var (dichVu, _, _, _, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiNhanKhongTonTaiException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, "khong-ton-tai", null, "Text", "Xin chào", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NoiDungTextRong_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "   ", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiKhongHopLe_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "KhongTonTai", "Xin chào", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiAnhThieuDuongDanFile_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_DuongDanFileSaiDinhDang_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "../../../etc/passwd", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_DuongDanFileDungDinhDang_ThanhCong()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
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
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() => dichVu.GuiTinNhanAsync(
            IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/uploads/3f2a1b4c-5d6e-7f80-9a1b-2c3d4e5f6789.png", "anh.png", 1024, "image/png", null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NguoiNhanIdKhongPhaiObjectIdHopLe_NemNgoaiLe()
    {
        var (dichVu, _, _, _, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiNhanKhongTonTaiException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, "khong-phai-object-id", null, "Text", "Xin chào", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_HopLe_LuuVaTraVeTinNhan()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
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
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
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
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
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
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiNhan, TenTaiKhoan = "NguoiNhan", ChoPhepTinNhanTuNguoiLa = false });

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LaBanBe_ChoPhepGuiDuKhiNguoiNhanKhongChoPhepNguoiLa()
    {
        var (dichVu, _, khoNguoiDung, khoLoiMoiKetBan, _, _) = TaoDichVu();
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
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
    }

    [Fact]
    public async Task LayDanhSachHoiThoaiAsync_TraVeTinNhanCuoiVaSoChuaDoc()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
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
    public async Task LayDanhSachHoiThoaiAsync_TinCuoiDaThuHoi_XemTruocHienPlaceholder()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "Bi mat", null, null, null, null, null);
        await dichVu.ThuHoiAsync(IdNguoiNhan, tin.Id);

        var hoiThoai = await dichVu.LayDanhSachHoiThoaiAsync(IdNguoiGui);

        Assert.Equal("Tin nhắn đã được thu hồi.", Assert.Single(hoiThoai).TinNhanCuoi);
    }

    [Fact]
    public async Task LayDanhSachHoiThoaiAsync_KhongCoTinNhan_TraVeDanhSachRong()
    {
        var (dichVu, _, _, _, _, _) = TaoDichVu();

        var hoiThoai = await dichVu.LayDanhSachHoiThoaiAsync(IdNguoiGui);

        Assert.Empty(hoiThoai);
    }

    // --- Chuẩn hóa "" == null cho nguoiNhanId/nhomId (bugfix review) ---

    [Fact]
    public async Task GuiTinNhanAsync_NhomIdChuoiRong_VanGuiThanhCong1_1KhongNemNhomKhongTonTai()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
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
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
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
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Trả lời", null, null, null, null, "507f1f77bcf86cd799439099"));
    }

    [Fact]
    public async Task GuiTinNhanAsync_TraLoiTinThuocCuocTroChuyenKhac_NemTinNhanKhongHopLe()
    {
        const string IdNguoiThuBa = "507f1f77bcf86cd799439013";
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
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
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
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
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinAB = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin A gửi B", null, null, null, null, null);

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiGui, null, "Text", "Trả lời trong cuộc tự nhắn tin", null, null, null, null, tinAB.Id));
    }

    // --- Thu hồi / Ghim / Bỏ ghim / Ẩn cục bộ (GĐ6b) ---

    [Fact]
    public async Task ThuHoiAsync_LaNguoiGui_DatDaThuHoiVaAnNoiDung()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Bí mật", null, null, null, null, null);

        var ketQua = await dichVu.ThuHoiAsync(IdNguoiGui, tin.Id);

        Assert.True(ketQua.DaThuHoi);
        Assert.Equal("Tin nhắn đã được thu hồi.", ketQua.NoiDungTinNhan);
    }

    [Fact]
    public async Task ThuHoiAsync_KhongPhaiNguoiGui_NemKhongPhaiNguoiGui()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Bí mật", null, null, null, null, null);

        await Assert.ThrowsAsync<KhongPhaiNguoiGuiException>(() => dichVu.ThuHoiAsync(IdNguoiNhan, tin.Id));
    }

    [Fact]
    public async Task GhimAsync_ThanhVienHopLe_DatDaGhim()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Ghi nhớ việc này", null, null, null, null, null);

        var ketQua = await dichVu.GhimAsync(IdNguoiNhan, tin.Id);

        Assert.True(ketQua.DaGhim);
        Assert.NotNull(ketQua.ThoiGianGhim);
    }

    [Fact]
    public async Task GhimAsync_KhongThuocHoiThoai_NemKhongCoQuyen()
    {
        const string IdNguoiThuBa = "507f1f77bcf86cd799439013";
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Riêng tư", null, null, null, null, null);

        await Assert.ThrowsAsync<KhongCoQuyenTrenTinNhanException>(() => dichVu.GhimAsync(IdNguoiThuBa, tin.Id));
    }

    [Fact]
    public async Task BoGhimAsync_DatLaiDaGhimFalse()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tạm ghim", null, null, null, null, null);
        await dichVu.GhimAsync(IdNguoiGui, tin.Id);

        var ketQua = await dichVu.BoGhimAsync(IdNguoiNhan, tin.Id);

        Assert.False(ketQua.DaGhim);
        Assert.Null(ketQua.ThoiGianGhim);
    }

    [Fact]
    public async Task AnAsync_SauKhiAn_KhongConXuatHienTrongLichSuNguoiDoAn()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin ẩn được", null, null, null, null, null);

        await dichVu.AnAsync(IdNguoiNhan, tin.Id);
        var lichSuCuaNguoiAn = await dichVu.LayLichSuAsync(IdNguoiNhan, IdNguoiGui, null, 30);

        Assert.Empty(lichSuCuaNguoiAn);
    }

    [Fact]
    public async Task AnAsync_KhongAnhHuongLichSuCuaNguoiKhac()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin ẩn được", null, null, null, null, null);

        await dichVu.AnAsync(IdNguoiNhan, tin.Id);
        var lichSuCuaNguoiGui = await dichVu.LayLichSuAsync(IdNguoiGui, IdNguoiNhan, null, 30);

        Assert.Single(lichSuCuaNguoiGui);
    }

    // --- Thả cảm xúc (GĐ7c) ---

    [Fact]
    public async Task ThaCamXucAsync_ChuaTung_ThemMoi()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);

        var ketQua = await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Haha");

        var camXuc = Assert.Single(ketQua.DanhSachCamXuc);
        Assert.Equal(IdNguoiNhan, camXuc.NguoiDungId);
        Assert.Equal("Haha", camXuc.LoaiCamXuc);
    }

    [Fact]
    public async Task ThaCamXucAsync_DaCoCamXucKhac_ThayTheKhongCongDon()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);
        await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Thich");

        var ketQua = await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Wow");

        Assert.Single(ketQua.DanhSachCamXuc);
        Assert.Equal("Wow", ketQua.DanhSachCamXuc[0].LoaiCamXuc);
    }

    [Fact]
    public async Task ThaCamXucAsync_LoaiCamXucKhongHopLe_NemTinNhanKhongHopLe()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() => dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "KhongTonTai"));
    }

    [Fact]
    public async Task ThaCamXucAsync_LoaiCamXucLaChuoiSo_NemTinNhanKhongHopLe()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() => dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "99"));
    }

    [Fact]
    public async Task ThaCamXucAsync_KhongThuocHoiThoai_NemKhongCoQuyen()
    {
        const string IdNguoiThuBa = "507f1f77bcf86cd799439013";
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);

        await Assert.ThrowsAsync<KhongCoQuyenTrenTinNhanException>(() => dichVu.ThaCamXucAsync(IdNguoiThuBa, tin.Id, "Thich"));
    }

    [Fact]
    public async Task BoCamXucAsync_DaCo_XoaDung()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);
        await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Thich");

        var ketQua = await dichVu.BoCamXucAsync(IdNguoiNhan, tin.Id);

        Assert.Empty(ketQua.DanhSachCamXuc);
    }

    [Fact]
    public async Task LayLichSuAsync_TinDaThuHoiCoCamXucTruoc_AnDanhSachCamXuc()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);
        await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Thich");
        await dichVu.ThuHoiAsync(IdNguoiGui, tin.Id);

        var lichSu = await dichVu.LayLichSuAsync(IdNguoiNhan, IdNguoiGui, null, 30);

        Assert.Empty(Assert.Single(lichSu).DanhSachCamXuc);
    }

    [Fact]
    public async Task LayTinDaGhimTheoNguoiDungAsync_LocDungTinDaAn()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Ghim rồi ẩn", null, null, null, null, null);
        await dichVu.GhimAsync(IdNguoiGui, tin.Id);
        await dichVu.AnAsync(IdNguoiNhan, tin.Id);

        var ghimTheoNguoiAn = await dichVu.LayTinDaGhimTheoNguoiDungAsync(IdNguoiNhan, IdNguoiGui);
        var ghimTheoNguoiKia = await dichVu.LayTinDaGhimTheoNguoiDungAsync(IdNguoiGui, IdNguoiNhan);

        Assert.Empty(ghimTheoNguoiAn);
        Assert.Single(ghimTheoNguoiKia);
    }

    // --- Kho Media & Tệp (GĐ7a) ---

    [Fact]
    public async Task LayMediaTheoNguoiDungAsync_LocTinDaThuHoiVaDaAn()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, khoTinNhanAn) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinAnh1 = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/api/tinnhan/file/507f1f77bcf86cd799439001", "a.png", 1024, "image/png", null);
        var tinAnh2 = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/api/tinnhan/file/507f1f77bcf86cd799439002", "b.png", 1024, "image/png", null);
        await dichVu.ThuHoiAsync(IdNguoiGui, tinAnh1.Id);
        await khoTinNhanAn.AnAsync(IdNguoiNhan, tinAnh2.Id);

        var ketQua = await dichVu.LayMediaTheoNguoiDungAsync(IdNguoiNhan, IdNguoiGui);

        Assert.Empty(ketQua);
    }

    [Fact]
    public async Task LayMediaTheoNguoiDungAsync_TinHopLe_TraVeDung()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/api/tinnhan/file/507f1f77bcf86cd799439003", "a.png", 1024, "image/png", null);
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        var ketQua = await dichVu.LayMediaTheoNguoiDungAsync(IdNguoiGui, IdNguoiNhan);

        Assert.Single(ketQua);
    }

    [Fact]
    public async Task LayMediaTheoNhomAsync_KhongPhaiThanhVien_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _, khoNhom, _) = TaoDichVu();
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", ThanhVienIds = new List<string> { "thanh-vien-khac" } });

        await Assert.ThrowsAsync<KhongPhaiThanhVienNhomException>(() => dichVu.LayMediaTheoNhomAsync(IdNguoiGui, "n1"));
    }

    // --- Tìm tin nhắn (GĐ7b) ---

    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_TuKhoaRong_TraVeRong()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        var ketQua = await dichVu.TimKiemTheoNguoiDungAsync(IdNguoiGui, IdNguoiNhan, "   ");

        Assert.Empty(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_LoaiTruTinDaAn()
    {
        var (dichVu, _, khoNguoiDung, _, _, khoTinNhanAn) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Hẹn 5 giờ chiều", null, null, null, null, null);
        await khoTinNhanAn.AnAsync(IdNguoiNhan, tin.Id);

        var ketQua = await dichVu.TimKiemTheoNguoiDungAsync(IdNguoiNhan, IdNguoiGui, "hẹn");

        Assert.Empty(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_TinHopLe_TraVeDung()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Hẹn 5 giờ chiều", null, null, null, null, null);

        var ketQua = await dichVu.TimKiemTheoNguoiDungAsync(IdNguoiGui, IdNguoiNhan, "hẹn");

        Assert.Single(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNhomAsync_KhongPhaiThanhVien_NemNgoaiLe()
    {
        var (dichVu, _, _, _, khoNhom, _) = TaoDichVu();
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", ThanhVienIds = new List<string> { "thanh-vien-khac" } });

        await Assert.ThrowsAsync<KhongPhaiThanhVienNhomException>(() => dichVu.TimKiemTheoNhomAsync(IdNguoiGui, "n1", "hẹn"));
    }

    // --- Mã hóa lai RSA-AES (GĐ6) ---

    [Fact]
    public async Task GuiTinNhanAsync_CaHaiBenCoKhoaRsa_LuuMaHoaTrongKhoKhongLuuPlaintext()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Chào Bình, tối nay học mật mã nhé!", null, null, null, null, null);

        var tinNhanTrongKho = Assert.Single(khoTinNhan.DanhSach);
        Assert.Empty(tinNhanTrongKho.NoiDungTinNhan); // Không lưu plaintext.
        Assert.False(string.IsNullOrEmpty(tinNhanTrongKho.CiphertextTinNhan));
        Assert.False(string.IsNullOrEmpty(tinNhanTrongKho.Nonce));
        Assert.False(string.IsNullOrEmpty(tinNhanTrongKho.AuthTag));
        Assert.Equal(2, tinNhanTrongKho.DanhSachKhoaPhien.Count); // Cả người gửi lẫn người nhận đều đọc lại được.
        Assert.Contains(tinNhanTrongKho.DanhSachKhoaPhien, k => k.NguoiDungId == IdNguoiGui);
        Assert.Contains(tinNhanTrongKho.DanhSachKhoaPhien, k => k.NguoiDungId == IdNguoiNhan);
        // DTO trả về ngay lúc gửi vẫn phải là nội dung gốc (đã giải mã lại để hiển thị cho người gửi thấy).
        Assert.Equal("Chào Bình, tối nay học mật mã nhé!", ketQua.NoiDungTinNhan);
    }

    [Fact]
    public async Task GuiTinNhanAsync_NguoiGuiChuaCoKhoaRsa_RoiVePluTextNhuTruocGD6()
    {
        // NguoiGui không được thêm vào kho (giống hầu hết test khác trong
        // file này) — mô phỏng đúng tài khoản tạo trước GĐ6/chưa có khóa.
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        var tinNhanTrongKho = Assert.Single(khoTinNhan.DanhSach);
        Assert.Equal("Xin chào", tinNhanTrongKho.NoiDungTinNhan);
        Assert.Empty(tinNhanTrongKho.DanhSachKhoaPhien);
        Assert.Null(tinNhanTrongKho.CiphertextTinNhan);
        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
    }

    [Fact]
    public async Task LayLichSuAsync_TinDaMaHoa_NguoiNhanGiaiMaDungNoiDungGoc()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Nội dung bí mật", null, null, null, null, null);

        var lichSuNguoiNhan = await dichVu.LayLichSuAsync(IdNguoiNhan, IdNguoiGui, null, 30);
        var lichSuNguoiGui = await dichVu.LayLichSuAsync(IdNguoiGui, IdNguoiNhan, null, 30);

        Assert.Equal("Nội dung bí mật", Assert.Single(lichSuNguoiNhan).NoiDungTinNhan);
        Assert.Equal("Nội dung bí mật", Assert.Single(lichSuNguoiGui).NoiDungTinNhan);
    }

    [Fact]
    public async Task GuiTinNhanAsync_Nhom_TatCaThanhVienCoKhoa_MoiThanhVienGiaiMaDungNoiDung()
    {
        var (dichVu, _, khoNguoiDung, _, khoNhom, _) = TaoDichVu();
        const string IdThanhVien3 = "507f1f77bcf86cd799439013";
        const string IdNhom = "507f1f77bcf86cd799439099";
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdThanhVien3, "ThanhVien3"));
        khoNhom.DanhSach.Add(new Nhom { Id = IdNhom, ThanhVienIds = new List<string> { IdNguoiGui, IdNguoiNhan, IdThanhVien3 } });

        await dichVu.GuiTinNhanAsync(IdNguoiGui, null, IdNhom, "Text", "Họp nhóm 5 giờ chiều", null, null, null, null, null);

        var lichSuThanhVien3 = await dichVu.LayLichSuNhomAsync(IdThanhVien3, IdNhom, null, 30);
        Assert.Equal("Họp nhóm 5 giờ chiều", Assert.Single(lichSuThanhVien3).NoiDungTinNhan);
    }

    [Fact]
    public async Task GuiTinNhanAsync_TraLoiTinDaMaHoa_TrichDanHienDungNoiDungGocKhongPhaiCiphertext()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));
        var tinGoc = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Hẹn 5 giờ chiều", null, null, null, null, null);

        var tinTraLoi = await dichVu.GuiTinNhanAsync(IdNguoiNhan, IdNguoiGui, null, "Text", "OK", null, null, null, null, tinGoc.Id);

        Assert.Equal("Hẹn 5 giờ chiều", tinTraLoi.TraLoi!.NoiDungTomTat);
    }

    [Fact]
    public async Task LayDanhSachHoiThoaiAsync_TinDaMaHoa_XemTruocHienDungNoiDungGoc()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào bạn", null, null, null, null, null);

        var hoiThoaiNguoiNhan = await dichVu.LayDanhSachHoiThoaiAsync(IdNguoiNhan);

        Assert.Equal("Xin chào bạn", Assert.Single(hoiThoaiNguoiNhan).TinNhanCuoi);
    }

    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_TinDaMaHoa_VanTimThayNhoGiaiMaLaiDeLoc()
    {
        // [GĐ6] Regex trên NoiDungTinNhan (kho thật) không bao giờ khớp được
        // tin đã mã hóa vì NoiDungTinNhan lúc đó rỗng — DichVuTinNhan phải tự
        // giải mã các "ứng viên đã mã hóa" rồi lọc từ khóa ở tầng service.
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Hẹn 5 giờ chiều", null, null, null, null, null);
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Chào buổi sáng", null, null, null, null, null);

        var ketQua = await dichVu.TimKiemTheoNguoiDungAsync(IdNguoiNhan, IdNguoiGui, "hẹn");

        Assert.Equal("Hẹn 5 giờ chiều", Assert.Single(ketQua).NoiDungTinNhan);
    }

    [Fact]
    public async Task TimKiemTheoNhomAsync_TinDaMaHoa_VanTimThayNhoGiaiMaLaiDeLoc()
    {
        var (dichVu, _, khoNguoiDung, _, khoNhom, _) = TaoDichVu();
        const string IdNhom = "507f1f77bcf86cd799439099";
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));
        khoNhom.DanhSach.Add(new Nhom { Id = IdNhom, ThanhVienIds = new List<string> { IdNguoiGui, IdNguoiNhan } });
        await dichVu.GuiTinNhanAsync(IdNguoiGui, null, IdNhom, "Text", "Họp nhóm 5 giờ chiều", null, null, null, null, null);

        var ketQua = await dichVu.TimKiemTheoNhomAsync(IdNguoiNhan, IdNhom, "họp");

        Assert.Equal("Họp nhóm 5 giờ chiều", Assert.Single(ketQua).NoiDungTinNhan);
    }
}
