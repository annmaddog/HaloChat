namespace HaloChat.Security;

public class DichVuMaHoa
{
    public string MaHoaTinNhan(string noiDungTinNhan)
    {
        // [BẢO MẬT - GĐ6]
        // Mã hóa nội dung bằng AES-256-GCM: sinh Nonce ngẫu nhiên, mã hóa
        // noiDungTinNhan bằng AES Session Key, trả về Ciphertext kèm Nonce +
        // AuthTag (định dạng lưu trữ do nhóm quyết định).
        return "";
    }

    public string GiaiMaTinNhan(string tinNhanDaMaHoa)
    {
        // [BẢO MẬT - GĐ6]
        // Giải mã nội dung bằng AES-256-GCM, dùng lại Nonce + AuthTag đã lưu
        // kèm tin nhắn.
        return "";
    }

    public string MaHoaKhoaPhien(string khoaPhienAes, string khoaCongKhaiNguoiNhan)
    {
        // [BẢO MẬT - GĐ6]
        // Mã hóa AES Session Key bằng RSA-OAEP, dùng Public Key của người
        // nhận, trước khi gửi lên Server (Server không cần đọc được
        // khoaPhienAes gốc).
        return "";
    }

    public string GiaiMaKhoaPhien(string khoaPhienDaMaHoa, string khoaBiMatNguoiNhan)
    {
        // [BẢO MẬT - GĐ6]
        // Giải mã AES Session Key bằng RSA Private Key của người nhận
        // (KhoaBiMat).
        return "";
    }
}
