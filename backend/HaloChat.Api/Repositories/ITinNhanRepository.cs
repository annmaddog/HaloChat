using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface ITinNhanRepository
{
    Task ThemMoiAsync(TinNhan tinNhan);

    /// <summary>
    /// Lấy lịch sử tin nhắn 1-1 giữa nguoiA và nguoiB, mới nhất trước.
    /// truocId (nếu có) chỉ lấy tin nhắn cũ hơn tin nhắn đó (phân trang lùi).
    /// </summary>
    Task<List<TinNhan>> LayLichSuTheoNguoiDungAsync(string nguoiA, string nguoiB, string? truocId, int soLuong);

    /// <summary>Đánh dấu đã đọc mọi tin nhắn nguoiGuiId đã gửi cho nguoiNhanId.</summary>
    Task DanhDauDaDocAsync(string nguoiGuiId, string nguoiNhanId);

    /// <summary>Toàn bộ tin nhắn 1-1 (NhomId null) mà nguoiDungId là người gửi hoặc người nhận, mới nhất trước.</summary>
    Task<List<TinNhan>> LayTatCaLienQuanAsync(string nguoiDungId);
}
