namespace HaloChat.Api.Repositories;

/// <summary>
/// Ném ra khi cơ sở dữ liệu từ chối thêm mới người dùng vì trùng lặp
/// TenTaiKhoan/Email (bắt được nhờ chỉ mục duy nhất), dùng làm lớp bảo vệ
/// chống race condition phía sau bước kiểm tra tồn tại ở tầng ứng dụng.
/// </summary>
public class TrungLapDinhDanhException : Exception
{
    public TrungLapDinhDanhException()
        : base("Tên tài khoản hoặc email đã tồn tại.")
    {
    }
}
