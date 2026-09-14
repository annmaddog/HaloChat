namespace HaloChat.Api.Options;

public class TuyChonResendEmail
{
    public const string TenMuc = "ResendEmail";

    public string ApiKey { get; set; } = string.Empty;

    // Domain halochat.website đã xác minh với Resend (SPF/DKIM/DMARC) nên có
    // thể gửi tới bất kỳ người nhận nào, không còn giới hạn "chỉ gửi cho
    // chính mình" của địa chỉ resend.dev dùng thử ban đầu.
    public string NguoiGuiEmail { get; set; } = "otp@halochat.website";
    public string NguoiGuiHienThi { get; set; } = "HaloChat";
}
