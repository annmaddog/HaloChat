using HaloChat.Api.Services;

namespace HaloChat.Api.Tests.Fakes;

public class DichVuEmailGiaLap : IDichVuEmail
{
    public List<(string DiaChiNhan, string MaOtp)> DaGui { get; } = new();

    public Task GuiEmailOtpAsync(string diaChiNhan, string maOtp)
    {
        DaGui.Add((diaChiNhan, maOtp));
        return Task.CompletedTask;
    }
}
