namespace HaloChat.Api.Services;

public interface IDichVuEmail
{
    Task GuiEmailOtpAsync(string diaChiNhan, string maOtp);
}
