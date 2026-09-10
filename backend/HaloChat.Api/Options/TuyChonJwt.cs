namespace HaloChat.Api.Options;

public class TuyChonJwt
{
    public const string TenMuc = "Jwt";

    public string ChuoiBiMat { get; set; } = string.Empty;
    public string NguoiPhatHanh { get; set; } = "HaloChat";
    public string DoiTuong { get; set; } = "HaloChatNguoiDung";
    public int SoPhutHetHan { get; set; } = 60;
}
