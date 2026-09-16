using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public enum LoaiTinNhan
{
    Text,
    Anh,
    File,
}

public class TinNhan
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiGuiId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? NguoiNhanId { get; set; }

    // [GĐ5b] Tin nhắn nhóm — để trống ở GĐ5a. Đúng một trong hai field
    // NguoiNhanId/NhomId có giá trị (spec §4, §10.2).
    [BsonRepresentation(BsonType.ObjectId)]
    public string? NhomId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public LoaiTinNhan LoaiTinNhan { get; set; } = LoaiTinNhan.Text;

    public string NoiDungTinNhan { get; set; } = string.Empty;

    public string? DuongDanFile { get; set; }
    public string? TenFileGoc { get; set; }
    public long? KichThuocFile { get; set; }
    public string? LoaiFile { get; set; }

    public bool DaDoc { get; set; } = false;

    // [GĐ5b-2] "Đã nhận" — true nếu người nhận đang online (>=1 kết nối
    // SignalR mở) tại thời điểm gửi. Chỉ có ý nghĩa với tin nhắn 1-1
    // (NguoiNhanId khác null); tin nhắn nhóm luôn để false, không track.
    public bool DaNhan { get; set; } = false;

    public DateTime ThoiGianTao { get; set; } = DateTime.UtcNow;

    // [GĐ6a] Snapshot thông tin tin gốc tại thời điểm trả lời — KHÔNG
    // tham chiếu sống. Null nếu tin này không phải trả lời tin nào.
    public TraLoiThongTin? TraLoi { get; set; }

    // [GĐ6b] Thu hồi: chỉ người gửi, không giới hạn thời gian. Khi true,
    // tầng DTO (AnhXaDto) LUÔN trả nội dung/file bằng placeholder.
    public bool DaThuHoi { get; set; } = false;

    // [GĐ6b] Ghim: ai trong hội thoại/nhóm cũng ghim/bỏ ghim được.
    public bool DaGhim { get; set; } = false;
    public DateTime? ThoiGianGhim { get; set; }

    // [GĐ7c] Mỗi người dùng chỉ có tối đa 1 phần tử (thả cảm xúc khác =
    // thay thế, không cộng dồn).
    public List<CamXucTinNhan> DanhSachCamXuc { get; set; } = new();
}
