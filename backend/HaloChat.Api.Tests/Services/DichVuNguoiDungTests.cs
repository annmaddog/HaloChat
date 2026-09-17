using HaloChat.Api.Models;
using HaloChat.Api.Options;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using HaloChat.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuNguoiDungTests
{
    private static (DichVuNguoiDung DichVu, NguoiDungGiaLap Kho, DichVuEmailGiaLap Email) TaoDichVu()
    {
        var kho = new NguoiDungGiaLap();
        var email = new DichVuEmailGiaLap();
        var dichVuJwt = new DichVuJwt(Microsoft.Extensions.Options.Options.Create(new TuyChonJwt
        {
            ChuoiBiMat = "khoa-bi-mat-du-dai-danh-cho-kiem-thu-toi-thieu-32-ky-tu",
        }));
        var dichVu = new DichVuNguoiDung(
            kho, new DichVuMatKhau(), dichVuJwt, email, new DichVuMaHoa(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DichVuNguoiDung>.Instance);
        return (dichVu, kho, email);
    }

    [Fact]
    public async Task DangKyTaiKhoan_ThanhCong_SinhCapKhoaRsaChoTaiKhoanMoi()
    {
        var (dichVu, kho, _) = TaoDichVu();

        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var nguoiDungMoi = Assert.Single(kho.DanhSach);
        Assert.False(string.IsNullOrWhiteSpace(nguoiDungMoi.KhoaCongKhai));
        Assert.False(string.IsNullOrWhiteSpace(nguoiDungMoi.KhoaBiMat));
    }

    [Fact]
    public async Task DangKyTaiKhoan_TenTaiKhoanDaTonTai_TraVeThatBai()
    {
        var (dichVu, kho, _) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "NguyenAn", Email = "khac@gmail.com" });

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_EmailDaTonTai_TraVeThatBai()
    {
        var (dichVu, kho, _) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "Khac", Email = "nguyenan@gmail.com" });

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_TenTaiKhoanTrungEmailNguoiKhac_TraVeThatBai()
    {
        var (dichVu, kho, _) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "TranBinh", Email = "trung@gmail.com" });

        // Đăng ký với TenTaiKhoan trùng Email của người đã tồn tại (va chạm chéo trường).
        var ketQua = await dichVu.DangKyTaiKhoan("trung@gmail.com", "moi@gmail.com", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_EmailTrungTenTaiKhoanNguoiKhac_TraVeThatBai()
    {
        var (dichVu, kho, _) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "TranBinh", Email = "khac@gmail.com" });

        // Đăng ký với Email trùng TenTaiKhoan của người đã tồn tại (va chạm chéo trường).
        var ketQua = await dichVu.DangKyTaiKhoan("NguoiMoi", "TranBinh", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_HopLe_LuuMatKhauDaBamKhongLuuBanRo()
    {
        var (dichVu, kho, _) = TaoDichVu();

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
        var (dichVu, _, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var token = await dichVu.DangNhap("NguyenAn", "SaiMatKhau");

        Assert.Null(token);
    }

    [Fact]
    public async Task DangNhap_TaiKhoanKhongTonTai_TraVeNull()
    {
        var (dichVu, _, _) = TaoDichVu();

        var token = await dichVu.DangNhap("KhongTonTai", "MatKhau123");

        Assert.Null(token);
    }

    [Fact]
    public async Task DangNhap_DungMatKhauBangTenTaiKhoan_TraVeToken()
    {
        var (dichVu, _, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var token = await dichVu.DangNhap("NguyenAn", "MatKhau123");

        Assert.NotNull(token);
    }

    [Fact]
    public async Task DangNhap_DungMatKhauBangEmail_TraVeToken()
    {
        var (dichVu, _, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var token = await dichVu.DangNhap("nguyenan@gmail.com", "MatKhau123");

        Assert.NotNull(token);
    }

    [Fact]
    public async Task DangNhap_TaiKhoanTaoTruocGD6ThieuKhoaRsa_TuDongCapKhoaBuTru()
    {
        // Mô phỏng tài khoản đã tồn tại từ trước khi có tính năng mã hóa
        // GĐ6 (hoặc dữ liệu cũ trên Mongo thật) — KhoaCongKhai/KhoaBiMat
        // rỗng. Đăng nhập phải tự vá bằng cách sinh cặp khóa mới, để các
        // tin nhắn gửi SAU lần đăng nhập này được mã hóa bình thường.
        var (dichVu, kho, _) = TaoDichVu();
        var matKhau = new DichVuMatKhau();
        var salt = matKhau.TaoSalt();
        kho.DanhSach.Add(new NguoiDung
        {
            TenTaiKhoan = "NguoiCu", Email = "nguoicu@gmail.com", Salt = salt,
            MatKhauBam = matKhau.BamMatKhau("MatKhau123", salt),
        });

        var token = await dichVu.DangNhap("NguoiCu", "MatKhau123");

        Assert.NotNull(token);
        var nguoiDung = Assert.Single(kho.DanhSach);
        Assert.False(string.IsNullOrWhiteSpace(nguoiDung.KhoaCongKhai));
        Assert.False(string.IsNullOrWhiteSpace(nguoiDung.KhoaBiMat));
    }

    [Fact]
    public async Task DangNhap_TaiKhoanDaCoKhoaRsa_KhongSinhKhoaMoiGhiDe()
    {
        var (dichVu, kho, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");
        var khoaCongKhaiTruoc = kho.DanhSach[0].KhoaCongKhai;

        await dichVu.DangNhap("NguyenAn", "MatKhau123");

        Assert.Equal(khoaCongKhaiTruoc, kho.DanhSach[0].KhoaCongKhai);
    }

    [Fact]
    public async Task LayDanhSachNguoiDung_KhongBaoGomChinhMinh()
    {
        var (dichVu, kho, _) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { Id = "1", TenTaiKhoan = "NguyenAn", Email = "a@gmail.com" });
        kho.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "TranBinh", Email = "b@gmail.com" });

        var danhSach = await dichVu.LayDanhSachNguoiDung("1");

        var duyNhat = Assert.Single(danhSach);
        Assert.Equal("TranBinh", duyNhat.TenTaiKhoan);
    }

    [Fact]
    public async Task CapNhatCaiDatAsync_CapNhatDungTruong()
    {
        var (dichVu, kho, _) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { Id = "1", TenTaiKhoan = "NguoiA" });

        await dichVu.CapNhatCaiDatAsync("1", true, false, false, false, true, false);

        var daLuu = kho.DanhSach.Single();
        Assert.True(daLuu.ChoPhepTinNhanTuNguoiLa);
        Assert.False(daLuu.HienThiTrangThaiHoatDong);
        Assert.False(daLuu.ChoPhepThemVaoNhom);
        Assert.False(daLuu.ThongBaoTinNhanMoi);
        Assert.True(daLuu.ThongBaoLoiMoiKetBan);
        Assert.False(daLuu.ThongBaoNhom);
    }

    [Fact]
    public async Task LayThongTinCaNhanAsync_TraVeDungThongTin()
    {
        var (dichVu, kho, _) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { Id = "1", TenTaiKhoan = "NguoiA", Email = "a@gmail.com", ChoPhepTinNhanTuNguoiLa = true });

        var hoSo = await dichVu.LayThongTinCaNhanAsync("1");

        Assert.NotNull(hoSo);
        Assert.Equal("NguoiA", hoSo!.TenTaiKhoan);
        Assert.True(hoSo.ChoPhepTinNhanTuNguoiLa);
    }

    [Fact]
    public async Task LayThongTinCaNhanAsync_KhongTonTai_TraVeNull()
    {
        var (dichVu, _, _) = TaoDichVu();

        var hoSo = await dichVu.LayThongTinCaNhanAsync("khong-ton-tai");

        Assert.Null(hoSo);
    }

    [Fact]
    public async Task YeuCauOtp_EmailTonTai_GuiEmailVaLuuOtp()
    {
        var (dichVu, kho, email) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "otpuser1", Email = "otpuser1@gmail.com", Salt = "salt" };
        kho.DanhSach.Add(nguoiDung);

        await dichVu.YeuCauOtpDatLaiMatKhauAsync("otpuser1@gmail.com");

        Assert.NotNull(nguoiDung.MaOtpBam);
        Assert.NotNull(nguoiDung.MaOtpHetHan);
        Assert.Single(email.DaGui);
        Assert.Equal("otpuser1@gmail.com", email.DaGui[0].DiaChiNhan);
        Assert.Matches(@"^\d{6}$", email.DaGui[0].MaOtp);
    }

    [Fact]
    public async Task YeuCauOtp_EmailKhongTonTai_KhongGuiEmailKhongNemLoi()
    {
        var (dichVu, _, email) = TaoDichVu();

        await dichVu.YeuCauOtpDatLaiMatKhauAsync("khong-ton-tai@gmail.com");

        Assert.Empty(email.DaGui);
    }

    [Fact]
    public async Task YeuCauOtp_ConTrongCooldown_KhongGuiLaiEmail()
    {
        var (dichVu, kho, email) = TaoDichVu();
        var nguoiDung = new NguoiDung
        {
            TenTaiKhoan = "otpuser2", Email = "otpuser2@gmail.com", Salt = "salt",
            MaOtpGuiLucNao = DateTime.UtcNow,
        };
        kho.DanhSach.Add(nguoiDung);

        await dichVu.YeuCauOtpDatLaiMatKhauAsync("otpuser2@gmail.com");

        Assert.Empty(email.DaGui);
    }

    [Fact]
    public async Task YeuCauOtp_EmailNemLoi_KhongNemLoiVaXoaThoiGianGuiOtp()
    {
        var (dichVu, kho, email) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "otpuser7", Email = "otpuser7@gmail.com", Salt = "salt" };
        kho.DanhSach.Add(nguoiDung);
        email.NemLoiLanKeTiep = true;

        var ngoaiLe = await Record.ExceptionAsync(() => dichVu.YeuCauOtpDatLaiMatKhauAsync("otpuser7@gmail.com"));

        Assert.Null(ngoaiLe);
        Assert.Null(nguoiDung.MaOtpGuiLucNao);
    }

    [Fact]
    public async Task DatLaiMatKhau_OtpDungConHieuLuc_DoiMatKhauThanhCong()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "otpuser3", Email = "otpuser3@gmail.com", Salt = "salt" };
        kho.DanhSach.Add(nguoiDung);
        var matKhau = new DichVuMatKhau();
        nguoiDung.MaOtpBam = matKhau.BamMatKhau("123456", nguoiDung.Salt);
        nguoiDung.MaOtpHetHan = DateTime.UtcNow.AddMinutes(5);

        var ketQua = await dichVu.DatLaiMatKhauAsync("otpuser3@gmail.com", "123456", "MatKhauMoi123");

        Assert.True(ketQua.ThanhCong);
        Assert.True(matKhau.KiemTraMatKhau("MatKhauMoi123", nguoiDung.Salt, nguoiDung.MatKhauBam));
        Assert.Null(nguoiDung.MaOtpBam);
    }

    [Fact]
    public async Task DatLaiMatKhau_OtpSai_TangSoLanThuSaiVaTraVeThatBai()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "otpuser4", Email = "otpuser4@gmail.com", Salt = "salt" };
        kho.DanhSach.Add(nguoiDung);
        var matKhau = new DichVuMatKhau();
        nguoiDung.MaOtpBam = matKhau.BamMatKhau("123456", nguoiDung.Salt);
        nguoiDung.MaOtpHetHan = DateTime.UtcNow.AddMinutes(5);

        var ketQua = await dichVu.DatLaiMatKhauAsync("otpuser4@gmail.com", "000000", "MatKhauMoi123");

        Assert.False(ketQua.ThanhCong);
        Assert.Equal("Mã OTP không đúng.", ketQua.ThongBao);
        Assert.Equal(1, nguoiDung.SoLanThuSai);
    }

    [Fact]
    public async Task DatLaiMatKhau_OtpDaHetHan_TraVeThatBaiKhongTangSoLanThuSai()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "otpuser5", Email = "otpuser5@gmail.com", Salt = "salt" };
        kho.DanhSach.Add(nguoiDung);
        var matKhau = new DichVuMatKhau();
        nguoiDung.MaOtpBam = matKhau.BamMatKhau("123456", nguoiDung.Salt);
        nguoiDung.MaOtpHetHan = DateTime.UtcNow.AddMinutes(-1); // đã hết hạn 1 phút trước

        var ketQua = await dichVu.DatLaiMatKhauAsync("otpuser5@gmail.com", "123456", "MatKhauMoi123");

        Assert.False(ketQua.ThanhCong);
        Assert.Equal("Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng gửi lại mã mới.", ketQua.ThongBao);
        Assert.Equal(0, nguoiDung.SoLanThuSai);
    }

    [Fact]
    public async Task DatLaiMatKhau_QuaSoLanSaiToiDa_TuChoiDuKhiOtpDung()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung
        {
            TenTaiKhoan = "otpuser6", Email = "otpuser6@gmail.com", Salt = "salt", SoLanThuSai = 5,
        };
        kho.DanhSach.Add(nguoiDung);
        var matKhau = new DichVuMatKhau();
        nguoiDung.MaOtpBam = matKhau.BamMatKhau("123456", nguoiDung.Salt);
        nguoiDung.MaOtpHetHan = DateTime.UtcNow.AddMinutes(5);

        var ketQua = await dichVu.DatLaiMatKhauAsync("otpuser6@gmail.com", "123456", "MatKhauMoi123");

        Assert.False(ketQua.ThanhCong);
        Assert.Equal("Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng gửi lại mã mới.", ketQua.ThongBao);
    }

    [Fact]
    public async Task DoiMatKhau_DungMatKhauCu_DoiThanhCong()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var matKhau = new DichVuMatKhau();
        var salt = matKhau.TaoSalt();
        var nguoiDung = new NguoiDung
        {
            Id = "1", TenTaiKhoan = "NguoiA", Salt = salt, MatKhauBam = matKhau.BamMatKhau("MatKhauCu123", salt),
        };
        kho.DanhSach.Add(nguoiDung);

        var ketQua = await dichVu.DoiMatKhauAsync("1", "MatKhauCu123", "MatKhauMoi456");

        Assert.True(ketQua.ThanhCong);
        Assert.True(matKhau.KiemTraMatKhau("MatKhauMoi456", nguoiDung.Salt, nguoiDung.MatKhauBam));
    }

    [Fact]
    public async Task DoiMatKhau_SaiMatKhauCu_TraVeThatBaiKhongDoiGiMatKhau()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var matKhau = new DichVuMatKhau();
        var salt = matKhau.TaoSalt();
        var matKhauBamGoc = matKhau.BamMatKhau("MatKhauCu123", salt);
        var nguoiDung = new NguoiDung { Id = "1", TenTaiKhoan = "NguoiA", Salt = salt, MatKhauBam = matKhauBamGoc };
        kho.DanhSach.Add(nguoiDung);

        var ketQua = await dichVu.DoiMatKhauAsync("1", "SaiMatKhau", "MatKhauMoi456");

        Assert.False(ketQua.ThanhCong);
        Assert.Equal(matKhauBamGoc, nguoiDung.MatKhauBam);
    }

    [Fact]
    public async Task DoiMatKhau_NguoiDungKhongTonTai_TraVeThatBai()
    {
        var (dichVu, _, _) = TaoDichVu();

        var ketQua = await dichVu.DoiMatKhauAsync("khong-ton-tai", "MatKhauCu123", "MatKhauMoi456");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DoiAnhDaiDienAsync_ThanhCong_CapNhatDungField()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "AnhDaiDien1", Email = "anhdaidien1@gmail.com" };
        kho.DanhSach.Add(nguoiDung);

        var hoSo = await dichVu.DoiAnhDaiDienAsync(nguoiDung.Id, "/api/tinnhan/file/abc123");

        Assert.Equal("/api/tinnhan/file/abc123", hoSo!.DuongDanAnhDaiDien);
    }

    [Fact]
    public async Task DoiAnhDaiDienAsync_IdKhongTonTai_TraVeNull()
    {
        var (dichVu, _, _) = TaoDichVu();

        var hoSo = await dichVu.DoiAnhDaiDienAsync("507f1f77bcf86cd799439099", "/api/tinnhan/file/abc123");

        Assert.Null(hoSo);
    }

    [Fact]
    public async Task DanhDauHoanTatHoSoAsync_ThanhCong_SetDungCoGiuNguyenFieldKhac()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "HoanTat1", Email = "hoantat1@gmail.com", TenHienThi = "Tên Riêng" };
        kho.DanhSach.Add(nguoiDung);

        var hoSo = await dichVu.DanhDauHoanTatHoSoAsync(nguoiDung.Id);

        Assert.True(hoSo!.DaXemHoanTatHoSo);
        Assert.Equal("Tên Riêng", hoSo.TenHienThi);
    }

    [Fact]
    public async Task DanhDauHoanTatHoSoAsync_IdKhongTonTai_TraVeNull()
    {
        var (dichVu, _, _) = TaoDichVu();

        var hoSo = await dichVu.DanhDauHoanTatHoSoAsync("507f1f77bcf86cd799439099");

        Assert.Null(hoSo);
    }
}
