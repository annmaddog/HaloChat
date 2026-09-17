using System.Security.Cryptography;
using System.Text;

namespace HaloChat.Security;

/// <summary>
/// [GĐ6] Cài đặt thật cho mã hóa lai RSA-AES — xem
/// docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md §2, §9.
/// AES-256-GCM mã hóa nội dung (nhanh, dữ liệu lớn); RSA-OAEP mã hóa khóa
/// AES đó bằng Public Key người nhận, giải quyết bài toán trao đổi khóa an
/// toàn qua mạng mà không cần 2 bên thống nhất trước 1 khóa bí mật chung.
/// </summary>
public class DichVuMaHoa : IDichVuMaHoa
{
    private const int KichThuocRsaBit = 2048;
    private const int KichThuocKhoaAesByte = 32; // AES-256 = khóa 256 bit = 32 byte
    private const int KichThuocNonceByte = 12; // Chuẩn khuyến nghị cho AES-GCM
    private const int KichThuocAuthTagByte = 16; // Chuẩn AES-GCM (128 bit)

    public (string KhoaCongKhai, string KhoaBiMat) SinhCapKhoaRsa()
    {
        using var rsa = RSA.Create(KichThuocRsaBit);
        var khoaCongKhai = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var khoaBiMat = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        return (khoaCongKhai, khoaBiMat);
    }

    public byte[] SinhKhoaPhienAes() => RandomNumberGenerator.GetBytes(KichThuocKhoaAesByte);

    public KetQuaMaHoaAes MaHoaTinNhan(string noiDungTinNhan, byte[] khoaPhienAes)
    {
        var noiDungBytes = Encoding.UTF8.GetBytes(noiDungTinNhan);
        var nonce = RandomNumberGenerator.GetBytes(KichThuocNonceByte);
        var ciphertext = new byte[noiDungBytes.Length];
        var authTag = new byte[KichThuocAuthTagByte];

        using var aesGcm = new AesGcm(khoaPhienAes, KichThuocAuthTagByte);
        aesGcm.Encrypt(nonce, noiDungBytes, ciphertext, authTag);

        return new KetQuaMaHoaAes(
            Convert.ToBase64String(ciphertext),
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(authTag));
    }

    public string GiaiMaTinNhan(string ciphertext, string nonce, string authTag, byte[] khoaPhienAes)
    {
        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        var nonceBytes = Convert.FromBase64String(nonce);
        var authTagBytes = Convert.FromBase64String(authTag);
        var plaintextBytes = new byte[ciphertextBytes.Length];

        using var aesGcm = new AesGcm(khoaPhienAes, KichThuocAuthTagByte);
        // Ném CryptographicException nếu AuthTag không khớp (dữ liệu bị sửa)
        // hoặc khoaPhienAes sai — đúng đặc tính "authenticated encryption"
        // của GCM, không âm thầm trả về dữ liệu rác.
        aesGcm.Decrypt(nonceBytes, ciphertextBytes, authTagBytes, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public string MaHoaKhoaPhien(byte[] khoaPhienAes, string khoaCongKhaiNguoiNhan)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(khoaCongKhaiNguoiNhan), out _);
        var khoaDaMaHoa = rsa.Encrypt(khoaPhienAes, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(khoaDaMaHoa);
    }

    public byte[] GiaiMaKhoaPhien(string khoaPhienDaMaHoa, string khoaBiMatNguoiNhan)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(khoaBiMatNguoiNhan), out _);
        return rsa.Decrypt(Convert.FromBase64String(khoaPhienDaMaHoa), RSAEncryptionPadding.OaepSHA256);
    }
}
