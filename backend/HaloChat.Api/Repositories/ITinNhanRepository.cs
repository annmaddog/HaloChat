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

    /// <summary>Lịch sử tin nhắn của 1 nhóm, mới nhất trước, phân trang lùi giống LayLichSuTheoNguoiDungAsync.</summary>
    Task<List<TinNhan>> LayLichSuNhomAsync(string nhomId, string? truocId, int soLuong);

    /// <summary>
    /// Đếm số tin nhắn của 1 nhóm có Id lớn hơn sauId (mới hơn) — null thì đếm tất cả tin nhắn của nhóm.
    /// Loại trừ tin nhắn do loaiTruNguoiGuiId gửi (không tính tin nhắn của chính người đang xem là "chưa đọc").
    /// </summary>
    Task<int> DemTinNhanSauIdAsync(string nhomId, string? sauId, string loaiTruNguoiGuiId);

    /// <summary>Xóa toàn bộ tin nhắn của 1 nhóm (dùng khi giải tán nhóm).</summary>
    Task XoaTheoNhomAsync(string nhomId);

    /// <summary>Lấy 1 tin nhắn theo id, null nếu không tồn tại. Dùng để validate trả lời (GĐ6a) và các hành động trên tin nhắn (GĐ6b).</summary>
    Task<TinNhan?> TimTheoIdAsync(string id);
}
