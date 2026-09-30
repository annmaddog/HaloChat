namespace HaloChat.Security;

/// <summary>
/// [Chữ ký số] Xác thực NGƯỜI GỬI + TOÀN VẸN nội dung tin nhắn — bổ sung cho
/// DichVuMaHoa (giấu nội dung), không thay thế. Dùng lại đúng cặp khóa RSA hiện
/// có (KhoaCongKhai/KhoaBiMat) — xem docs/superpowers/specs/2026-09-30-halochat-chu-ky-so-design.md §3.
/// </summary>
public interface IDichVuChuKySo
{
    /// <summary>Ký noiDung bằng khóa bí mật RSA (Base64) của người gửi — trả chữ ký Base64.</summary>
    string KyDuLieu(string noiDung, string khoaBiMatNguoiKy);

    /// <summary>Xác minh chuKy (Base64) trên đúng noiDung bằng khóa công khai RSA (Base64) của người ký.</summary>
    bool XacMinhChuKy(string noiDung, string chuKy, string khoaCongKhaiNguoiKy);
}
