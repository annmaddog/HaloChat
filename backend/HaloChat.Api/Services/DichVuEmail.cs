using System.Net.Http.Headers;
using System.Net.Http.Json;
using HaloChat.Api.Options;
using Microsoft.Extensions.Options;

namespace HaloChat.Api.Services;

// Gửi email qua API HTTPS của Resend (thay vì SMTP trực tiếp) — nhiều nền
// tảng PaaS miễn phí (Render, Railway, ...) chặn cổng SMTP (25/465/587) ra
// ngoài để chống spam, khiến MailKit không bao giờ kết nối được. HTTPS
// (cổng 443) thì không bị chặn, nên chuyển hẳn sang gọi API qua HttpClient.
public class DichVuEmail : IDichVuEmail
{
    private readonly HttpClient _httpClient;
    private readonly TuyChonResendEmail _tuyChon;

    public DichVuEmail(HttpClient httpClient, IOptions<TuyChonResendEmail> tuyChon)
    {
        _httpClient = httpClient;
        _tuyChon = tuyChon.Value;
        _httpClient.BaseAddress = new Uri("https://api.resend.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tuyChon.ApiKey);
    }

    public async Task GuiEmailOtpAsync(string diaChiNhan, string maOtp)
    {
        var noiDung =
            "Xin chào,\n\n" +
            $"Mã OTP để đặt lại mật khẩu HaloChat của bạn là: {maOtp}\n\n" +
            "Mã này có hiệu lực trong 10 phút. Không chia sẻ mã này với bất kỳ ai.\n\n" +
            "Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.";

        var yeuCau = new
        {
            from = $"{_tuyChon.NguoiGuiHienThi} <{_tuyChon.NguoiGuiEmail}>",
            to = new[] { diaChiNhan },
            subject = "Mã OTP đặt lại mật khẩu HaloChat",
            text = noiDung,
        };

        var phanHoi = await _httpClient.PostAsJsonAsync("emails", yeuCau);
        if (!phanHoi.IsSuccessStatusCode)
        {
            var noiDungLoi = await phanHoi.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Resend trả lỗi {(int)phanHoi.StatusCode}: {noiDungLoi}");
        }
    }
}
