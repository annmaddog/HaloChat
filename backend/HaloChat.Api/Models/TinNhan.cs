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

    public DateTime ThoiGianTao { get; set; } = DateTime.UtcNow;
}
