namespace HaloChat.Security;

public interface IDichVuMatKhau
{
    string TaoSalt();
    string BamMatKhau(string matKhau, string salt);
    bool KiemTraMatKhau(string matKhau, string salt, string matKhauBam);
}
