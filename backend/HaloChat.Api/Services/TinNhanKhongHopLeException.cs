namespace HaloChat.Api.Services;

/// <summary>Ném ra khi dữ liệu gửi tin nhắn không hợp lệ (loại sai, thiếu nội dung/file).</summary>
public class TinNhanKhongHopLeException : Exception
{
    public TinNhanKhongHopLeException(string thongBao) : base(thongBao)
    {
    }
}
