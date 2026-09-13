using HaloChat.Api.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HaloChat.Api.Services;

public class DichVuEmail : IDichVuEmail
{
    private readonly TuyChonSmtpEmail _tuyChon;

    public DichVuEmail(IOptions<TuyChonSmtpEmail> tuyChon)
    {
        _tuyChon = tuyChon.Value;
    }

    public async Task GuiEmailOtpAsync(string diaChiNhan, string maOtp)
    {
        var thongDiep = new MimeMessage();
        thongDiep.From.Add(new MailboxAddress(_tuyChon.NguoiGuiHienThi, _tuyChon.TenDangNhap));
        thongDiep.To.Add(MailboxAddress.Parse(diaChiNhan));
        thongDiep.Subject = "Mã OTP đặt lại mật khẩu HaloChat";
        thongDiep.Body = new TextPart("plain")
        {
            Text =
                $"Xin chào,\n\n" +
                $"Mã OTP để đặt lại mật khẩu HaloChat của bạn là: {maOtp}\n\n" +
                "Mã này có hiệu lực trong 10 phút. Không chia sẻ mã này với bất kỳ ai.\n\n" +
                "Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.",
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_tuyChon.MayChu, _tuyChon.Cong, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_tuyChon.TenDangNhap, _tuyChon.MatKhauUngDung);
        await client.SendAsync(thongDiep);
        await client.DisconnectAsync(true);
    }
}
