using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public class Nhom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string TenNhom { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public string? DuongDanAnhDaiDien { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiTaoId { get; set; } = string.Empty;

    // Nhúng thẳng danh sách id thành viên (quy mô đồ án nhỏ, không cần
    // collection join riêng — đúng spec §10.2). Luôn chứa cả NguoiTaoId.
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> ThanhVienIds { get; set; } = new();

    public DateTime ThoiGianTao { get; set; } = DateTime.UtcNow;
}
