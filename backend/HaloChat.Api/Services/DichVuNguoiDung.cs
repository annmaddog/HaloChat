using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;
using HaloChat.Security;

namespace HaloChat.Api.Services;

public class DichVuNguoiDung : IDichVuNguoiDung
{
    private readonly INguoiDungRepository _kho;
    private readonly IDichVuMatKhau _dichVuMatKhau;
    private readonly IDichVuJwt _dichVuJwt;
    private readonly IDichVuEmail _dichVuEmail;

    public DichVuNguoiDung(
        INguoiDungRepository kho, IDichVuMatKhau dichVuMatKhau, IDichVuJwt dichVuJwt, IDichVuEmail dichVuEmail)
    {
        _kho = kho;
        _dichVuMatKhau = dichVuMatKhau;
        _dichVuJwt = dichVuJwt;
        _dichVuEmail = dichVuEmail;
    }

    public async Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau)
    {
        if (await _kho.TonTaiDinhDanhAsync(tenTaiKhoan))
        {
            return new KetQuaDangKyDto(false, "Tên tài khoản đã tồn tại.");
        }

        if (await _kho.TonTaiDinhDanhAsync(email))
        {
            return new KetQuaDangKyDto(false, "Email đã được sử dụng.");
        }

        var salt = _dichVuMatKhau.TaoSalt();
        var nguoiDungMoi = new NguoiDung
        {
            TenTaiKhoan = tenTaiKhoan,
            Email = email,
            Salt = salt,
            MatKhauBam = _dichVuMatKhau.BamMatKhau(matKhau, salt),
        };

        try
        {
            await _kho.ThemMoiAsync(nguoiDungMoi);
        }
        catch (TrungLapDinhDanhException)
        {
            return new KetQuaDangKyDto(false, "Tên tài khoản hoặc email đã tồn tại.");
        }

        return new KetQuaDangKyDto(true, "Đăng ký thành công.");
    }

    public async Task<string?> DangNhap(string tenDangNhap, string matKhau)
    {
        var nguoiDung = await _kho.TimTheoTenTaiKhoanHoacEmailAsync(tenDangNhap);
        if (nguoiDung is null)
        {
            return null;
        }

        if (!_dichVuMatKhau.KiemTraMatKhau(matKhau, nguoiDung.Salt, nguoiDung.MatKhauBam))
        {
            return null;
        }

        return _dichVuJwt.TaoJwt(nguoiDung);
    }

    public async Task<List<NguoiDungTomTatDto>> LayDanhSachNguoiDung(string idHienTai)
    {
        var tatCa = await _kho.LayTatCaAsync();
        return tatCa
            .Where(nd => nd.Id != idHienTai)
            .Select(nd => new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email))
            .ToList();
    }

    public Task CapNhatCaiDatAsync(string idHienTai, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong) =>
        _kho.CapNhatCaiDatAsync(idHienTai, choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong);

    public async Task<HoSoCaNhanDto?> LayThongTinCaNhanAsync(string id)
    {
        var nguoiDung = await _kho.TimTheoIdAsync(id);
        return nguoiDung is null
            ? null
            : new HoSoCaNhanDto(
                nguoiDung.Id, nguoiDung.TenTaiKhoan, nguoiDung.Email,
                nguoiDung.ChoPhepTinNhanTuNguoiLa, nguoiDung.HienThiTrangThaiHoatDong);
    }

    private const int HetHanOtpPhut = 10;
    private const int CooldownOtpGiay = 60;
    private const int SoLanSaiToiDa = 5;

    public async Task YeuCauOtpDatLaiMatKhauAsync(string email)
    {
        var nguoiDung = await _kho.TimTheoTenTaiKhoanHoacEmailAsync(email);
        if (nguoiDung is null)
        {
            return; // Không tiết lộ email không tồn tại — controller luôn trả thông báo chung.
        }

        if (nguoiDung.MaOtpGuiLucNao is not null &&
            (DateTime.UtcNow - nguoiDung.MaOtpGuiLucNao.Value).TotalSeconds < CooldownOtpGiay)
        {
            return; // Chặn spam gửi lại liên tục — vẫn không tiết lộ gì ra ngoài.
        }

        var maOtp = TaoMaOtp();
        var maOtpBam = _dichVuMatKhau.BamMatKhau(maOtp, nguoiDung.Salt);
        var hetHan = DateTime.UtcNow.AddMinutes(HetHanOtpPhut);
        var guiLucNao = DateTime.UtcNow;

        await _kho.LuuOtpAsync(nguoiDung.Id, maOtpBam, hetHan, guiLucNao);
        await _dichVuEmail.GuiEmailOtpAsync(nguoiDung.Email, maOtp);
    }

    public async Task<KetQuaDatLaiMatKhauDto> DatLaiMatKhauAsync(string email, string maOtp, string matKhauMoi)
    {
        const string ThongBaoOtpKhongDung = "Mã OTP không đúng.";
        const string ThongBaoOtpHetHan = "Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng gửi lại mã mới.";

        var nguoiDung = await _kho.TimTheoTenTaiKhoanHoacEmailAsync(email);
        if (nguoiDung is null || nguoiDung.MaOtpBam is null || nguoiDung.MaOtpHetHan is null)
        {
            return new KetQuaDatLaiMatKhauDto(false, ThongBaoOtpHetHan);
        }

        if (nguoiDung.MaOtpHetHan.Value < DateTime.UtcNow || nguoiDung.SoLanThuSai >= SoLanSaiToiDa)
        {
            return new KetQuaDatLaiMatKhauDto(false, ThongBaoOtpHetHan);
        }

        var maOtpBamNhapVao = _dichVuMatKhau.BamMatKhau(maOtp, nguoiDung.Salt);
        if (maOtpBamNhapVao != nguoiDung.MaOtpBam)
        {
            await _kho.TangSoLanThuSaiOtpAsync(nguoiDung.Id);
            return new KetQuaDatLaiMatKhauDto(false, ThongBaoOtpKhongDung);
        }

        var saltMoi = _dichVuMatKhau.TaoSalt();
        var matKhauBamMoi = _dichVuMatKhau.BamMatKhau(matKhauMoi, saltMoi);
        await _kho.DatLaiMatKhauAsync(nguoiDung.Id, matKhauBamMoi, saltMoi);

        return new KetQuaDatLaiMatKhauDto(true, "Đặt lại mật khẩu thành công.");
    }

    private static string TaoMaOtp()
    {
        var so = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000);
        return so.ToString("D6");
    }
}
