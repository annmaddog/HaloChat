namespace HaloChat.Api.Services;

public interface IDichVuMatKhau
{
    string TaoSalt();
    string BamMatKhau(string matKhau, string salt);
    bool KiemTraMatKhau(string matKhau, string salt, string matKhauBam);
}
