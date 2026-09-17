namespace HaloChat.Security;

/// <summary>Kết quả mã hóa 1 chuỗi bằng AES-256-GCM — cả 3 giá trị đều dạng Base64.</summary>
public record KetQuaMaHoaAes(string Ciphertext, string Nonce, string AuthTag);

/// <summary>
/// Mã hóa lai RSA-AES: AES-256-GCM mã hóa nội dung tin nhắn (nhanh, dữ liệu
/// lớn), RSA-OAEP mã hóa chính khóa AES đó (giải quyết bài toán trao đổi
/// khóa an toàn qua mạng — xem docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md §2).
/// </summary>
public interface IDichVuMaHoa
{
    /// <summary>Sinh 1 cặp khóa RSA mới (Public/Private, dạng Base64) — gọi 1 lần khi đăng ký tài khoản.</summary>
    (string KhoaCongKhai, string KhoaBiMat) SinhCapKhoaRsa();

    /// <summary>Sinh 1 khóa AES-256 ngẫu nhiên dùng cho 1 tin nhắn/phiên.</summary>
    byte[] SinhKhoaPhienAes();

    /// <summary>Mã hóa nội dung tin nhắn bằng AES-256-GCM.</summary>
    KetQuaMaHoaAes MaHoaTinNhan(string noiDungTinNhan, byte[] khoaPhienAes);

    /// <summary>Giải mã nội dung tin nhắn bằng AES-256-GCM.</summary>
    string GiaiMaTinNhan(string ciphertext, string nonce, string authTag, byte[] khoaPhienAes);

    /// <summary>Mã hóa khóa AES bằng RSA-OAEP dùng Public Key của người nhận.</summary>
    string MaHoaKhoaPhien(byte[] khoaPhienAes, string khoaCongKhaiNguoiNhan);

    /// <summary>Giải mã khóa AES bằng RSA-OAEP dùng Private Key của người nhận.</summary>
    byte[] GiaiMaKhoaPhien(string khoaPhienDaMaHoa, string khoaBiMatNguoiNhan);
}
