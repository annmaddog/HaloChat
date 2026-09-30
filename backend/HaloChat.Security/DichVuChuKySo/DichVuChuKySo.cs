using System.Security.Cryptography;
using System.Text;

namespace HaloChat.Security;

/// <summary>Cài đặt thật cho IDichVuChuKySo — RSA-PSS + SHA-256, dùng lại định dạng khóa PKCS#1 (Base64) giống DichVuMaHoa.</summary>
public class DichVuChuKySo : IDichVuChuKySo
{
    public string KyDuLieu(string noiDung, string khoaBiMatNguoiKy)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(khoaBiMatNguoiKy), out _);
        var duLieu = Encoding.UTF8.GetBytes(noiDung);
        var chuKy = rsa.SignData(duLieu, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return Convert.ToBase64String(chuKy);
    }

    public bool XacMinhChuKy(string noiDung, string chuKy, string khoaCongKhaiNguoiKy)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(khoaCongKhaiNguoiKy), out _);
            var duLieu = Encoding.UTF8.GetBytes(noiDung);
            var chuKyBytes = Convert.FromBase64String(chuKy);
            return rsa.VerifyData(duLieu, chuKyBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // Chữ ký/khóa hỏng định dạng (Base64 sai, độ dài khóa sai...) → coi như
            // không hợp lệ thay vì làm sập request đọc tin nhắn.
            return false;
        }
    }
}
