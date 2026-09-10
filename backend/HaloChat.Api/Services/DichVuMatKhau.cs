using System.Security.Cryptography;
using System.Text;

namespace HaloChat.Api.Services;

public class DichVuMatKhau : IDichVuMatKhau
{
    public string TaoSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public string BamMatKhau(string matKhau, string salt)
    {
        var duLieu = Encoding.UTF8.GetBytes(matKhau + salt);
        var bamBytes = SHA256.HashData(duLieu);
        return Convert.ToBase64String(bamBytes);
    }

    public bool KiemTraMatKhau(string matKhau, string salt, string matKhauBam)
    {
        var bamMoi = Convert.FromBase64String(BamMatKhau(matKhau, salt));
        var bamCu = Convert.FromBase64String(matKhauBam);

        if (bamMoi.Length != bamCu.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(bamMoi, bamCu);
    }
}
