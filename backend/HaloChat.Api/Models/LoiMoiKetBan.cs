using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public enum TrangThaiLoiMoiKetBan
{
    ChoDuyet,
    DaChapNhan,
    DaTuChoi,
}

public class LoiMoiKetBan
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiGuiId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiNhanId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public TrangThaiLoiMoiKetBan TrangThai { get; set; } = TrangThaiLoiMoiKetBan.ChoDuyet;

    public DateTime ThoiGianTao { get; set; } = DateTime.UtcNow;
}
