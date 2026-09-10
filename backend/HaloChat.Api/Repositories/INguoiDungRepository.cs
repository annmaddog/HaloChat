using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface INguoiDungRepository
{
    Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan);
    Task<bool> TonTaiEmailAsync(string email);
    Task ThemMoiAsync(NguoiDung nguoiDung);
    Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap);
}
