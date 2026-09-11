using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface INguoiDungRepository
{
    Task<bool> TonTaiDinhDanhAsync(string dinhDanh);
    Task ThemMoiAsync(NguoiDung nguoiDung);
    Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap);
    Task<List<NguoiDung>> LayTatCaAsync();
    Task<NguoiDung?> TimTheoIdAsync(string id);
}
