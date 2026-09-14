using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuNguoiDung
{
    Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau);
    Task<string?> DangNhap(string tenDangNhap, string matKhau);
    Task<List<NguoiDungTomTatDto>> LayDanhSachNguoiDung(string idHienTai);
    Task CapNhatCaiDatAsync(
        string idHienTai, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong,
        bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom);
    Task<HoSoCaNhanDto?> LayThongTinCaNhanAsync(string id);
    Task YeuCauOtpDatLaiMatKhauAsync(string email);
    Task<KetQuaDatLaiMatKhauDto> DatLaiMatKhauAsync(string email, string maOtp, string matKhauMoi);
    Task<KetQuaDoiMatKhauDto> DoiMatKhauAsync(string idHienTai, string matKhauCu, string matKhauMoi);
}
