namespace HaloChat.Api.Options;

public class TuyChonSmtpEmail
{
    public const string TenMuc = "SmtpEmail";

    public string MayChu { get; set; } = "smtp.gmail.com";
    public int Cong { get; set; } = 587;
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauUngDung { get; set; } = string.Empty;
    public string NguoiGuiHienThi { get; set; } = "HaloChat";
}
