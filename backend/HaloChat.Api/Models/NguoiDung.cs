using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public class NguoiDung
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string TenTaiKhoan { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MatKhauBam { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;

    // [BẢO MẬT - GĐ6] Cặp khóa RSA (KhoaCongKhai/KhoaBiMat) sẽ được sinh và
    // gán vào đây khi nhóm triển khai mã hóa lai RSA-AES. Để trống ở giai
    // đoạn này theo đúng chính sách stub trong spec (§9).
    public string KhoaCongKhai { get; set; } = string.Empty;
    public string KhoaBiMat { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public bool ChoPhepTinNhanTuNguoiLa { get; set; } = false;

    // [GĐ5b-2] Khi false, server không đẩy sự kiện TrangThaiHoatDongThayDoi
    // cho user này tới bạn bè — vẫn cho phép ẩn trạng thái online/offline
    // theo ý muốn, đúng toggle "Hiển thị trạng thái hoạt động" ở Cài đặt.
    public bool HienThiTrangThaiHoatDong { get; set; } = true;

    // [GĐ5c] Khi false, DichVuNhom.ThemThanhVienAsync/TaoNhomAsync từ chối
    // thêm người này vào bất kỳ nhóm nào (xem KhongChoPhepThemVaoNhomException).
    public bool ChoPhepThemVaoNhom { get; set; } = true;

    // [GĐ5c] 3 toggle quyết định badge tương ứng ở sidebar (Tin nhắn/Bạn
    // bè/Nhóm) có cộng dồn số chưa đọc hay không — không điều khiển bất kỳ
    // kênh thông báo đẩy thật nào (hệ thống chưa có kênh nào như vậy).
    public bool ThongBaoTinNhanMoi { get; set; } = true;
    public bool ThongBaoLoiMoiKetBan { get; set; } = true;
    public bool ThongBaoNhom { get; set; } = true;

    // [Quên mật khẩu] Mã OTP băm bằng Salt sẵn có của chính user (không cần
    // field salt riêng) — xem IDichVuMatKhau.BamMatKhau. Null khi chưa từng
    // yêu cầu OTP hoặc đã đặt lại mật khẩu thành công.
    public string? MaOtpBam { get; set; }
    public DateTime? MaOtpHetHan { get; set; }
    public int SoLanThuSai { get; set; } = 0;

    // Thời điểm gửi OTP gần nhất — dùng để chặn spam gửi lại liên tục
    // (cooldown 60 giây), tách biệt với MaOtpHetHan (thời điểm OTP đó
    // hết hạn sử dụng, 10 phút sau khi gửi).
    public DateTime? MaOtpGuiLucNao { get; set; }
}
