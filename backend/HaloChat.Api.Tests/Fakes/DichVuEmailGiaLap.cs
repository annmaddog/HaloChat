using HaloChat.Api.Services;

namespace HaloChat.Api.Tests.Fakes;

public class DichVuEmailGiaLap : IDichVuEmail
{
    public List<(string DiaChiNhan, string MaOtp)> DaGui { get; } = new();

    /// <summary>
    /// Khi true, lần gọi GuiEmailOtpAsync tiếp theo sẽ ném lỗi (giả lập SMTP
    /// hỏng) rồi tự động trở về false — không ảnh hưởng các kiểm thử khác.
    /// </summary>
    public bool NemLoiLanKeTiep { get; set; }

    public Task GuiEmailOtpAsync(string diaChiNhan, string maOtp)
    {
        if (NemLoiLanKeTiep)
        {
            NemLoiLanKeTiep = false;
            throw new InvalidOperationException("Giả lập lỗi gửi email OTP (SMTP hỏng).");
        }

        DaGui.Add((diaChiNhan, maOtp));
        return Task.CompletedTask;
    }
}
