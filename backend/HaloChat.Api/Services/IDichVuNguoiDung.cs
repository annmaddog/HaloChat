using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuNguoiDung
{
    Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau);
    Task<string?> DangNhap(string tenDangNhap, string matKhau);
    Task<List<NguoiDungTomTatDto>> LayDanhSachNguoiDung(string idHienTai);
    Task CapNhatCaiDatAsync(string idHienTai, bool choPhepTinNhanTuNguoiLa);
    Task<HoSoCaNhanDto?> LayThongTinCaNhanAsync(string id);
}
