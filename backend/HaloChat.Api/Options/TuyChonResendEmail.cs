namespace HaloChat.Api.Options;

public class TuyChonResendEmail
{
    public const string TenMuc = "ResendEmail";

    public string ApiKey { get; set; } = string.Empty;

    // Mặc định "onboarding@resend.dev" — địa chỉ gửi thử miễn phí của Resend,
    // dùng được ngay không cần xác minh domain riêng, nhưng chỉ gửi tới đúng
    // email đã đăng ký tài khoản Resend cho tới khi domain được xác minh.
    public string NguoiGuiEmail { get; set; } = "onboarding@resend.dev";
    public string NguoiGuiHienThi { get; set; } = "HaloChat";
}
