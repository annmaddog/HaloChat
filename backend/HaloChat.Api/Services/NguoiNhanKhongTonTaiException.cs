namespace HaloChat.Api.Services;

/// <summary>Ném ra khi gửi tin nhắn cho một NguoiDungId không tồn tại trong hệ thống.</summary>
public class NguoiNhanKhongTonTaiException : Exception
{
    public NguoiNhanKhongTonTaiException(string nguoiNhanId)
        : base($"Người nhận không tồn tại: {nguoiNhanId}.")
    {
    }
}
