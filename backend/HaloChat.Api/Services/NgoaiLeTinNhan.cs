namespace HaloChat.Api.Services;

public class TinNhanKhongTonTaiException : Exception
{
    public TinNhanKhongTonTaiException() : base("Tin nhắn không tồn tại.")
    {
    }
}

public class KhongPhaiNguoiGuiException : Exception
{
    public KhongPhaiNguoiGuiException() : base("Bạn không phải người gửi tin nhắn này.")
    {
    }
}

public class KhongCoQuyenTrenTinNhanException : Exception
{
    public KhongCoQuyenTrenTinNhanException() : base("Bạn không có quyền thao tác trên tin nhắn này.")
    {
    }
}
