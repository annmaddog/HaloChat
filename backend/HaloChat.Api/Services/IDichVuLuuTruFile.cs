namespace HaloChat.Api.Services;

/// <summary>Kết quả đọc lại 1 file đã lưu — nội dung nhị phân + thông tin để trả về HTTP response.</summary>
public record KetQuaLayFile(byte[] NoiDung, string LoaiMime, string TenFile);

/// <summary>
/// Lưu trữ file người dùng tải lên (ảnh/tài liệu đính kèm tin nhắn) — trừu
/// tượng hóa nơi lưu thật sự để dễ thay đổi và dễ giả lập trong test.
///
/// [Sửa lỗi] Bản gốc lưu trực tiếp vào ổ đĩa container (ContentRootPath/uploads).
/// Trên Render (nền tảng đang host backend), ổ đĩa container là "ephemeral" —
/// bị xóa sạch mỗi khi deploy lại hoặc server ngủ rồi khởi động lại (gói
/// miễn phí sleep sau 15 phút không hoạt động) — khiến mọi file đã tải lên
/// trước đó biến mất vĩnh viễn, tải xuống báo 404. Chuyển sang lưu trong
/// MongoDB (GridFS) — cùng Atlas cluster đã dùng sẵn, bền vững qua các lần
/// deploy/restart, không cần thêm dịch vụ/tài khoản ngoài.
/// </summary>
public interface IDichVuLuuTruFile
{
    /// <summary>Lưu file, trả về id duy nhất để tải lại sau này.</summary>
    Task<string> LuuAsync(Stream noiDung, string tenFile, string loaiMime);

    /// <summary>Đọc lại file theo id — null nếu id không hợp lệ hoặc không tồn tại.</summary>
    Task<KetQuaLayFile?> LayAsync(string id);
}
