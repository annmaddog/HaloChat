using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface ILoiMoiKetBanRepository
{
    /// <summary>Có lời mời đang chờ hoặc đã là bạn bè giữa 2 người (bất kể ai gửi trước) không.</summary>
    Task<bool> TonTaiLoiMoiDangHoatDongAsync(string nguoiA, string nguoiB);

    Task ThemMoiAsync(LoiMoiKetBan loiMoi);

    Task<LoiMoiKetBan?> TimTheoIdAsync(string id);

    Task CapNhatTrangThaiAsync(string id, TrangThaiLoiMoiKetBan trangThai);

    /// <summary>Lời mời đã DaChapNhan liên quan tới người dùng (cả 2 chiều) — dùng làm danh sách bạn bè.</summary>
    Task<List<LoiMoiKetBan>> LayBanBeAsync(string nguoiDungId);

    Task<List<LoiMoiKetBan>> LayLoiMoiDenAsync(string nguoiDungId);

    Task<List<LoiMoiKetBan>> LayLoiMoiGuiAsync(string nguoiDungId);

    Task<bool> LaBanBeAsync(string nguoiA, string nguoiB);
}
