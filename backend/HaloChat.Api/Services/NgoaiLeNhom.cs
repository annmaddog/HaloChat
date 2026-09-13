namespace HaloChat.Api.Services;

/// <summary>Ném ra khi thao tác trên 1 nhóm không tồn tại.</summary>
public class NhomKhongTonTaiException : Exception
{
    public NhomKhongTonTaiException() : base("Nhóm không tồn tại.")
    {
    }
}

/// <summary>Ném ra khi người gọi không phải thành viên của nhóm đang thao tác.</summary>
public class KhongPhaiThanhVienNhomException : Exception
{
    public KhongPhaiThanhVienNhomException() : base("Bạn không phải thành viên của nhóm này.")
    {
    }
}

/// <summary>Ném ra khi người gọi không phải người tạo (admin) của nhóm.</summary>
public class KhongCoQuyenQuanTriNhomException : Exception
{
    public KhongCoQuyenQuanTriNhomException() : base("Chỉ người tạo nhóm mới có quyền thực hiện thao tác này.")
    {
    }
}

/// <summary>Ném ra khi id thành viên được chọn không tồn tại trong hệ thống.</summary>
public class ThanhVienKhongTonTaiException : Exception
{
    public ThanhVienKhongTonTaiException(string id) : base($"Người dùng không tồn tại: {id}.")
    {
    }
}

/// <summary>Ném ra khi cố xóa người tạo (admin) khỏi nhóm bằng thao tác xóa thành viên thường (phải dùng rời nhóm).</summary>
public class KhongTheXoaNguoiTaoException : Exception
{
    public KhongTheXoaNguoiTaoException() : base("Không thể xóa người tạo nhóm — người tạo phải tự rời nhóm.")
    {
    }
}
