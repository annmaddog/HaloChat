namespace HaloChat.Api.Services;

/// <summary>Ném ra khi người dùng cố gửi lời mời kết bạn cho chính mình.</summary>
public class KhongTheTuKetBanException : Exception
{
    public KhongTheTuKetBanException() : base("Không thể tự gửi lời mời kết bạn cho chính mình.")
    {
    }
}

/// <summary>Ném ra khi người được mời kết bạn không tồn tại trong hệ thống.</summary>
public class NguoiDuocMoiKhongTonTaiException : Exception
{
    public NguoiDuocMoiKhongTonTaiException(string id) : base($"Người dùng không tồn tại: {id}.")
    {
    }
}

/// <summary>Ném ra khi đã có lời mời đang chờ hoặc đã là bạn bè giữa 2 người.</summary>
public class LoiMoiKetBanDaTonTaiException : Exception
{
    public LoiMoiKetBanDaTonTaiException() : base("Đã có lời mời kết bạn hoặc đã là bạn bè.")
    {
    }
}

/// <summary>Ném ra khi thao tác (chấp nhận/từ chối) trên 1 lời mời không tồn tại.</summary>
public class LoiMoiKetBanKhongTonTaiException : Exception
{
    public LoiMoiKetBanKhongTonTaiException() : base("Lời mời kết bạn không tồn tại.")
    {
    }
}

/// <summary>Ném ra khi lời mời đã được chấp nhận/từ chối trước đó, không thể xử lý lại.</summary>
public class LoiMoiKetBanDaXuLyException : Exception
{
    public LoiMoiKetBanDaXuLyException() : base("Lời mời kết bạn đã được xử lý trước đó.")
    {
    }
}

/// <summary>Ném ra khi người không phải là người nhận cố chấp nhận/từ chối lời mời.</summary>
public class KhongCoQuyenXuLyLoiMoiException : Exception
{
    public KhongCoQuyenXuLyLoiMoiException() : base("Bạn không có quyền xử lý lời mời này.")
    {
    }
}
