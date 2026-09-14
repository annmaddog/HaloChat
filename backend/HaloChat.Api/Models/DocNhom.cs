using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

// [GĐ5c] Theo dõi mỗi thành viên đã đọc tới tin nhắn nào trong mỗi nhóm —
// thay thế TinNhan.DaDoc dùng chung cho cả nhóm (không chính xác theo
// từng người). Không đụng TinNhan.DaDoc của nhánh 1-1.
public class DocNhom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiDungId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string NhomId { get; set; } = string.Empty;

    // Id tin nhắn mới nhất người này đã đọc trong nhóm này. Null = chưa
    // từng mở nhóm này — mọi tin nhắn trong nhóm đều tính là chưa đọc.
    public string? TinNhanCuoiDaDocId { get; set; }

    public DateTime ThoiGianDoc { get; set; } = DateTime.UtcNow;
}
