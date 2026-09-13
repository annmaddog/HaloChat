using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface INhomRepository
{
    Task ThemMoiAsync(Nhom nhom);

    Task<Nhom?> TimTheoIdAsync(string id);

    /// <summary>Danh sách nhóm mà userId là thành viên (ThanhVienIds chứa id đó).</summary>
    Task<List<Nhom>> LayTheoThanhVienAsync(string userId);

    Task ThemThanhVienAsync(string nhomId, string userId);

    Task XoaThanhVienAsync(string nhomId, string userId);

    Task CapNhatThongTinAsync(string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien);

    Task XoaNhomAsync(string nhomId);
}
